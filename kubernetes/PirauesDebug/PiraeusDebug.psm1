function New-PiraeusDeployment
{
	param([string]$Path, [string]$File, [string]$SubscriptionName, [string]$ResourceGroupName, [string]$Location, 
	  [string]$Email, [string]$Dns, [string]$ClusterName, [string]$AppID, [string]$Password, 
	  [string]$OrleansStorageAcctName, [int]$NodeCount, [string]$GatewayVmSize, 
	  [string]$OrleansVmSize, [string]$LogLevel)
		
		$env:AZURE_HTTP_USER_AGENT='pid-332e88b9-31d3-5070-af65-de3780ad5c8b'
  
		kubectl create namespace "cert-manager"
		
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
		
		Update-Step -Step $step -Message "Apply HELM RBAC" -Start $start
		$step++
		New-KubectlApply -Filename "$Path/helm-rbac.yaml" -Namespace "kube-system"

		Update-Step -Step $step -Message "Start Tiller" -Start $start
		$step++
		helm init --service-account tiller
		Set-Timer -Message "...waiting 45 seconds for Tiller to start" -Seconds 45
		
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
		helm install "$Path/piraeus-silo" --name piraeus-silo --namespace kube-system --set dataConnectionString=$orleansConnectionString --set instrumentationKey=$siloAIKey --set logLevel=$LogLevel
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices
			
			Update-Step -Step $step -Message "Trying again to install Orleans cluster from helm chart" -Start $start
			$step++
			helm install "$Path/piraeus-silo" --name piraeus-silo --namespace kube-system --set dataConnectionString=$orleansConnectionString --set instrumentationKey=$siloAIKey --set logLevel=$LogLevel
		}

		Update-Step -Step $step -Message "Creating App Insights for Piraeus Management API and getting instrumentation key" -Start $start
		$step++
		$mgmtAIKey = Get-InstrumentationKey "$Dns-api" -ResourceGroupName $ResourceGroupName -Location $Location
		
		Update-Step -Step $step -Message "Install Piraeus Management API from helm chart" -Start $start
		$step++
		helm install "$Path/piraeus-mgmt-api" --namespace kube-system --set dataConnectionString="$orleansConnectionString"  --set managementApiIssuer="$apiIssuer" --set managementApiAudience="$apiAudience" --set managmentApiSymmetricKey="$apiSymmetricKey" --set managementApiSecurityCodes="$apiSecurityCodes" --set instrumentationKey=$mgmtAIKey --set logLevel=$LogLevel
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices

			Update-Step -Step $step -Message "Trying again to install Piraeus Management API from helm chart" -Start $start
			$step++
			helm install "$Path/piraeus-mgmt-api" --namespace kube-system --set dataConnectionString="$orleansConnectionString"  --set managementApiIssuer="$apiIssuer" --set managementApiAudience="$apiAudience" --set managmentApiSymmetricKey="$apiSymmetricKey" --set managementApiSecurityCodes="$apiSecurityCodes" --set instrumentationKey=$mgmtAIKey --set logLevel=$LogLevel
		}

		Update-Step -Step $step -Message "Creating App Insights for Piraeus Web Socket Gateway and getting instrumentation key" -Start $start
		$step++
		$websocketAIKey = Get-InstrumentationKey "$Dns-websocket" -ResourceGroupName $ResourceGroupName -Location $Location

		Update-Step -Step $step -Message "Install Piraeus Web Socket Gateway from helm chart" -Start $start
		$step++
		helm install "$Path/piraeus-websocket" --namespace kube-system --set dataConnectionString="$orleansConnectionString" --set auditConnectionString="$auditConnectionString" --set clientIdentityNameClaimType="$identityClaimType" --set clientIssuer="$issuer" --set clientAudience="$audience" --set clientTokenType="$tokenType" --set clientSymmetricKey="$symmetricKey" --set coapAuthority="$coapAuthority" --set instrumentationKey=$websocketAIKey --set logLevel=$LogLevel 
		if($LASTEXITCODE -ne 0 )
		{
			Update-Step -Step $step -Message "Waiting for Kubernetes API Services to start" -Start $start
			$step++
			Set-WaitForApiServices

			Update-Step -Step $step -Message "Trying again to install Piraeus Web Socket Gateway from helm chart" -Start $start
			$step++
			helm install "$Path/piraeus-websocket" --namespace kube-system --set dataConnectionString="$orleansConnectionString" --set auditConnectionString="$auditConnectionString" --set clientIdentityNameClaimType="$identityClaimType" --set clientIssuer="$issuer" --set clientAudience="$audience" --set clientTokenType="$tokenType" --set clientSymmetricKey="$symmetricKey" --set coapAuthority="$coapAuthority" --set instrumentationKey=$websocketAIKey --set logLevel=$LogLevel 
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
        iotHubConnectionString = ""
        apiCodes = $apiSecurityCodes
        apiIssuer = $apiIssuer
        apiAudience = $apiAudience
        apiSymmetricKey = $apiSymmetricKey
        apiAppInsights = "$Dns-api"
        apiAppInsightsKey = $mgmtAIKey
        claimTypes = $identityClaimType
        claimValues = ""
        containerName = "maps"
        filename = ""
        tableName = "gateway"
        apiCode = $apiSecurityCodes.Split(";")[0]	
        lifetimeMinutes = 525600
        vrtuIP = ""
        vrtuVmSize = ""
        virtualRtuId = ""
        vrtuConnectionString = ""
        vrtuInstrumentationKey = ""
        tenantId = ""
        clientId = ""
        domain = ""
        monitorDns = ""
        monitorPublicIP = ""
        monitorInstrumentationKey = ""
        monitorVmSize = ""
    }
		
	$config | ConvertTo-Json -depth 100 | Out-File $File	
	
	New-SampleConfig -DnsName $Dns -Location $Location -Key $apiSecurityCodes.Split(";")[0]		

	Write-Host "---- Piraeus Deployed and Sample configured -----" -ForegroundColor Cyan		  
}

function Add-CertManager
{
	param([string]$Namespace = "cert-manager")

    kubectl label namespace $Namespace certmanager.k8s.io/disable-validation="true"
    kubectl apply -f "https://raw.githubusercontent.com/jetstack/cert-manager/release-0.11/deploy/manifests/00-crds.yaml" -n "$Namespace" --validate=false
    helm repo add jetstack https://charts.jetstack.io
    helm repo update
    helm install --name cert-manager --namespace $Namespace --version v0.11.0 --set ingressShim.extraArgs='{--default-issuer-name=letsencrypt-prod,--default-issuer-kind=ClusterIssuer}' jetstack/cert-manager --set webhook.enabled=true      
    
}

function Add-Issuer 
{
    param([string]$Email, [string]$IssuerPath, [string]$IssuerDestination, [string]$Namespace = "kube-system")

    Copy-Item -Path $IssuerPath -Destination $IssuerDestination
	Update-Yaml -NewValue $Email -MatchString "EMAILREF" -Filename $IssuerDestination           
	kubectl apply -f $IssuerDestination -n $Namespace
	Remove-Item -Path $IssuerDestination
}

function Add-NGINX
{
	param([string]$Namespace = "kube-system")
	$looper = $true
    while($looper)
    {
		try
		{
			helm install stable/nginx-ingress --namespace $Namespace --set controller.replicaCount=1
			if($LASTEXITCODE -ne 0 )
            {
				Write-Host "Error installing NGINX, waiting 20 seconds to try install NGINX again..." -ForegroundColor Yellow
				Start-Sleep -Seconds 20
            }
            else
            {
				$looper = $false
            }			
		}
		catch
		{
			Write-Host "Waiting 20 seconds to try install NGINX again..." -ForegroundColor Yellow
            Start-Sleep -Seconds 20
		}
	}
}

function Add-NodePool()
{
    param([string]$ResourceGroupName, [string]$ClusterName, [string]$NodePoolName, [int]$NodeCount, [string]$VmSize)

    az aks nodepool add --resource-group $ResourceGroupName --cluster-name $ClusterName --name $NodePoolName --node-count $NodeCount --node-vm-size $VmSize
}

function Get-AksClusterExists
{
    param([string]$SubscriptionName, [string]$ClusterName)

    $aksList = az aks list --subscription "$SubscriptionName" | ConvertFrom-Json

    foreach($item in $aksList)
    {
        $id = $item.id
        $parts = $id.Split("/")
        $name = $parts[$parts.Length - 1]

        if($name -eq $ClusterName)
        {
            return $true
        }
    }

    return $false
}

function Get-AksCredentials()
{
    param([string]$ResourceGroupName, [string]$ClusterName)

    $looper = $true
    while($looper)
    {
        try
        {         
            az aks get-credentials --resource-group $ResourceGroupName --name $ClusterName            
            $looper = $false
        }
        catch
        {
            Write-Host "Waiting 30 seconds to try get aks credentials again..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }    
    }
}

function Get-ApiServicesOnline()
{
	$v = kubectl get apiservice
	$ft = $true
	while(([string]$v).IndexOf("False") -ne -1)
	{
		if($ft)
		{
			Write-Host("K8 metrics-server and/or cert-manager-webhook is offline right now. We'll keep waiting until they are online") -ForegroundColor Yellow
			$ft = $false
		}
		else
		{
			Write-Host("Waiting 60 secs for the K8 apiservices to come back online, yuck...") -ForegroundColor Yellow
		}
		Start-Sleep -Seconds 60
		$v = kubectl get apiservice
	}
}

function Get-ExternalIPForService
{
	param([string]$AppName, [string]$Namespace = "kube-system")
	$looper = $TRUE
    while($looper)
    {   $externalIP = ""                  
        $lineValue = kubectl get service -l app=$AppName --namespace $Namespace
        
        Write-Host "Last Exit Code for get external ip $LASTEXITCODE" -ForegroundColor White
        if($LASTEXITCODE -ne 0 )
        {
            Write-Host "Try get external ip...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }  
        elseif($lineValue.Length -gt 0)
        {
            $line = $lineValue[1]
            $lineout = $line -split '\s+'
            $externalIP = $lineout[3]              
        }
        
              
        if($externalIP -eq "<pending>")
        {        
            Write-Host "External IP is pending...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        elseif($externalIP.Length -eq 0)
        {
            Write-Host "External IP is zero length...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        else
        {
			$looper = $FALSE
            Write-Host "External IP is $externalIP" -ForegroundColor Magenta
            return $externalIP
        }
    }
}

function Get-ExternalIP
{
	param([string]$Namespace = "kube-system")
    $looper = $TRUE
    while($looper)
    {   $externalIP = ""                  
        $lineValue = kubectl get service -l app=nginx-ingress --namespace $Namespace
        
        Write-Host "Last Exit Code for get external ip $LASTEXITCODE" -ForegroundColor White
        if($LASTEXITCODE -ne 0 )
        {
            Write-Host "Try get external ip...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }  
        elseif($lineValue.Length -gt 0)
        {
            $line = $lineValue[1]
            $lineout = $line -split '\s+'
            $externalIP = $lineout[3]              
        }
        
              
        if($externalIP -eq "<pending>")
        {        
            Write-Host "External IP is pending...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        elseif($externalIP.Length -eq 0)
        {
            Write-Host "External IP is zero length...waiting 30 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        else
        {
			$looper = $FALSE
            Write-Host "External IP is $externalIP" -ForegroundColor Magenta
            return $externalIP
        }
    }
}

function Get-InstrumentationKey
{
    param([string]$AppName, [string]$ResourceGroupName, [string]$Location)

    $jsonAppString = az monitor app-insights component show --app $AppName -g $ResourceGroupName
	$host.ui.RawUI.ForegroundColor = 'Gray'	
    if($LASTEXITCODE -ne 0)
    {
		Write-Host "Not an error. App Insights component not found and will be created." -ForegroundColor Yellow
	    Write-Host "-- Step $step - Creating App Insights $AppName" -ForegroundColor Green
			
	    $jsonAppString = az monitor app-insights component create -a $AppName -l $Location -k other -g $ResourceGroupName --application-type other
	    $step++       
    }

    $appKey = New-RandomKey(8)
    $jsonKeyString = az monitor app-insights api-key create --api-key $appKey -g $ResourceGroupName -a $AppName
    $keyObj = ConvertFrom-Json -InputObject "$jsonKeyString"
    $instrumentationKey = $keyObj.apiKey	
	$host.ui.RawUI.ForegroundColor = 'Gray'
    return $instrumentationKey
}

function Get-IoTHubConnectionString
{
    param([string]$HubName, [string]$ResourceGroupName)
        
    $hub = az iot hub show-connection-string --name $HubName --resource-group $ResourceGroupName | ConvertFrom-Json

    if($LASTEXITCODE -ne 0)
    {
		$host.ui.RawUI.ForegroundColor = 'Gray'
        Write-Host "Not an error. The IoT Hub does not exist.  Trying to create a free IoT Hub" -ForegroundColor Yellow
        $res = az iot hub create --name $HubName --resource-group $ResourceGroupName --sku F1 --partition-count 2
        $host.ui.RawUI.ForegroundColor = 'Gray'
        if($LASTEXITCODE -ne 0)
        {
			$host.ui.RawUI.ForegroundColor = 'Gray'
			Write-Host "Not an error. Cannot create a free IoT Hub (F1) because it is already used." -ForegroundColor Yellow
            $newS1Sku = Read-Host "Would you like to create an S1 SKU (25/month) [y/n] ? "
			if($newS1Sku.ToLowerInvariant() -eq "y")
			{
				$host.ui.RawUI.ForegroundColor = 'Gray'
				az iot hub create --name $HubName --resource-group $ResourceGroupName --sku S1
            }
            else
            {
				Write-Host("Exiting script") -ForegroundColor Yellow
				return ""
            }
        }  
            
        $host.ui.RawUI.ForegroundColor = 'Gray'
        Write-Host "Waiting 60 seconds for IoT Hub to be available." -ForegroundColor Yellow
        Start-Sleep -Seconds 60
        $hub = az iot hub show-connection-string --name $HubName --resource-group $ResourceGroupName | ConvertFrom-Json
	
        return $hub.connectionString
    }
    else
    {
        return $hub.connectionString
    }	
}

function Get-ServicePrincipal
{
    param([string]$AppID, [string]$Password)   
    

    if($AppID -eq $null -or $AppID.Length -eq 0)
	{
		#create the service principal
		$creds = az ad sp create-for-rbac  --skip-assignment
		$credsObj = ConvertFrom-Json -InputObject "$creds"
		$appId = $credsObj.appId
		$pwd = $credsObj.password		
	}
	else
	{
		$appId = $AppID
        $pwd = $Password
	}


    $spn = [PSCustomObject]@{
        appId = $appId
        pwd = $pwd
    }
    
    return $spn   
}

function Get-StorageAccountConnectionString
{
    param([string]$StorageAcctName, [string]$ResourceGroupName)

    $storageJsonString = az storage account show-connection-string --name $StorageAcctName --resource-group $ResourceGroupName
	$storageObj = ConvertFrom-Json -InputObject "$storageJsonString"
	return $storageObj.connectionString
}

function Get-StorageAccountNameAvailable
{
    param([string]$StorageAcctName, [string]$SubscriptionName)

    $jsonString = az storage account check-name --name $StorageAcctName --subscription $SubscriptionName
    $jsonObj = ConvertFrom-Json -InputObject "$jsonString"
    return $jsonObj.nameAvailable	

}

function New-AksCluster
{
    param([string]$ClusterName, [string]$ResourceGroupName, [string]$AppID, [string]$Password, [string]$VmSize, [int]$NodeCount)

    az aks create --resource-group $ResourceGroupName --name $ClusterName --node-count $NodeCount --service-principal $AppID --client-secret $Password --node-vm-size $VmSize --generate-ssh-keys
}

function New-KubectlApply
{
	param([string]$Filename, [string]$Namespace = "kube-system")
	
	$looper = $true
    while($looper)
    {    
		kubectl apply -f $Filename -n $Namespace
		if($LASTEXITCODE -ne 0)
        {
            Write-Host "Waiting 30 to re-apply file..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
        }
        else
        {
			$looper = $false
        }
	}
}

function New-KubectlClusterCleanup
{
    param([string]$ClusterName, [string]$ResourceGroupName)

    #Remove previous deployments from kubectl
	$cleanup = Read-Host "Clean up previous kubectl deployment [y/n] ? "
	if($cleanup.ToLowerInvariant() -eq "y")
	{
		$cleanupClusterName = Read-Host "Enter previous cluster name [Enter blank == $ClusterName] "
		$cleanupResourceGroup = Read-Host "Enter previous resource group name [Enter blank == $ResourceGroupName] "
		
		if($cleanupClusterName.Length -eq 0)
		{
			$cleanupClusterName = $ClusterName
		}
		
		if($cleanupResourceGroup.Length -eq 0)
		{
			$cleanupResourceGroup = $ResourceGroupName
		}
		
		$condition1 = "users.clusterUser_" + $cleanupResourceGroup + "_" + $cleanupClusterName
		$condition2 = "clusters." + $cleanupClusterName
		kubectl config unset $condition1
		kubectl config unset $condition2
	}
}

function New-RandomKey   
{
    param([int]$Length)
    
	$random = new-Object System.Random
	$buffer = [System.Byte[]]::new($Length)
	$random.NextBytes($buffer)
	$stringVar = [Convert]::ToBase64String($buffer)
    if($stringVar.Contains("+") -or $stringVar.Contains("/"))
    {
        return New-RandomKey($Length)
    }
    else
    {
        return $stringVar
    }
}

function New-RandomStorageAcctName()
{
	$alpha = "abcdefghijklmnopqrstuvwxyz"
	$alpha2 = "0123456789"
	$array = $alpha.ToCharArray()
	$array2 = $alpha2.ToCharArray()
	$maxLength = 6
	$random = new-Object System.Random
	$randonString = ""
	$dummy = $null
	For ($i=0; $i -lt $maxLength; $i++)  
	{
		$index = $random.Next($alpha.Length)
		$randomString += $array[$index] 
	}
	
		
	$maxLength = 2
	For ($i=0; $i -lt $maxLength; $i++)  
	{
		$index = $random.Next($alpha2.Length)
		$randomString += $array2[$index] 
	}
	
	return $randomString
}

function New-StorageAccount
{
    param([string]$StorageAcctName, [string]$Location, [string]$ResourceGroupName)

    az storage account create --location $Location --name $StorageAcctName --resource-group $ResourceGroupName --sku "Standard_LRS" --kind StorageV2

}

function Remove-AksCluster()
{
    param([string]$ClusterName, [string]$ResourceGroupName)

    $clusterLine = az aks list --query "[?contains(name, '$ClusterName')]" --output table
	if($clusterLine.Length -gt 0)
	{
		az aks delete --name $ClusterName --resource-group $ResourceGroupName --yes
	}
}

function Set-AppInsightsExtension()
{
    az extension show -n application-insights
    
    if($LASTEXITCODE -ne 0)
    {
		az extension add -n application-insights -y
    }
}

function Set-ApplyYaml
{
    param([string]$File, [string]$Namespace)

    $looper = $true
    while($looper)
    {
        kubectl apply -f $File -n $Namespace
        if($LASTEXITCODE -ne 0)
        {
            Write-Host "kubectl apply failed for $File. Waiting 10 seconds to try again..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
        }
        else
        {
            $looper = $false
        }
    }
}

function Set-Certificate
{
    param([string]$Dns, [string]$Location, [string]$Path, [string]$Destination, [string]$Namespace = "cert-manager")

        Copy-Item -Path $Path -Destination $Destination
	Update-Yaml -NewValue $Dns -MatchString "INGRESSDNS" -Filename $Destination
	Update-Yaml -NewValue $Location -MatchString "LOCATION" -Filename $Destination
	New-KubectlApply -Filename $Destination -Namespace $Namespace
	Remove-Item -Path $Destination
}


function Set-CleanOrleansStorageAccount
{
    param([string]$StorageAcctName)

    az storage container delete --name grainstate  --account-name $StorageAcctName 		
	az storage table delete --name OrleansSiloInstances --account-name $StorageAcctName 
	az storage table delete --name 'MetricsHourPrimaryTransactionsBlob' --account-name $StorageAcctName 
	az storage table delete --name 'MetricsHourPrimaryTransactionsFile' --account-name $StorageAcctName 
	az storage table delete --name 'MetricsHourPrimaryTransactionsQueue' --account-name $StorageAcctName 
	az storage table delete --name 'MetricsHourPrimaryTransactionsTable' --account-name $StorageAcctName
}

function Set-Ingress
{
    param([string]$Dns, [string]$Location, [string]$Path, [string]$Destination, [string]$Namespace = "kube-system")
    
    Copy-Item -Path $Path -Destination $Destination
	Update-Yaml -NewValue $Dns -matchString "INGRESSDNS" -filename $Destination
	Update-Yaml -newValue $Location -matchString "LOCATION" -filename $Destination
    New-KubectlApply -Filename $Destination -Namespace $Namespace
	Remove-Item -Path $Destination
}

function Set-IoTHubExtension()
{
	az extension show -n azure-cli-iot-ext
    
    if($LASTEXITCODE -ne 0)
    {
		az extension add -n azure-cli-iot-ext -y
    }
}

function Set-NodeLabel
{
    param([string]$NodeMatchValue, [string]$Key, [string]$Value)
    
    $looper = $true
    while($looper)
    {    
        $nodes = kubectl get nodes
        if($LASTEXITCODE -ne 0)
        {
            Write-Host "Waiting 10 seconds to get nodes from kubectl..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
        }
        else
        {
            foreach($node in $nodes)
            {
               $nodeVal = $node.Split(" ")[0]
               if($nodeVal.Contains($NodeMatchValue))
               {
		            kubectl label nodes $nodeVal "$Key=$Value"
                    if($LASTEXITCODE -ne 0)
                    {
                        Write-Host "Set node label failed. Waiting 10 seconds to try again..." -ForegroundColor Yellow
                        Start-Sleep -Seconds 10
                    }
                    else
                    {
                        $looper = $false
                    }
               }
            }
        }
    }
}

function Set-ResourceGroup
{
    param([string]$ResourceGroupName, [string]$Location)

    $rgoutcome = az group exists --name $ResourceGroupName
	
	if($rgoutcome -eq "false")
	{
		az group create --name $ResourceGroupName --location $Location 
	}	
}

function Set-Subscription
{
    param([string]$SubscriptionName)

    az account set --subscription "$SubscriptionName"
}

function Set-Timer
{
    param([string]$Message, [int]$Seconds)

    Write-Host $Message -ForegroundColor Yellow
    Start-Sleep -Seconds $Seconds
    
}

function Set-WaitForApiServices()
{
	$v = kubectl get apiservice
	$ft = $true
	while(([string]$v).IndexOf("False") -ne -1)
	{
		if($ft)
		{
			Write-Host("K8 metrics-server and/or cert-manager-webhook is offline right now. We'll keep waiting until they are online") -ForegroundColor Yellow
			$ft = $false
		}
		else
		{
			Write-Host("Waiting 60 secs for the K8 apiservices to come back online, yuck...") -ForegroundColor Yellow
		}
		Start-Sleep -Seconds 60
		$v = kubectl get apiservice
	}
}

function Update-MonitorIngressDns
{
    param([string]$Dns, [string]$Location, [string]$Path, [string]$Destination)
    
    Copy-Item -Path $Path -Destination $Destination
	Update-Yaml -NewValue $Dns -matchString "INGRESSDNS" -filename $Destination
	Update-Yaml -newValue $Location -matchString "LOCATION" -filename $Destination 
	
}

function Update-PublicIP
{
    param([string]$PublicIP, [string]$Dns, [string]$SubscriptionName)

    if($subscriptionNameOrId.Length -ne 0)
	{
	  az network public-ip update --ids $PublicIP --dns-name $Dns --subscription $SubscriptionName
	}
	else
	{
	  az network public-ip update --ids $PublicIP --dns-name $Dns
	}
}


function Update-Step
{
    param([int]$Step, [string]$Message, [DateTime]$Start)
    
		$endTime = Get-Date
		$timeSpan = New-TimeSpan -Start $Start -End $endTime
		$val = $timeSpan.ToString("mm\:ss")
		
        Write-Host "--Step $Step - $Message - $val" -ForegroundColor Green    
}

function Update-Yaml()
{
    Param ([string]$NewValue, [string]$MatchString, [string]$Filename)

    (Get-Content $Filename) -replace $MatchString,$NewValue | out-file $Filename -Encoding ascii
}
    param([string]$appName, [string]$rg, [string]$loc)

    $jsonAppString = az monitor app-insights component show --app $appName -g $rg
	$host.ui.RawUI.ForegroundColor = 'Gray'	
    if($LASTEXITCODE -ne 0)
    {
	    Write-Host "-- Step $step - Creating App Insights $appName" -ForegroundColor Green
			
	    $jsonAppString = az monitor app-insights component create -a $appName -l $loc -k other -g $rg --application-type other
	    $step++       
    }

    $appKey = New-RandomKey(8)
    $jsonKeyString = az monitor app-insights api-key create --api-key $appKey -g $resourceGroupName -a $appName
    $keyObj = ConvertFrom-Json -InputObject "$jsonKeyString"
    $instrumentationKey = $keyObj.apiKey	
	$host.ui.RawUI.ForegroundColor = 'Gray'
    return $instrumentationKey
}


function New-SampleConfig
{
    param([string]$DnsName, [string]$Location, [string]$Key)
    
    $authority = $DnsName.ToLower() + "." + $Location.ToLower() + ".cloudapp.azure.com"
    $url = "https://$authority"
    Write-Host "Using $url for management api" -ForegroundColor Yellow
   
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

function New-PiraeusCleanup
{
#cleanup script for source check in

    $path1 = "./deploy.json"
    $deploy = Get-Content -Raw -Path $path1 | ConvertFrom-Json
    $deploy.email = ""
    $deploy.dnsName = ""
    $deploy.location = ""
    $deploy.storageAcctName = ""
    $deploy.resourceGroupName = ""
    $deploy.subscriptionNameOrId = ""
    $deploy.appId = ""
    $deploy.pwd = ""
    $deploy.clusterName = "piraeuscluster"
    $deploy.nodeCount = 1
    $deploy.apiIssuer = "http://skunklab.io/mgmt"
    $deploy.apiAudience = "http://skunklab.io/mgmt"
    $deploy.apiSymmetricKey = "//////////////////////////////////////////8="
    $deploy.apiSecurityCodes = "12345678;87654321"
    $deploy.identityClaimType = "http://skunklab.io/name"
    $deploy.issuer = "http://skunklab.io/"
    $deploy.audience = "http://skunklab.io/"
    $deploy.symmetricKey = "//////////////////////////////////////////8="
    $deploy.tokenType = "JWT"
    $deploy.coapAuthority = "skunklab.io"
    $deploy.frontendVMSize = "Standard_D2s_v3"
    $deploy.orleansVMSize = "Standard_D4s_v3"
    $deploy | ConvertTo-Json -depth 100 | Out-File $path1
	

    $path2 = "../src/Samples.Mqtt.Client/config.json"
    $sampleConfig = Get-Content -Raw -Path $path2 | ConvertFrom-Json
    $sampleConfig.email = ""
    $sampleConfig.dnsName = ""
    $sampleConfig.location = ""
    $sampleConfig.storageAcctName = ""
    $sampleConfig.resourceGroupName = ""
    $sampleConfig.subscriptionNameOrId = ""
    $sampleConfig.appId = $null

    $sampleConfig.appId = $null
    $sampleConfig.pwd = $null
    $sampleConfig.clusterName = $null
    $sampleConfig.nodeCount = $null
    $sampleConfig.apiIssuer = $null
    $sampleConfig.apiAudience = $null
    $sampleConfig.apiSymmetricKey = $null
    $sampleConfig.apiSecurityCodes = $null
    $sampleConfig.identityClaimType = $null
    $sampleConfig.issuer = $null
    $sampleConfig.audience = $null
    $sampleConfig.symmetricKey = $null
    $sampleConfig.tokenType = $null
    $sampleConfig.coapAuthority = $null
    $sampleConfig.frontendVMSize = $null
    $sampleConfig.orleansVMSize = $null

    $sampleConfig | ConvertTo-Json -depth 100 | Out-File $path2 
  
}