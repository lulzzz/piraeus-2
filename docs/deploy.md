
Deploying Piraeus
=================

The **prereqs ** for deployment can be found [here](./prereqs.md) and are required for successful deployment of the cluster.


Deployment Description
----------------------

The deployment will be made to an Azure AKS cluster and will take approx. 16-20
minutes. The following resources will be included in the deployment:

-   NGINX ingress controller with Let's Encrypt and cert-manager to obtain and
    manage certificates

-   Web socket server gateway

-   Orleans cluster

-   2 Azure storage accounts. 
	- A storage account for the Orleans grain state
	- Another storage account will be added automatically using the name of the 1st storage account and the word "audit"  appended.

Deployment Instructions
-----------------------
Please follow the prescription deployment instructions to successfully deploy and configure the Piraeus AKS cluster.

(1) Open a command prompt
(2) Navigate to the piraeus source "src" folder.
(3) Type "build". This will build the source and make the Management API
available.
(4) Navigate to "kubernetes" folder, e.g., “../kubernetes”
(5) Type "pwsh" to start PowerShell Core.
(6) Type ". ./NewPiraeusDeploy.ps1" to load the PowerShell script.
(7) Use the following PowerShell command “New-PiraeusDeploy” to run the script
and deploy Piraeus.

**Example Command**
```
 New-PiraeusDeploy -SubscriptionName "mysubscription" -ResourceGroupName "TestDeployments" \` -ClusterName "piraeuscluster1" -Email "alias\@email.com" -Dns "stingingbee1" -Location "eastus" \` -StorageAcctName "stingingbeestore" -NodeCount 1 -FrontendVMSize "Standard_D2s_v3" \` -OrleansVMSize "Standard_D4s_v3" -AppID "zz27e53t-y1re-b5g7-h342-h587621wwhyt" \` -Password "5387cdt-9g7t-3gj9-1234-7d6th7edf079" 
 ```


**Command**: New-PiraeusDeploy

| **Parameter**     | **Description**                                                                                                                                |
|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| SubscriptionName  | The name of the Azure subscription you will be deploying Piraeus.                                                                              |
| ResourceGroupName | The name of the Azure resource group the deployment will reside.                                                                               |
| ClusterName       | The name of the Piraeus AKS cluster. If omitted the default value is "piraeuscluster"                                                          |
| Email             | Your email address. This is used by Let's Encrypt to send you notifications on the free certificate that will be issued during the deployment. |
| Dns               | The dns name of the deployment, e.g., "flyingfrog23". The FQDN name will be \<dns\>.\<location\>.cloudapp.azure.com                            |
| Location          | The location of the Azure data center for the deployment, e.g., "eastus"                                                                       |
| StorageAcctName   | The name of the Orleans grain state storage account. The script will create a new storage account, if one does not exist.                      |
| NodeCount         | The number of nodes in the AKS cluster per node pool. If omitted, the default is 1.                                                            |
| FrontendVMSize    | The SKU for the frontend VM. If omitted, the default is "Standard_D2s_v3"                                                                      |
| OrleansVMSize     | The VM SKU for a node in the Orleans cluster. If omitted, the default is "Standard_D4s_v3"                                                     |
| AppID             | The application id of a service principal to use for the deployment. If omitted, a new service principal will be created.                      |
| Password          | The password of a service principal to use for the deployment. If omitted, a new service principal will be created.                            |

(8) The deployment will create to 2 π-systems for testing and automatically
configure the Sample.Mqtt.Client project in source.  Run 2 instances of the sample client choosing to use the configuration file [y] select role "A" for one client and role "B" for the other.  You will be able to send messages between the clients.
