#AKS Functions

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

function Add-NodePool
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

function Get-AksCredentials
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

function Remove-AksCluster
{
    param([string]$ClusterName, [string]$ResourceGroupName)

    $clusterLine = az aks list --query "[?contains(name, '$ClusterName')]" --output table
	if($clusterLine.Length -gt 0)
	{
		az aks delete --name $ClusterName --resource-group $ResourceGroupName --yes
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

function Set-Ingress
{
    param([string]$Dns, [string]$Location, [string]$Path, [string]$Destination, [string]$Namespace = "kube-system")
    
    Copy-Item -Path $Path -Destination $Destination
	Update-Yaml -NewValue $Dns -matchString "INGRESSDNS" -filename $Destination
	Update-Yaml -newValue $Location -matchString "LOCATION" -filename $Destination
    New-KubectlApply -Filename $Destination -Namespace $Namespace
	Remove-Item -Path $Destination
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

function Set-Timer
{
    param([string]$Message, [int]$Seconds)

    Write-Host $Message -ForegroundColor Yellow
    Start-Sleep -Seconds $Seconds    
}

function Set-WaitForApiServices
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

function Update-Yaml
{
    Param ([string]$NewValue, [string]$MatchString, [string]$Filename)

    (Get-Content $Filename) -replace $MatchString,$NewValue | out-file $Filename -Encoding ascii
}