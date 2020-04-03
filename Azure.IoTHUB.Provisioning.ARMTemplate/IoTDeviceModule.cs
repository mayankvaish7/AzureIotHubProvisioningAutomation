/*=============================================================================
 |   Assignment:  Azure IoTHub Soft Dummy Devices Creation
 |       Author:  Mayank Vaish - mayankvaish1@gmail.com
 |
 |  Description:  This Code will create Soft dummy devices and send dummy messages to Azure IoTHub
 |
 |     Language:  C#
 | Ex. Packages:  Microsoft.Azure.Devices
                    Microsoft.Azure.Management.ResourceManager
                    Newtonsoft.Json.Linq
                    Microsoft.IdentityModel.Clients
 |                
 | Deficiencies:  
 *===========================================================================*/

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Azure.IoTHub.ARMTemplateProvisioning;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Message = Microsoft.Azure.Devices.Client.Message;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Azure.IoTHUB.Provisioning.ARMTemplate
{
    public class IoTDeviceModule
    {
        private Microsoft.Azure.Management.Fluent.IAzure _azure;
        private string _token;
        private string _rgName;
        private string _deploymentName;
        private string _subscriptionId;
        private static TransportType s_transportType = TransportType.Amqp;
        private IoTDeviceModule()
        {

        }
        public IoTDeviceModule(Microsoft.Azure.Management.Fluent.IAzure azure, string token, string rgName, string DeploymentName, string subscriptionId)
        {
            _azure = azure;
            _token = token;
            _rgName = rgName;
            _deploymentName = DeploymentName;
            _subscriptionId = subscriptionId;
        }

        public JObject CreateIoTHub()
        {
            try
            {
                //Create IoT Hub Route to Servicebus - Route your telemetry and device messages to Azure Service Bus and add publisher and subscriber capability.
                var templateJsonToCreateIoTHub = JObject.Parse(File.ReadAllText(HttpContext.Current.Server.MapPath("~/Asset/Step1ArmTemplateCreateIoTHUB.json")));
                var ioTHubDeploymentResponse = _azure.Deployments.Define(_deploymentName)
                    .WithExistingResourceGroup(_rgName)
                    .WithTemplate(templateJsonToCreateIoTHub)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .Create();

                System.Collections.Generic.IEnumerable<Microsoft.Azure.Management.ResourceManager.Fluent.IDeployment> ioTHubDeploymentListResourceGroup =
                    _azure.Deployments.ListByResourceGroup(_rgName);

                //Create IoTHub Connection String
                var ioTHubDeploymentEnumerator = ioTHubDeploymentListResourceGroup.GetEnumerator();
                ioTHubDeploymentEnumerator.MoveNext();
                var outputvalue = ioTHubDeploymentEnumerator.Current;
                JObject jObject = JObject.Parse(ioTHubDeploymentEnumerator.Current.Outputs.ToString());

                return jObject;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task AddIoTDevice(string deviceId, string ioTConnectionstring)
        {
            try
            {
                //Register device into IoT hub 
                Device device;
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString("ioTConnectionstring");
                device = await registryManager.AddDeviceAsync(new Device(deviceId));

                DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(ioTConnectionstring, s_transportType);
                if (deviceClient == null)
                {
                    Console.WriteLine("Failed to create DeviceClient!");
                }
                else
                {
                    var sample = new MessageSample(deviceClient);
                    sample.RunSampleAsync().GetAwaiter().GetResult();
                }
            }
            catch (DeviceAlreadyExistsException)
            {
            }
            catch (Exception)
            {
                Utilities.Log("Error while adding Devie: " + deviceId);
            }
        }

        public async Task<DeviceClient> AddDeviceAsync(string deviceId, string ioTConnectionstring)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(ioTConnectionstring);
            Device device; DeviceClient deviceClient = null;
            try
            {
                //Add Device to IoTHub
                var d = new Device(deviceId);
                device = await registryManager.AddDeviceAsync(d);
                deviceClient = DeviceClient.CreateFromConnectionString(String.Format(ioTConnectionstring + ";DeviceId={0}", deviceId), s_transportType);
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.
                GetDeviceAsync(deviceId);
            }
            return deviceClient;
        }

        public async Task<Boolean> SendMicDisconnectedMessage(DeviceClient deviceClient, int messageCount, int intervalInSecond)
        {
            try
            {
                //Send AMQP Message from Device to IoTHub
                if (deviceClient != null)
                {
                    String msg = "Hello from AMQP Device - ";
                    for (int i = 0; i < messageCount; i++)
                    {
                        Random rand = new Random();
                        string current = msg + " " + DateTime.Now.ToLocalTime() + " - " + rand.Next().ToString();
                        var message = new Message(Encoding.ASCII.GetBytes(current));
                        message.Properties.Add("DeviceMessage", "Telemetry");
                        await deviceClient.SendEventAsync(message);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<Boolean> SendDisplayDisconnectedMessage(DeviceClient deviceClient, int messageCount, int intervalInSecond)
        {
            try
            {
                //Send AMQP Message from Device to IoTHub
                if (deviceClient != null)
                {
                    String msg = "Hello from AMQP Device - ";
                    for (int i = 0; i < messageCount; i++)
                    {
                        Random rand = new Random();
                        string current = msg + " " + DateTime.Now.ToLocalTime() + " - " + rand.Next().ToString();
                        var message = new Message(Encoding.ASCII.GetBytes(current));
                        message.Properties.Add("DeviceMessage", "Telemetry");
                        await deviceClient.SendEventAsync(message);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<Boolean> SendSpeakerDisconnectedMessage(DeviceClient deviceClient, int messageCount, int intervalInSecond)
        {
            try
            {
                //Send AMQP Message from Device to IoTHub
                if (deviceClient != null)
                {
                    String msg = "Hello from AMQP Device - ";
                    for (int i = 0; i < messageCount; i++)
                    {
                        Random rand = new Random();
                        string current = msg + " " + DateTime.Now.ToLocalTime() + " - " + rand.Next().ToString();
                        var message = new Message(Encoding.ASCII.GetBytes(current));
                        message.Properties.Add("DeviceMessage", "Telemetry");
                        await deviceClient.SendEventAsync(message);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<Boolean> SendNetworkDisconnectedMessage(DeviceClient deviceClient, int messageCount, int intervalInSecond)
        {
            try
            {
                //Send AMQP Message from Device to IoTHub
                if (deviceClient != null)
                {
                    String msg = "Hello from AMQP Device - ";
                    for (int i = 0; i < messageCount; i++)
                    {
                        Random rand = new Random();
                        string current = msg + " " + DateTime.Now.ToLocalTime() + " - " + rand.Next().ToString();
                        var message = new Message(Encoding.ASCII.GetBytes(current));
                        message.Properties.Add("DeviceMessage", "Telemetry");
                        await deviceClient.SendEventAsync(message);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<Boolean> SendWebcamDisconnectedMessage(DeviceClient deviceClient, int messageCount, int intervalInSecond)
        {
            try
            {
                //Send AMQP Message from Device to IoTHub
                if (deviceClient != null)
                {
                    String msg = "Hello from AMQP Device - ";
                    for (int i = 0; i < messageCount; i++)
                    {
                        Random rand = new Random();
                        string current = msg + " " + DateTime.Now.ToLocalTime() + " - " + rand.Next().ToString();
                        var message = new Message(Encoding.ASCII.GetBytes(current));
                        message.Properties.Add("DeviceMessage", "Telemetry");
                        await deviceClient.SendEventAsync(message);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

    }
}
