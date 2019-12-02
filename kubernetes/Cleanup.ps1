function New-VrtuCleanSolution() 
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








    $path2 = "../src/AzureIoT.Deployment.Function/secrets.json"
    $deployFuncConfig = Get-Content -Raw -Path $path2 | ConvertFrom-Json
    $deployFuncConfig.hostname = ""
    $deployFuncConfig.storageConnectionString = ""
    $deployFuncConfig.tableName = ""
    $deployFuncConfig.serviceUrl = ""
    $deployFuncConfig.defaultTemplate = ""
    $deployFuncConfig.defaultIoTHubConnectionString = ""
    $deployFuncConfig | ConvertTo-Json -depth 100 | Out-File $path2    

    $path3 = "../src/VirtualRtu.Gateway/secrets.json"
    $gwConfig = Get-Content -Raw -Path $path3 | ConvertFrom-Json
    $gwConfig.hostname = ""
    $gwConfig.virtualRtuId = ""
    $gwConfig.instrumentationKey = ""
    $gwConfig.symmetricKey = ""
    $gwConfig.lifetimeMinutes = 0
    $gwConfig.storageConnectionString = ""
    $gwConfig.container = ""
    $gwConfig.filename = ""
    $gwConfig | ConvertTo-Json -depth 100 | Out-File $path3   

    $path4 = "../src/VirtualRtu.Configuration.Function/secrets.json"
    $funcConfig = Get-Content -Raw -Path $path4 | ConvertFrom-Json
    $funcConfig.symmetricKey = ""
    $funcConfig.apiToken = ""
    $funcConfig.lifetimeMinutes = 0
    $funcConfig.tableName = ""
    $funcConfig.storageConnectionString = ""
    $funcConfig.rtuMapContainer = ""
    $funcConfig.rtuMapFilename = ""
    $funcConfig | ConvertTo-Json -depth 100 | Out-File $path4
    
    $path5 = "./deploy.json"
    $deployConfig = Get-Content -Raw -Path $path5 | ConvertFrom-Json
    $deployConfig.virtualRtuId = ""
	$deployConfig.module.instrumentationKey = ""
    $deployConfig | ConvertTo-Json -depth 100 | Out-File $path5    

    $path6 = "../src/VirtualRtu.Module/Properties/launchSettings.json"
    $lsConfig = Get-Content -Raw -Path $path6 | ConvertFrom-Json
    $lsConfig.profiles.'VirtualRtu.Module'.environmentVariables.MODULE_CONNECTIONSTRING = ""
    $lsConfig | ConvertTo-Json -depth 100 | Out-File $path6
}