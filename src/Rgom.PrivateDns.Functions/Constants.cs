using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rgom.PrivateDns.Functions
{
	internal class Constants
	{
		internal const string DnsEntitiesTableName = "DnsEntities";
		
		internal static Regex NicCaptureRegEx = new Regex("/networkInterfaces/(?<nic>[a-z0-9-.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex VmCaptureRegEx = new Regex("/virtualMachines/(?<vm>[a-z0-9-.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex PrivateEndpointCaptureRegEx = new Regex("/privateEndpoints/(?<privateEndpoint>[a-z0-9-.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex ResourceGroupCaptureRegEx = new Regex("/resourcegroups/(?<resourcegroup>[a-z0-9-]+)/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

		/// Still missing:
		/// 
		/// privatelink.monitor.azure.com
		/// privatelink.oms.opinsights.azure.com
		/// privatelink.ods.opinsights.azure.com
		/// privatelink.agentsvc.azure-automation.com
		internal static readonly Dictionary<string, string> ZoneMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(@"
		{
			""Microsoft.ContainerService/managedClusters/management"": ""privatelink.{0}.azmk8s.io"",
			""Microsoft.RecoveryServices/vaults/vault"": ""privatelink.{0}.backup.windowsazure.com"",
			""Microsoft.StorageSync/storageSyncServices/afs"": ""privatelink.afs.azure.net"",
			""Microsoft.MachineLearningServices/workspaces/workspace"": ""privatelink.api.azureml.ms"",
			""Microsoft.AppConfiguration/configurationStores/configurationStore"": ""privatelink.azconfig.io"",
			""Microsoft.Automation/automationAccounts/Webhook"": ""privatelink.azure-automation.net"",
			""Microsoft.Automation/automationAccounts/DSCAndHybridWorker"": ""privatelink.azure-automation.net"",
			""Microsoft.ContainerRegistry/registries/registry"": ""privatelink.azurecr.io"",
			""Microsoft.Devices/IotHubs/iotHub"": ""privatelink.azure-devices.net"",
			""Microsoft.Web/sites/sites"": ""privatelink.azurewebsites.net"",
			""Microsoft.Storage/storageAccounts/blob"": ""privatelink.blob.core.windows.net"",
			""Microsoft.DocumentDB/databaseAccounts/Cassandra"": ""privatelink.cassandra.cosmos.azure.com"",
			""Microsoft.CognitiveServices/accounts/account"": ""privatelink.cognitiveservices.azure.com"",
			""Microsoft.Sql/servers/SqlServer"": ""privatelink.database.windows.net"",
			""Microsoft.Storage/storageAccounts/dfs"": ""privatelink.dfs.core.windows.net"",
			""Microsoft.DocumentDB/databaseAccounts/SQL"": ""privatelink.documents.azure.com"",
			""Microsoft.EventGrid/topics/topic"": ""privatelink.eventgrid.azure.net"",
			""Microsoft.EventGrid/domains/domain"": ""privatelink.eventgrid.azure.net"",
			""Microsoft.Storage/storageAccounts/file"": ""privatelink.file.core.windows.net"",
			""Microsoft.DocumentDB/databaseAccounts/Gremlin"": ""privatelink.gremlin.cosmos.azure.com"",
			""Microsoft.DBforMariaDB/servers/mariadbServer"": ""privatelink.mariadb.database.azure.com"",
			""Microsoft.DocumentDB/databaseAccounts/MongoDB"": ""privatelink.mongo.cosmos.azure.com"",
			""Microsoft.DBforMySQL/servers/mysqlServer"": ""privatelink.mysql.database.azure.com"",
			""Microsoft.DBforPostgreSQL/servers/postgresqlServer"": ""privatelink.postgres.database.azure.com"",
			""Microsoft.Storage/storageAccounts/queue"": ""privatelink.queue.core.windows.net"",
			""Microsoft.Search/searchServices/searchService"": ""privatelink.search.windows.net"",
			""Microsoft.SignalRService/SignalR/signalR"": ""privatelink.service.signalr.net"",
			""Microsoft.EventHub/namespaces/namespace"": ""privatelink.servicebus.windows.net"",
			""Microsoft.Relay/namespaces/namespace"": ""privatelink.servicebus.windows.net"",
			""Microsoft.Synapse/workspaces/Sql"": ""privatelink.sql.azuresynapse.net"",
			""Microsoft.Storage/storageAccounts/table"": ""privatelink.table.core.windows.net"",
			""Microsoft.DocumentDB/databaseAccounts/Table"": ""privatelink.table.cosmos.azure.com"",
			""Microsoft.KeyVault/vaults/vault"": ""privatelink.vaultcore.azure.net"",
			""Microsoft.Storage/storageAccounts/web"": ""privatelink.web.core.windows.net""
		}");

		internal const string PrivateDnsZoneGroupsArmTemplateFormat = @"
			{{
				""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
				""contentVersion"": ""1.0.0.0"",
				""parameters"": {{}},
				""resources"": [
					{{
						""type"": ""Microsoft.Network/privateEndpoints/privateDnsZoneGroups"",
						""apiVersion"": ""2020-05-01"",
						""name"": ""{0}/default"",
						""properties"": {{
							""privateDnsZoneConfigs"": [
								{{
									""name"": ""default"",
									""properties"": {{
										""privateDnsZoneId"": ""{1}""
									}}
								}}
							]
						}}
					}}
				]
			}}
		";

		internal const string PrivateDnsZoneGroupsArmTemplate = @"
			{
				""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
				""contentVersion"": ""1.0.0.0"",
				""parameters"": {
					""privateEndpointName"": {
						""type"": ""string""
					},
					""privateDnsZoneResourceId"": {
						""type"": ""string""
					}
				},
				""variables"": {
					""privateDnsZoneGroupsName"": ""[concat(parameters('privateEndpointName'), '/default')]""
				},
				""resources"": [
					{
						""type"": ""Microsoft.Network/privateEndpoints/privateDnsZoneGroups"",
						""apiVersion"": ""2020-05-01"",
						""name"": ""[variables('privateDnsZoneGroupsName')]"",
						""properties"": {
							""privateDnsZoneConfigs"": [
								{
									""name"": ""default"",
									""properties"": {
										""privateDnsZoneId"": ""[parameters('privateDnsZoneResourceId')]""
									}
								}
							]
						}
					}
				]
			}
		";
	}
}
