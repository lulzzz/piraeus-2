function Set-Subscription
{
    param([string]$SubscriptionName)

    az account set --subscription "$SubscriptionName"
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

function Get-StorageAccountNameAvailable
{
    param([string]$StorageAcctName, [string]$SubscriptionName)

    $jsonString = az storage account check-name --name $StorageAcctName --subscription $SubscriptionName
    $jsonObj = ConvertFrom-Json -InputObject "$jsonString"
    return $jsonObj.nameAvailable
}

function Get-StorageAccountConnectionString
{
    param([string]$StorageAcctName, [string]$ResourceGroupName)

    $storageJsonString = az storage account show-connection-string --name $StorageAcctName --resource-group $ResourceGroupName
	$storageObj = ConvertFrom-Json -InputObject "$storageJsonString"
	return $storageObj.connectionString
}

function Get-TenantId
{
    param([string]$SubscriptionName)

    $acctObj = az account show --subscription "$SubscriptionName" | ConvertFrom-Json
    return $acctObj.tenantId
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

function New-RandomStorageAcctName
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

function New-RegisterApp
{
    param([string]$AppName, $ReplyUris)

    $adObj = az ad app list --filter "displayName eq '$AppName'" | ConvertFrom-Json

    if($adObj.appId.Length -ne 0)
    {
        return $adObj.appId
    }
    else
    {
        $regObj = az ad app create --display-name "$AppName" --native-app $false --reply-urls $ReplyUris | ConvertFrom-Json
        return $regObj.appId
    }
}

function New-StorageAccount
{
    param([string]$StorageAcctName, [string]$Location, [string]$ResourceGroupName)

    az storage account create --location $Location --name $StorageAcctName --resource-group $ResourceGroupName --sku "Standard_LRS" --kind StorageV2
}

function Set-AppInsightsExtension
{
    az extension show -n application-insights
    
    if($LASTEXITCODE -ne 0)
    {
		az extension add -n application-insights -y
    }
}

function Set-IoTHubExtension
{
	az extension show -n azure-cli-iot-ext
    
    if($LASTEXITCODE -ne 0)
    {
		az extension add -n azure-cli-iot-ext -y
    }
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

