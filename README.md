# Private DNS Functions

An Azure Function that responds to `Microsoft.Resources.ResourceWriteSuccess` and `Microsoft.Resources.ResourceDeleteSuccess` events for Private Endpoints and Network Interfaces to automatically add DNS recordsets in the appropriate Private DNS Zone.

This is useful for enabling automatic DNS registration at scale across any number of Subscriptions and across regions. Additionally, the ability to tag a NIC with a hostname streamlines the creation of DNS entries for resources such as internal load balancers or providing alternate hostnames for VMs.

# Setup & Usage

There are three steps for setting up this solution:

1. Deploy the Azure Function using the included template (`./templates/azuredeploy.function.json`).
1. Create a `Private DNS Zone Contributor` role assignment for the Azure Function's system managed identity over the scope of your private DNS zones. You'll also need to create a `Reader` role assignment for the Azure Function's system managed identity for the entire scope of your environment so it can get Private Endpoints and Network Interfaces.
1. Create the system topic Event Grid subscriptions to be handled by the newly deployed Azure Function. For increased scalability, this template should be incorporated into a baseline Blueprint assigned to your subscriptions.

## Assumptions
1. All private DNS zones in your environment are consolidated in a single Resource Group
1. Your environment utilizes a single private DNS zone VMs and Load Balancers (i.e. non-PrivateEndpoint resources)

If your environment doesn't have the Private Endpoint private DNS zones created, consider utilizing this template to deploy the DNZ zones an optionally linking to a Virtual Network:

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.privatedns.json)  [![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.privatedns.json)


## 1. Deploy the Azure Function

Use the Deploy to Azure button below to begin deployment of the Azure Function. You will need to provide the following parameter values:

- location
    - Defaults to Resource Group location
- name
    - Name for all the resources to be created
- tenantId
    - Defaults to subscription tenant
- privateDnsSubscriptionId
    - ID of the Subscription which contains your environment's private DNS zones
    - Defaults to subscription to which the template is being deployed
- privateDnsResourceGroupName
    - Name of the Resource Group which contains your environment's private DNS zones
- defaultPrivateDnsZone
    - The name of your private DNS zone for Azure VMs and internal load balancers
    - Ex: azure.mycompany.net
- hostnameTagName
    - Tag name to use for specifying hostnames on Network Interface resources
- zipPackageUrl
    - URL to MSDeploy zip file of the code in this repo
    - Defaults to the latest CI release of this repo

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.function.json)  [![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.function.json)

## 2. Create Role Assignments

Use one of the following guides for creating a `Private DNS Zone Contributor` role assignment for the newly deploy Azure Function's system managed identity over the private DNS zone scope and a `Reader` role assignment for the Azure Function's system managed identity for the entire scope of your environment:

- [Add or remove Azure role assignments using the Azure portal](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal)
- [Add or remove Azure role assignments using Azure PowerShell](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-powershell)
- [Add or remove Azure role assignments using Azure CLI](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-cli)

## 3. Create the System Topic Event Grid Subscriptions

The last step is to create the system topic Event Grid subscriptions in all the Azure Subscriptions you'd like to be handled by the Azure Function. As previously mentioned, you should consider including this template in a baseline Blueprint. [Quickstart: Define and assign a blueprint in the portal](https://docs.microsoft.com/en-us/azure/governance/blueprints/create-blueprint-portal)

In order to deploy this template, you will need the Azure Function Resource Id from step #1 (which is output from the ARM template deployment). 

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.systemtopics.json)  [![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Frjygraham%2FPrivateDnsFunctions%2Fmaster%2Ftemplates%2Fazuredeploy.systemtopics.json)

# Validation

In order to validate the setup is successful, create a Network Interface with a tag key matching the value supplied for the `hostnameTagName` parameter value. You should observe an A record added to the private DNS zone specified in the `defaultPrivateDnsZone` parameter value within a minute or two of successful NIC creation.

Delete the Network Interface and observe the A record is successfully removed.

You can also validate Private Endpoint functionality by creating a new Private Endpoint for one of the currently supported services. Upon successful Private Endpoint creation, you should observe an A record added to the private DNS zone aligned to the Azure service. For more information, please see: [Azure Private Endpoint DNS Configuration](https://docs.microsoft.com/en-us/azure/private-link/private-endpoint-dns)

Deleting the Private Endpoint should result in the A record being removed from the private DNS zone.

# Limitations

This solution currently only supports the following Private Endpoints enabled Azure services:

- SQL DB (privatelink.database.windows.net)
- Azure Synapse Analytics (privatelink.database.windows.net)
- Storage Account / Blob (privatelink.blob.core.windows.net)
- Storage Account / Table (privatelink.table.core.windows.net)
- Storage Account / Queue (privatelink.queue.core.windows.net)
- Storage Account / File (privatelink.file.core.windows.net)
- Storage Account / Web (privatelink.web.core.windows.net)
- Data Lake File System Gen2 (privatelink.dfs.core.windows.net)
- Azure Cosmos DB / SQL (privatelink.documents.azure.com)
- Azure Cosmos DB / MongoDB (privatelink.mongo.cosmos.azure.com)
- Azure Cosmos DB / Cassandra (privatelink.cassandra.cosmos.azure.com)
- Azure Cosmos DB / Gremlin (privatelink.gremlin.cosmos.azure.com)
- Azure Cosmos DB / Table (privatelink.table.cosmos.azure.com)
- Azure Database for MySQL (privatelink.mysql.database.azure.com)
- Azure Database for MariaDB (privatelink.mariadb.database.azure.com)
- Azure Key Vault (privatelink.vaultcore.azure.net)
- Azure App Configuration (privatelink.azconfig.io)
- Azure Event Hub (privatelink.servicebus.windows.net)
- Azure Service Bus (privatelink.servicebus.windows.net)
- Azure Relay (privatelink.servicebus.windows.net)
- Azure WebApps (privatelink.azurewebsites.net)
- Azure Machine Learning (privatelink.api.azureml.ms)

Unsupported Private Endpoint enabled Azure Services:

- Azure Database for PostgreSQL (privatelink.postgres.database.azure.com)
- Azure Kubernetes Service - Kubernetes API (privatelink.{region}.azmk8s.io)
- Azure Search (privatelink.search.windows.net)
- Azure Container Registry (privatelink.azurecr.io)
- Azure Backup (privatelink.{region}.backup.windowsazure.com)
- Azure Event Grid / topic (privatelink.eventgrid.azure.net)
- Azure Event Grid / domain (privatelink.eventgrid.azure.net)

# Future Enhancements

1. Add outstanding Private Endpoint services.
1. Remove private DNS entries when VMs are deallocated. Add DNS entries when VM is started.

# License

Copyright 2020 Ryan Graham

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.