function New-PiraeusDemo
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$Location, 
	  [string]$Email, [string]$Dns, [string]$ClusterName, [string]$AppID, [string]$Password, 
	  [string]$OrleansStorageAcctName, [int]$NodeCount, [string]$GatewayVmSize, 
	  [string]$OrleansVmSize, [string]$LogLevel)
	  
	  $configParams = New-PiraeusDeployment -Path $Path -File $File -SubscriptionName $SubscriptionName -ResourceGroupName $ResourceGroupName -Location $Location -Email $Email -Dns $Dns ClusterName $ClusterName -AppId $AppId -Password $Password -OrleansStorageAcctName $OrleansStorageAcctName -NodeCount $NodeCount -GatewayVmSize $GatewayVmSize -OrleansVmSize $OrleansVmSize -LogLevel $LogLevel
	  
	  $config = ConvertFrom-Json -InputObject $configParams
	  
	  $sampleConfig = [PSCustomObject]@{        
        dns = "$config.piraeusDns"
        location = "$config.location"
        issuer = "$config.issuer"
        audience = "$config.audience"
        identityClaimType = "$config."
        symmetricKey = "$config.symmetricKey"
        }
	  
	  $sampleConfig | ConvertTo-Json -depth 100 | Out-File "$Path/../src/Samples.Mqtt.Client/config.json"
	  
	  $key = "$config.apiCode"
	  $dnsName = "$config.piraeusDns"
	  $loc = "$config.location"
	  
	  New-SampleConfig -DnsName $dnsName -Location $loc -Key $key
}


function New-PiraeusDeployment
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$Location, 
	  [string]$Email, [string]$Dns, [string]$ClusterName, [string]$AppID, [string]$Password, 
	  [string]$OrleansStorageAcctName, [int]$NodeCount, [string]$GatewayVmSize, 
	  [string]$OrleansVmSize, [string]$LogLevel)
		
		$env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'
				
		if($GatewayVmSize.Length -eq 0)
		{
			$GatewayVmSize = "Standard_D2s_v3"
		}

		if($OrleansVmSize.Length -eq 0)
		{
			$OrleansVmSize = "Standard_D4s_v3"
		} 

		if($NodeCount -eq 0)
		{
			$NodeCount = 1
		}
		
		if($LogLevel.Length -eq 0)
		{
			$LogLevel = "Information"
		}

		if($File.Length -eq 0)
		{
			$dateTimeString = Get-Date -Format "MM-dd-yyyyTHH-mm-ss"
			$File = "./piraeus-" + $dateTimeString + ".json"
		}

		$start = Get-Date
		$step = 1
		
		Update-Step -Step $step -Message "Clean up previous local config from previous Kubectl deployment (optional)" -Start $start
		$step++
		New-KubectlClusterCleanup -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName
			   
		Update-Step -Step $step -Message "Adding Application Insights extension to Azure CLI" -Start $start
		$step++
		Set-AppInsightsExtension
		
		Update-Step -Step $step -Message "Adding Iot Hub extension to Azure CLI" -Start $start
		Set-IoTHubExtension
		$step++

		Update-Step -Step $step -Message "Set Subscription for deployment" -Start $start
		$step++
		Set-Subscription -SubscriptionName $SubscriptionName
		
		Update-Step -Step $step -Message "Set Resource Group for deployment" -Start $start
		$step++
		Set-ResourceGroup -ResourceGroupName $ResourceGroupName -Location $Location
		
		Update-Step -Step $step -Message "If exists delete Piraeus AKS cluster $ClusterName from Azure" -Start $start
		$step++
		Remove-AksCluster -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName    
		
		Update-Step -Step $step -Message "Set Service Principal" -Start $start
		$step++
		$spn = Get-ServicePrincipal -AppID $AppID -Password $Password   
		$spnAppId = $spn."appId"
		$spnPwd = $spn."pwd"		

		Update-Step -Step $step -Message "Create Orleans storage account" -Start $start
		$step++   
		
		$orleansNameAvailable = Get-StorageAccountNameAvailable -StorageAcctName "$OrleansStorageAcctName"  -SubscriptionName "$SubscriptionName"
		
		if($orleansNameAvailable)
		{
			Write-Host "Storage Account Name available" 
			New-StorageAccount -StorageAcctName $OrleansStorageAcctName -Location $Location -ResourceGroupName $ResourceGroupName       
		}

		$orleansConnectionString = Get-StorageAccountConnectionString -StorageAcctName $OrleansStorageAcctName -ResourceGroupName $ResourceGroupName
		if($LASTEXITCODE -ne 0)
		{
			Write-Host "Failed to get orleans storage account connection string...terminating script." -ForegroundColor Yellow
			return
		}


		Update-Step -Step $step -Message "Orleans storage account connection string obtained" -Start $start
		$step++     

		Update-Step -Step $step -Message "Create Audit storage account" -Start $start
		$step++
		$auditStorageAcctName = $OrleansStorageAcctName + "audit"
		$auditNameAvailable = Get-StorageAccountNameAvailable -SubscriptionName $SubscriptionName -StorageAcctName $auditStorageAcctName
		
		if($auditNameAvailable)
		{
			New-StorageAccount -StorageAcctName $auditStorageAcctName  -Location $Location -ResourceGroupName $ResourceGroupName
		}

		$auditConnectionString = Get-StorageAccountConnectionString -StorageAcctName $auditStorageAcctName -ResourceGroupName $ResourceGroupName
		if($LASTEXITCODE -ne 0)
		{
			Write-Host "Failed to get audit storage account connection string...terminating script." -ForegroundColor Yellow
			return
		}
	   
		Update-Step -Step $step -Message "Audit storage account connection string obtained" -Start $start
		$step++

		Update-Step -Step $step -Message "Creating new Piraeus AKS cluster" -Start $start
		$step++
		
		Write-Host "Cluster = $ClusterName" -ForegroundColor Cyan
		New-AksCluster -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName -AppId "$spnAppId" -Password "$spnPwd" -VmSize $GatewayVmSize -NodeCount $NodeCount
		
		Update-Step -Step $step -Message "Get AKS credentials" -Start $start
		$step++
		Get-AksCredentials -ClusterName $ClusterName -ResourceGroupName $ResourceGroupName
		
		Update-Step -Step $step -Message "Create cert-manager namespace" -Start $start
		$step++
				
		#Update-Step -Step $step -Message "Apply HELM RBAC" -Start $start
		#$step++
		#New-KubectlApply -Filename "$Path/helm-rbac.yaml" -Namespace "kube-system"

		#Update-Step -Step $step -Message "Start Tiller" -Start $start
		#$step++
		#helm init --service-account tiller
		#Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45
		
		Update-Step -Step $step -Message "Set Node pool label for Piraeus front end" -Start $start
		$step++
		Set-NodeLabel -NodeMatchValue "nodepool1" -Key "pool" -Value "nodepool1"

		Update-Step -Step $step -Message "Add cert manager for Let's Encrypt" -Start $start
		$step++
		Add-CertManager  
		Set-Timer -Message "...waiting 45 seconds for cert-manager to initialize" -Seconds 45

		Update-Step -Step $step -Message "Add cert issuer for Lets Encrypt" -Start $start
		$step++
		Add-Issuer -Email $Email -IssuerPath "$Path/issuer.yaml" -IssuerDestination "$Path/issuer-copy.yaml"
		Set-Timer -Message "...waiting 30 seconds for issuer to initialize" -Seconds 30

		Update-Step -Step $step -Message "Upate local HELM repo" -Start $start
		$step++
		helm repo add stable https://kubernetes-charts.storage.googleapis.com/

		Update-Step -Step $step -Message "Add NGINX" -Start $start
		$step++
		Add-NGINX
		Set-Timer "...waiting 45 seconds for nginx to initialize" -Seconds 45

		Update-Step -Step $step -Message "Get External IP address" -Start $start
		$step++
		$IP = Get-ExternalIP 

		Update-Step -Step $step -Message "Create Public IP ID" -Start $start
		$step++
		$PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)

		Update-Step -Step $step -Message "Update Public IP ID" -Start $start
		$step++
		Update-PublicIP -PublicIP $PUBLICIPID -Dns $Dns -SubscriptionName $SubscriptionName
		 
		Update-Step -Step $step -Message "Set certificate for cert-manager and Lets Encrypt" -Start $start
		$step++
		Set-Certificate -Dns $Dns -Location $Location -Path "$Path/certificate.yaml" -Destination "$Path/certificate-copy.yaml"

		Update-Step -Step $step -Message "Add Orleans cluster for 2nd node pool" -Start $start
		$step++
		Add-NodePool -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName -NodePoolName "nodepool2" -NodeCount $NodeCount -VmSize $OrleansVmSize
		Set-Timer -Message "...waiting 60 seconds for node to initialize" -Seconds 60

		Update-Step -Step $step -Message "Create node pool label for 2nd node pool" -Start $start
		$step++
		Set-NodeLabel "nodepool2" "pool" "nodepool2"


		Update-Step -Step $step -Message "Creating random Piraeus Management API keys" -Start $start
		$step++
		$apiKey1 = New-RandomKey -Length 16
		$apiKey2 = New-RandomKey -Length 16
		$apiSecurityCodes = $apiKey1 + ";" + $apiKey2
		$apiIssuer = "http://$Dns.$Location.cloudapp.azure.com/mgmt"
		$apiAudience = $apiIssuer
		$identityClaimType = "http://$Dns.$Location.cloudapp.azure.com/name"
		$issuer = "http://$Dns.$Location.cloudapp.azure.com/"
		$audience = $issuer
		$coapAuthority = "http://$Dns.$Location.cloudapp.azure.com"
		$tokenType = "JWT"

		Update-Step -Step $step -Message "Creating random Piraeus Management API symmetric key" -Start $start
		$step++
		$apiSymmetricKey = New-RandomKey -Length 32

		Update-Step -Step $step -Message "Creating random Piraeus Gateway symmetric key" -Start $start
		$step++
		$symmetricKey = New-RandomKey -Length 32
		
		Update-Step -Step $step -Message "Creating App Insights for Orleans cluster and getting instrumentation key" -Start $start
		$step++
		$siloAIKey = Get-InstrumentationKey "$Dns-silo" -ResourceGroupName $ResourceGroupName -Location $Location
		
		Update-Step -Step $step -Message "Install Orleans cluster from helm chart" -Start $start
		$step++
		helm install piraeus-silo  "$Path/piraeus-silo" --namespace kube-system --set dataConnectionString=$orleansConnectionString --set instrumentationKey=$siloAIKey --set logLevel=$LogLevel 
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices
			
			Update-Step -Step $step -Message "Trying again to install Orleans cluster from helm chart" -Start $start
			$step++
			helm install piraeus-silo "$Path/piraeus-silo" --namespace kube-system --set dataConnectionString=$orleansConnectionString --set instrumentationKey=$siloAIKey --set logLevel=$LogLevel
		}

		Update-Step -Step $step -Message "Creating App Insights for Piraeus Management API and getting instrumentation key" -Start $start
		$step++
		$mgmtAIKey = Get-InstrumentationKey "$Dns-api" -ResourceGroupName $ResourceGroupName -Location $Location
		
		Update-Step -Step $step -Message "Install Piraeus Management API from helm chart" -Start $start
		$step++
		helm install piraeus-mgmt-api "$Path/piraeus-mgmt-api" --namespace kube-system --set dataConnectionString="$orleansConnectionString"  --set managementApiIssuer="$apiIssuer" --set managementApiAudience="$apiAudience" --set managmentApiSymmetricKey="$apiSymmetricKey" --set managementApiSecurityCodes="$apiSecurityCodes" --set instrumentationKey=$mgmtAIKey --set logLevel=$LogLevel
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices

			Update-Step -Step $step -Message "Trying again to install Piraeus Management API from helm chart" -Start $start
			$step++
			helm install piraeus-mgmt-api "$Path/piraeus-mgmt-api" --namespace kube-system --set dataConnectionString="$orleansConnectionString"  --set managementApiIssuer="$apiIssuer" --set managementApiAudience="$apiAudience" --set managmentApiSymmetricKey="$apiSymmetricKey" --set managementApiSecurityCodes="$apiSecurityCodes" --set instrumentationKey=$mgmtAIKey --set logLevel=$LogLevel
		}

		Update-Step -Step $step -Message "Creating App Insights for Piraeus Web Socket Gateway and getting instrumentation key" -Start $start
		$step++
		$websocketAIKey = Get-InstrumentationKey "$Dns-websocket" -ResourceGroupName $ResourceGroupName -Location $Location

		Update-Step -Step $step -Message "Install Piraeus Web Socket Gateway from helm chart" -Start $start
		$step++
		helm install piraeus-websocket "$Path/piraeus-websocket" --namespace kube-system --set dataConnectionString="$orleansConnectionString" --set auditConnectionString="$auditConnectionString" --set clientIdentityNameClaimType="$identityClaimType" --set clientIssuer="$issuer" --set clientAudience="$audience" --set clientTokenType="$tokenType" --set clientSymmetricKey="$symmetricKey" --set coapAuthority="$coapAuthority" --set instrumentationKey=$websocketAIKey --set logLevel=$LogLevel 
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices

			Update-Step -Step $step -Message "Trying again to install Piraeus Web Socket Gateway from helm chart" -Start $start
			$step++
			helm install piraeus-websocket "$Path/piraeus-websocket" --namespace kube-system --set dataConnectionString="$orleansConnectionString" --set auditConnectionString="$auditConnectionString" --set clientIdentityNameClaimType="$identityClaimType" --set clientIssuer="$issuer" --set clientAudience="$audience" --set clientTokenType="$tokenType" --set clientSymmetricKey="$symmetricKey" --set coapAuthority="$coapAuthority" --set instrumentationKey=$websocketAIKey --set logLevel=$LogLevel 
		}

		Update-Step -Step $step -Message "Updating the NGINX ingress controller" -Start $start
		$step++
		Set-Ingress -Dns $Dns -Location $Location -Path "$Path/ingress.yaml" -Destination "$Path/ingress-copy.yaml"


		Update-Step -Step $step -Message "Write File = $File" -Start $start
		$step++
		
		$config = [PSCustomObject]@{        
        subscription = $SubscriptionName
        resourceGroup = $ResourceGroupName
        piraeusClusterName = $ClusterName
        piraeusHostname = "$Dns.$Location.cloudapp.azure.com"
        piraeusDns = $Dns
        email = $Email
        location = $Location
        logLevel = $LogLevel
        nodeCount = $NodeCount
        appId = $spnAppId 
        pwd = $spnPwd
        piraeusPublicIP = $IP 
        tokenType = $tokenType
        symmetricKey = $symmetricKey
        identityClaimType = $identityClaimType
        coapAuthority = $coapAuthority        
        orleansConnectionString = $orleansConnectionString
        orleanVmSize = $OrleansVmSize
        orleansAppInsights = "$Dns-silo"
        orleansAppInsightsKey = $siloAIKey
        gatewayVmSize = $GatewayVmSize 
        auditConnectionString = $auditConnectionString      
        gatewayAppInsights = "$Dns-websocket"
        gatewayAppInsightsKey = $websocketAIKey
        apiCodes = $apiSecurityCodes
        apiIssuer = $apiIssuer
        apiAudience = $apiAudience
        apiSymmetricKey = $apiSymmetricKey
        apiAppInsights = "$Dns-api"
        apiAppInsightsKey = $mgmtAIKey
        claimTypes = $identityClaimType        
        apiCode = $apiSecurityCodes.Split(";")[0]	
        lifetimeMinutes = 525600    
    }
		
	$config | ConvertTo-Json -depth 100 | Out-File $File

	Write-Host "---- Piraeus Deployed -----" -ForegroundColor Cyan	
	return $config	  
}

function New-SampleConfig
{
    param([string]$DnsName, [string]$Location, [string]$Key)
    
    $authority = $DnsName.ToLower() + "." + $Location.ToLower() + ".cloudapp.azure.com"
    $url = "https://$authority"
    Write-Host "Using $url for management api" -ForegroundColor Yellow

    Import-Module "../src/Piraeus.Module.Core/bin/Release/netcoreapp3.0/Piraeus.Module.Core.dll"
    Write-Host "Module imported" -ForegroundColor Yellow

    #get a security token for the management API
    Write-Host "--- Get security token for Piraeus configuration ---" -Foreground Yellow
    $token = Get-PiraeusManagementToken -ServiceUrl $url -Key $Key
    while($LASTEXITCODE -ne 0)
    {
		Write-Host "--- Try get security token again...waiting 30 seconds" -ForegroundColor Yellow
		Start-Sleep -Seconds 30
		$token = Get-PiraeusManagementToken -ServiceUrl $url -Key $Key
    }
    
    Write-Host "--- Got security token, ready to configure Piraeus ---" -ForegroundColor Green
    
        
    Write-Host "---  INFORMATION ABOUT Sample Config ----" -ForegroundColor White
    Write-Host "The client demos create security tokens based on the selection of a 'Role', i.e., 'A' or 'B'" -ForegroundColor White
    Write-Host "The script will create 2 CAPL policies" -ForegroundColor White
    Write-Host "  (1) a client in role 'A' may transmit to 'resource-a' and subscribe to 'resource-b'" -ForegroundColor White
    Write-Host "  (2) a client in role 'B' may transmit to 'resource-b' and subscribe to 'resource-a'" -ForegroundColor White
    Write-Host "-----------------------------------------" -ForegroundColor White
    Write-Host ""
    Start-Sleep -Seconds 1
        
    #--------------- CAPL policy for users in role "A" ------------------------
    Write-Host "-- Building CAPL Authorization Policies ---" -ForegroundColor White
    Write-Host "  (1) Match Expression : Find a claim type in the security token" -ForegroundColor White
    Write-Host "  (2) Operation -- Binds a claim value from the matched claim type to perform an operation, e.g., Equals" -ForegroundColor White
    Write-Host "  (3) Rule -- Create a rule that binds a match expression and an operation" -ForegroundColor White
    Write-Host "  (4) Policy -- create a policy that is uniquely identifiable, that incorporates a Rule (or Logical Connective)" -ForegroundColor White
    Write-Host ""
    Start-Sleep -Seconds 1              

    #define the claim type to match to determines the client's role
    $authority = $DnsName.ToLower() + "." + $Location.ToLower() + "." + "cloudapp.azure.com"
    $matchClaimType = "http://$authority/role"

    #create a match expression of 'Literal' to match the role claim type
    $match = New-CaplMatch -Type Literal -ClaimType $matchClaimType -Required $true  

    #create an operation to check the match claim value is 'Equal' to "A"
    $operation_A = New-CaplOperation -Type Equal -Value "A"

    #create a rule to bind the match expression and operation
    $rule_A = New-CaplRule -Evaluates $true -MatchExpression $match -Operation $operation_A

    #define a unique identifier (as URI) for the policy
    $policyId_A = "http://$authority/policy/resource-a" 

    #create the policy for clients in role "A"
    $policy_A = New-CaplPolicy -PolicyID $policyId_A -EvaluationExpression $rule_A
    #-------------------End Policy for "B"------------------------------------
        
        
    #--------------- CAPL policy for users in role "B" ------------------------

    #create an operation to check the match claim value is 'Equal' to "B"
    $operation_B = New-CaplOperation -Type Equal -Value "B"

    #create a rule to bind the match expression and operation
    $rule_B = New-CaplRule -Evaluates $true -MatchExpression $match -Operation $operation_B

    #define a unique identifier (as URI) for the policy
    $policyId_B = "http://$authority/policy/resource-b" 

    #create the policy for users in role "A"
    $policy_B = New-CaplPolicy -PolicyID $policyId_B -EvaluationExpression $rule_B

    #-------------------End Policy for "B"------------------------------------

    # The policies are completed.  We need to add them to Piraeus

    Add-CaplPolicy -ServiceUrl $url -SecurityToken $token -Policy $policy_A 
    Add-CaplPolicy -ServiceUrl $url -SecurityToken $token -Policy $policy_B

    Write-Host "CAPL policies added to Piraeus" -ForegroundColor Yellow


    #Uniquely identify Piraeus resources by URI
    $resource_A = "http://$authority/resource-a"
    $resource_B = "http://$authority/resource-b"

    #Add the resources to Piraeus

    #Resource "A" lets users with role "A" send and users with role "B" subscribe to receive transmissions
    Add-PiraeusEventMetadata -ResourceUriString $resource_A -Enabled $true -RequireEncryptedChannel $false -PublishPolicyUriString $policyId_A -SubscribePolicyUriString $policyId_B -ServiceUrl $url -SecurityToken $token -Audit $false

    #Resource "B" lets users with role "B" send and users with role "A" subscribe to receive transmissions
    Add-PiraeusEventMetadata -ResourceUriString $resource_B -Enabled $true -RequireEncryptedChannel $false -PublishPolicyUriString $policyId_B -SubscribePolicyUriString $policyId_A -ServiceUrl $url -SecurityToken $token -Audit $false

    Write-Host "PI-System metadata added to Piraeus" -ForegroundColor Yellow
    Write-Host""
    #Quick check get the resource data and verify what was set
    Write-Host "----- PI-System $resource_A Metadata ----" -ForegroundColor Green
    Get-PiraeusEventMetadata -ResourceUriString $resource_A -ServiceUrl $url -SecurityToken $token
    

    Write-Host""


    Write-Host "----- PI-System $resource_B Metadata ----" -ForegroundColor Green
    Get-PiraeusEventMetadata -ResourceUriString $resource_B -ServiceUrl $url -SecurityToken $token
}