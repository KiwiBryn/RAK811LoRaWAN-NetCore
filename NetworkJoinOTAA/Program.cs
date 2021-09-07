//---------------------------------------------------------------------------------
// Copyright (c) September 2021, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.NetCore.Rak811.NetworkJoinOTAA
{
	using System;
	using System.Diagnostics;
	using System.IO.Ports;
	using System.Threading;

	public class Program
	{
		private const string SerialPortId = "/dev/ttyS0";
		private const string AppEui = "...";
		private const string AppKey = "...";
		private const byte MessagePort = 1;
		private const string Payload = "A0EEE456D02AFF4AB8BAFD58101D2A2A"; // Hello LoRaWAN

		public static void Main()
		{
			string response;

			Debug.WriteLine("devMobile.IoT.NetCore.Rak811.NetworkJoinOTAA starting");

			Debug.WriteLine(String.Join(",", SerialPort.GetPortNames()));

			try
			{
				using (SerialPort serialPort = new SerialPort(SerialPortId))
				{
					// set parameters
					serialPort.BaudRate = 9600;
					serialPort.DataBits = 8;
					serialPort.Parity = Parity.None;
					serialPort.StopBits = StopBits.One;
					serialPort.Handshake = Handshake.None;

					serialPort.ReadTimeout = 5000;

					serialPort.NewLine = "\r\n";

					serialPort.Open();

					// clear out the RX buffer
					response = serialPort.ReadExisting();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
					Thread.Sleep(500);

					// Set the Working mode to LoRaWAN
					Console.WriteLine("Set Work mode");
					serialPort.WriteLine("at+set_config=lora:work_mode:0");
					Thread.Sleep(5000);
					response = serialPort.ReadExisting();
					response = response.Trim('\0');
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					// Set the Region to AS923
					Console.WriteLine("Set Region");
					serialPort.WriteLine("at+set_config=lora:region:AS923");
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					// Set the JoinMode
					Console.WriteLine("Set Join mode");
					serialPort.WriteLine("at+set_config=lora:join_mode:0");
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					// Set the appEUI
					Console.WriteLine("Set App Eui");
					serialPort.WriteLine($"at+set_config=lora:app_eui:{AppEui}");
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					// Set the appKey
					Console.WriteLine("Set App Key");
					serialPort.WriteLine($"at+set_config=lora:app_key:{AppKey}");
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					// Set the Confirm flag
					Console.WriteLine("Set Confirm off");
					serialPort.WriteLine("at+set_config=lora:confirm:0");
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
					
					// Join the network
					Console.WriteLine("Start Join");
					serialPort.WriteLine("at+join");
					Thread.Sleep(10000);
					response = serialPort.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					while (true)
					{
						Console.WriteLine("Sending");
						serialPort.WriteLine($"at+send=lora:{MessagePort}:{Payload}");
						Thread.Sleep(1000);

						// The OK
						Console.WriteLine("Send result");
						response = serialPort.ReadLine();
						Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

						// The Signal strength information etc.
						Console.WriteLine("Network confirmation");
						response = serialPort.ReadLine();
						Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

						Thread.Sleep(20000);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
}
