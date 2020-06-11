using Azure.IoTHUB.Provisioning.ARMTemplate;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;

namespace Azure.IoTHub.ARMTemplateProvisioning.Controllers
{
    public class AzureIoTHubProvisionController : ApiController
    {
        // GET api/AzureIoTHubProvision/{IoTHubCode}
        [HttpGet, Route("api/AzureIoTHubProvision")]
        public async Task<IHttpActionResult> Get(string IoTHubAutomationCode, string ResourceGroupName)
        {
            //http://localhost:3642/api/AzureIoTHubProvision?IoTHubAutomationCode=XXXXXXXX&ResourceGroupName=XXXXXXXX

            //=================================================================
            //Azure CLI --  > az account set --subscription XXXXXX-Name Or ID
            //az ad sp create-for-rbac --sdk-auth
            //Above commands will give on Azure CLI will give "ClientId", "TenantId", "ClientValue", "SubscriptionId"

            string elapsedTime = "";
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            if (WebConfigurationManager.AppSettings["IoTHUBCode"].Equals(IoTHubAutomationCode))
            {
                string clientId = WebConfigurationManager.AppSettings["ClientId"];
                string tenantId = WebConfigurationManager.AppSettings["TenantId"];
                string clientValue = WebConfigurationManager.AppSettings["ClientValue"];
                string subscriptionId = WebConfigurationManager.AppSettings["SubscriptionId"];
                string ioTConnectionString = string.Empty;
                var accessToken = await GetAccessToken(tenantId, clientId, clientValue);
                var creds = new AzureCredentialsFactory().FromServicePrincipal(clientId, clientValue, tenantId, AzureEnvironment.AzureGlobalCloud);
                Microsoft.Azure.Management.Fluent.IAzure azure = Microsoft.Azure.Management.Fluent.Azure
                       .Configure()
                       .WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                       .Authenticate(creds).WithSubscription(subscriptionId);

                var rgName = ResourceGroupName;
                var deploymentName = "Sample1" + DateTime.Now.Day.ToString().Split(' ')[0].Trim();

                try
                {
                    if (azure.ResourceGroups.Contain(rgName) == true)
                    {
                        //Cleanup Resourcegroup - 3 Mins
                        azure.ResourceGroups.DeleteByName(rgName);
                    }

                    //************************Step1 - Create ResourceGroup ***********************************
                    var deploymentRG = azure.ResourceGroups.Define(rgName)
                        .WithRegion(Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region.USWest)
                        .Create();

                    if (azure.ResourceGroups.Contain(rgName) && !string.IsNullOrEmpty(accessToken))
                    {
                        IoTDeviceModule ioTDeviceModule = new IoTDeviceModule(azure, accessToken, rgName, deploymentName, subscriptionId);
                        //************************Step2 - Start Create IoTHub -- 15 Mins *************************************
                        JObject jObject = CreateIoTHub(subscriptionId, accessToken, azure, rgName, deploymentName, ioTDeviceModule);

                        ioTConnectionString = string.Format("HostName={0}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey={1}", (jObject).GetValue("ioTHubName")["value"],
                                            (jObject).GetValue("iotHubKeys")["value"]["value"][0]["primaryKey"]);

                        Boolean IsGetDevicesToSendMessages = await CreateDeviceandSendMessages(ioTConnectionString, ioTDeviceModule);

                        stopWatch.Stop();
                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan ts = stopWatch.Elapsed;

                        // Format and display the TimeSpan value.
                        elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                            ts.Hours, ts.Minutes, ts.Seconds,
                                            ts.Milliseconds / 10);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return await Task.FromResult(Ok(new { status = "IoTHub Created Successfully - " + elapsedTime }));
        }

        private async Task<Boolean> CreateDeviceandSendMessages(string ioTConnectionString, IoTDeviceModule ioTDeviceModule)
        {
            try
            {
                //Create Device
                DeviceClient deviceClient;
                Random rnd = new Random();
                for (int j = 0; j <= 10; j++)
                {
                    deviceClient = await ioTDeviceModule.AddDeviceAsync("DeviceID" + j.ToString(), ioTConnectionString);
                    var docresponse = "Test Messagefrom DeviceId" + j.ToString();

                    //Send upto 2  Messages to IoTHub
                    for (int i = 0; i <= 2; i++)
                    {
                        //Send AMQP Message from Device to IoTHub
                        if (deviceClient != null)
                        {
                            var message = new Message(Encoding.ASCII.GetBytes(docresponse));
                            message.Properties.Add("DeviceMessage", "TelemetryData");
                            message.Properties.Add("NewMessage", "MessageData1");
                            await deviceClient.SendEventAsync(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        private static JObject CreateIoTHub(string subscriptionId, string accessToken, Microsoft.Azure.Management.Fluent.IAzure azure, string rgName, string deploymentName, IoTDeviceModule ioTDeviceModule)
        {
            return ioTDeviceModule.CreateIoTHub();
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        private static async Task<string> GetAccessToken(string tenantId, string clientId, string clientKey)
        {
            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext
                .AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            string token = result.AccessToken;
            return token;
        }
    }
}