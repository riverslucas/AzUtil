using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Util;

namespace ConsoleApp3
{
    public class GraphClient
    {
        private readonly string TenantId = "";
        private readonly string ClientId = "";
        private readonly string Secret = "";
        public AccessToken ManagementToken { get; set; }
        public GraphClient(string tenantId, string clientID, string secret)
        {
            TenantId = tenantId;
            ClientId = clientID;
            Secret = secret;
        }

        public async Task<GraphServiceClient> GetClient()
        {
            var credetials = new ClientSecretCredential(TenantId, ClientId, Secret);
            var context = new TokenRequestContext(new string[] { "https://graph.microsoft.com//.default" });
            var token = await credetials.GetTokenAsync(context);

            var graph = new GraphServiceClient(new DelegateAuthenticationProvider((r) =>
            {
                r.Headers
                 .Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                ManagementToken = token;
                return Task.CompletedTask;
            }));
            return graph;
        }


        public async Task<Application> AddApplicationRegistration(Application item)
        {
            try
            {
                var client = await GetClient();
                item.RequiredResourceAccess = new List<RequiredResourceAccess>()
                {
                    new RequiredResourceAccess() {
                        ResourceAppId = "00000003-0000-0000-c000-000000000000",
                        ResourceAccess = new List<ResourceAccess>()
                        {
                            new ResourceAccess()
                            {
                                Id = new Guid("e1fe6dd8-ba31-4d61-89e7-88639da4683d"),
                                Type = "Scope",
                            }
                        }
                    }
                };
                var newApp = await client.Applications.Request().AddAsync(item);
                return newApp;
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<PasswordCredential> AddSecret(Application item)
        {
            var client = await GetClient();
            return await client.Applications[item.Id].AddPassword().Request().PostAsync();
        }

        public async Task DeleteApplication(string applicationId)
        {
            var client = await GetClient();
            if (await ApplicationExists(applicationId))
            {
                await client.Applications[applicationId].Request().DeleteAsync();
            }
            else
            {
                throw new Exception($"Resource with id {applicationId} NotFound");
            }
        }

        public async Task DeleteServicePrincipal(string servicePrincipalObjectId)
        {
            var client = await GetClient();
            if (await ServicePrincipalExists(servicePrincipalObjectId))
            {
                await client.ServicePrincipals[servicePrincipalObjectId].Request().DeleteAsync();
            }
            else
            {
                throw new Exception($"Resource with id {servicePrincipalObjectId} NotFound");
            }
        }

        public async Task<bool> ApplicationExists(string applicationId)
        {
            try
            {
                var client = await GetClient();
                var exist = await client.Applications[applicationId].Request().GetAsync();
                return true;
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> ServicePrincipalExists(string servicePrincipal)
        {
            try
            {
                var client = await GetClient();
                var exist = await client.ServicePrincipals[servicePrincipal].Request().GetAsync();
                return true;
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ApplicationResults> AddServicePrincipal(ServicePrincipal item)
        {
            Console.WriteLine($"Creates Application {item.DisplayName} ONLY");
            var client = await GetClient();
            var newApp = await AddApplicationRegistration(new Application()
            {
                DisplayName = item.DisplayName,
            });
            Console.WriteLine($"Creates ServicePrincipal using app ID  {newApp.AppId}");

            item.AppId = newApp.AppId;
            item.DisplayName = newApp.DisplayName;
            var newSp = await client.ServicePrincipals.Request().AddAsync(item);
            Console.WriteLine($"Returns both");
            return new ApplicationResults() { CurrentApplication = newApp, CurrentServicePrincipal = newSp };
        }

        public async Task AddOwners(string[] ownerList, Application application)
        {
            var client = await GetClient();
            foreach (var owner in ownerList)
            {
                var directoryObject = new DirectoryObject
                {
                    Id = owner
                };
                await client.Applications[application.Id].Owners.References
                    .Request()
                    .AddAsync(directoryObject);
            }
        }

        public async Task<string> CreateNewADGroup(string groupName, string tenantId)
        {
            try
            {
                var client = await GetClient();
                var newGroup = await client.Groups.Request().AddAsync(new Group()
                {
                    Description = "Some Description",
                    DisplayName = groupName,
                    MailEnabled = false,
                    MailNickname = $"Nickname_{groupName}",
                    SecurityEnabled = true
                });
                return groupName;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> GetAllRoles(string token)
        {
            var url = "https://management.azure.com/providers/Microsoft.Authorization/roleDefinitions?api-version=2022-04-01";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var results = await client.GetAsync(url);
            return await results.Content.ReadAsStringAsync();
        }


        public async Task<string> AddRbacToResourceGroup(ServicePrincipal sp, string tenantId, string subscriptionId, string myresourcegroup, string roleassignmentID)
        {
            try
            {
                var client = await GetClient();
                var scope = $"subscriptions/{subscriptionId}/resourceGroups/{myresourcegroup}";
                
                var guid = Guid.NewGuid().ToString();
                var url = $"https://management.azure.com/{scope}/providers/Microsoft.Authorization/roleAssignments/{guid}?api-version=2022-04-01";
                var payload = new AzureRoleDefinitionPayload()
                {
                    properties = new AzureRoleDefinitionPayloadProperties()
                    {
                        roleDefinitionId = $"/{scope}/providers/Microsoft.Authorization/roleDefinitions/{roleassignmentID}",
                        principalId = sp.Id,
                        principalType = "ServicePrincipal"
                    }
                };
                var token = await GetManagementAzureToken();
                using (var httpClient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(payload);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                    var response = await httpClient.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    throw new Exception($"{response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                }

            }
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<string> AddRbacToResource(ServicePrincipal sp, string tenantId, string subscriptionId, string myresourcegroup, string roleAssignmentID, string provider, string providerName)
        {
            //
            try
            {
                var client = await GetClient();
                var scope = $"subscriptions/{subscriptionId}/resourceGroups/{myresourcegroup}/providers/{provider}/{providerName}";
                
                var guid = Guid.NewGuid().ToString();
                var url = $"https://management.azure.com/{scope}/providers/Microsoft.Authorization/roleAssignments/{guid}?api-version=2022-04-01";
                var payload = new AzureRoleDefinitionPayload()
                {
                    properties = new AzureRoleDefinitionPayloadProperties()
                    {
                        roleDefinitionId = $"/{scope}/providers/Microsoft.Authorization/roleDefinitions/{roleAssignmentID}",
                        principalId = sp.Id,
                        principalType = "ServicePrincipal"
                    }
                };
                var token = await GetManagementAzureToken();
                using (var httpClient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(payload);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                    var response = await httpClient.PutAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    throw new Exception($"{response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                }

            }
            catch (Exception)
            {
                throw;
            }
        }


        private async Task<string> GetManagementAzureToken()
        {
            var creds = new ClientSecretCredential(TenantId, ClientId, Secret);
            var context = new TokenRequestContext(new string[] { "https://management.azure.com//.default" });
            var token = await creds.GetTokenAsync(context);
            return token.Token;
        }
    }
}
