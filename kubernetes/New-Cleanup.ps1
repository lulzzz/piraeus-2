function New-Cleanup() 
{
#cleanup script for source check in

    $path1 = "./deploy.json"
    $deploy = Get-Content -Raw -Path $path1 | ConvertFrom-Json
    $deploy.email = ""
    $deploy.dns = ""
    $deploy.location = ""
    $deploy.storageAcctName = ""
    $deploy.resourceGroupName = ""
    $deploy.subscriptionName = ""
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
    $sampleConfig.dns = ""
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


