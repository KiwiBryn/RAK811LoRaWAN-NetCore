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
			SerialPort serialDevice=null;
			string response;

			Debug.WriteLine("devMobile.IoT.NetCore.Rak811.NetworkJoinOTAA starting");

			Debug.WriteLine(String.Join(",", SerialPort.GetPortNames()));

			try
			{
				serialDevice = new SerialPort(SerialPortId);

				// set parameters
				serialDevice.BaudRate = 9600;
				serialDevice.DataBits = 8;
				serialDevice.Parity = Parity.None;
				serialDevice.StopBits = StopBits.One;
				serialDevice.Handshake = Handshake.None;

				serialDevice.ReadTimeout = 5000;

				serialDevice.Open();

				// clear out the RX buffer
				response = serialDevice.ReadExisting();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
				Thread.Sleep(500);

				// Set the Working mode to LoRaWAN
				Console.WriteLine("Set Work mode");
				serialDevice.Write("at+set_config=lora:work_mode:0\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadExisting();
				Debug.WriteLine($"RX :{response} bytes:{response.Length}");

				// Set the Region to AS923
				Console.WriteLine("Set Region");
				serialDevice.Write("at+set_config=lora:region:AS923\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

				// Set the JoinMode
				Console.WriteLine("Set Join mode");
				serialDevice.Write("at+set_config=lora:join_mode:0\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

				// Set the appEUI
				Console.WriteLine("Set App Eui");
				serialDevice.Write($"at+set_config=lora:app_eui:{AppEui}\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

				// Set the appKey
				Console.WriteLine("Set App Key");
				serialDevice.Write($"at+set_config=lora:app_key:{AppKey}\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
	
				// Set the Confirm flag
				Console.WriteLine("Set Confirm off");
				serialDevice.Write("at+set_config=lora:confirm:0\r\n");
				Thread.Sleep(500);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");
				Thread.Sleep(500);

				// Join the network
				Console.WriteLine("Start Join");
				serialDevice.Write("at+join\r\n");
				Thread.Sleep(10000);
				response = serialDevice.ReadLine();
				Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

				while (true)
				{
					serialDevice.Write($"at+send=lora:{MessagePort}:{Payload}\r\n");
					Thread.Sleep(500);
					//response = serialDevice.ReadExisting();
					response = serialDevice.ReadLine();
					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length}");

					Thread.Sleep(20000);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			finally
			{
				if ((serialDevice != null) && (serialDevice.IsOpen))
				{
					serialDevice.Close();
				}
			}
		}
	}
}
