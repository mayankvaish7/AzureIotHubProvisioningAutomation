// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace Azure.IoTHub.ARMTemplateProvisioning
{
	public class MessageSample
	{
		private const int MessageCount = 1;
		private const int TemperatureThreshold = 30;
		private static Random s_randomGenerator = new Random();
		private float _temperature;
		private float _humidity;
		private DeviceClient _deviceClient;

		public MessageSample(DeviceClient deviceClient)
		{
			_deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
		}

		public MessageSample(string deviceConnectionString)
		{
			_deviceClient = string.IsNullOrEmpty(deviceConnectionString) ? throw new ArgumentNullException(nameof(deviceConnectionString)) : DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);
		}
		public MessageSample(string deviceConnectionString, bool mqtt)
		{
			_deviceClient = string.IsNullOrEmpty(deviceConnectionString) ? throw new ArgumentNullException(nameof(deviceConnectionString)) : DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
		}

		public async Task SendMessage()
		{
			var payload = "{" +
			"\"sensorState\":\"TestMessage\", " +
			"\"localTimestamp\":\"" + DateTime.Now.ToLocalTime() + "\"" +
			"}";

			var msg = new Message(Encoding.UTF8.GetBytes(payload));
			msg.ContentType = "application/json";   
			msg.ContentEncoding = "utf-8";
			msg.Properties["Status"] = "Active";
			await _deviceClient.SendEventAsync(msg);
		}

		public async Task RunSampleAsync()
		{
			await SendEvent().ConfigureAwait(false);
			await ReceiveCommands().ConfigureAwait(false);
		}

		private async Task SendEvent()
		{
			string dataBuffer;

			Console.WriteLine("Device sending {0} messages to IoTHub...\n", MessageCount);

			for (int count = 0; count < MessageCount; count++)
			{
				_temperature = s_randomGenerator.Next(20, 35);
				_humidity = s_randomGenerator.Next(60, 80);
				dataBuffer = $"{{\"messageId\":{count},\"temperature\":{_temperature},\"humidity\":{_humidity}}}";
				Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
				eventMessage.Properties.Add("temperatureAlert", (_temperature > TemperatureThreshold) ? "true" : "false");
				Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

				await _deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
			}
		}

		public async Task ReceiveCommands()
		{
			Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
			Console.WriteLine("Use the IoT Hub Azure Portal to send a message to this device.\n");

			Message receivedMessage;
			string messageData;

			receivedMessage = await _deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (receivedMessage != null)
			{
				messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
				Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

				int propCount = 0;
				foreach (var prop in receivedMessage.Properties)
				{
					Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
				}

				await _deviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
			}
			else
			{
				Console.WriteLine("\t{0}> Timed out", DateTime.Now.ToLocalTime());
			}
		}
	}
}
