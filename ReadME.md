


# Piraeus
## Introduction
Getting the right information to the right place at the right time is a difficult task in highly distributed environments.  Piraeus simplifies how heterogenous subsystems can interact statically, dynamically, and organically using an open-systems approach to real-time communications. Simplicity is the key where standard channels and protocols are supported with no coupling between subsystems.  The technology utilizes Microsoft Orleans to facilate on-demand routes for information delivery and Claims Authorization Policy Language (CAPL) for fine-grain access control between senders and receivers of messages.  The low latency and linearly scalable technology means you can build distributed systems, even complex systems, with simplicity and have real-time communications that scale.

The technology is designed to run on docker containers and the getting started sample show you how to get up and running in minutes on Azure AKS.

- For Management API using PowerShell v6 see [here](/docs/MgmtApi.md)

- For custom production deployments see [here](/docs/deployconfig.md)

![Architecture](/docs/arch.jpg)
[Deployment Details](/docs/deploydetail.md)
## Getting Started
See [here](/docs/deploy.md) for more details on deploying Piraues.

**Quick Start** 
 1. Clone the source
 2. Run the "build.cmd" from a command prompt in the "src" folder.
 3. Ensure the [prereqs](/docs/prereqs.md) are installed. 
 4. Deploy Piraeus to Azure AKS
 5. Configure Piraeus
 6. Run a sample client
 
 ### Deploying Piraeus Quick Start
 
 1. . Open a command prompt and navigate to the /src folder and type "build". This will build the source and make the Piraeus PowerShell commands available.
 2.  Navigate to the /kubernetes folder using the command prompt.
 3. Type *pwsh* to get a PowerShell command prompt 
 4. Type "az login" and login Azure.
 5. Type ". ./New-PiraeusDeploy.ps1" to load the PowerShell command.
 6. Execute the following command "New-PiraeusDeploy" with the following parameters
 -  *-SubscriptionName*  Name of Azure subscription to do the deployment.
 -  *-ResourceGroupName*  Name of the Resoure Group for the deployment.
 -  *-Email* Your email address, which is necessary for the Let's Encrypt certificates (limited 50 dns names per email address per week)
 -  *-Dns* Dns name for the deployment, which can be used only  1 time for each new deployment, e.g., "flyingdogs42".  Note:  Do not reuse and previously used Dns name with this deployment.
 -  *-Location* Azure data center location, e.g., "eastus"
 -  *-StorageAcctName* The name of a storage account to be created. Note: Do not use an existing storage account.

FQDN of the Piraeus deployment will be:
```<dns>.<location>.cloudapp.azure.com```

The command will produce a configuration output file in the "kubernetes" folder in the form "**mm-dd-yyyyThh-mm-ss.json**"

A sample will be automatically configured in Piraeus and a  configuration file will be written to Samples.Mqtt.Client project.  

**Running the Sample**
Run 2 instances of the Samples.Mqtt.Client project.  Use the "use file [y]" option when prompted.  Type in different client "names" in each of the 2 instances and select role "A" for one instance and "B" for the other when prompted.  Now, your 2 instances can communicate with each other.

