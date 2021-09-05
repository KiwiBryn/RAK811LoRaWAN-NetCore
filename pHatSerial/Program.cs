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
namespace devMobile.IoT.NetCore.Rak811.pHatSerial
{
	using System;
	using System.Diagnostics;

	using System.IO.Ports;
	using System.Threading;

	public class Program
	{
		private const string SerialPortId = "/dev/ttyS0";

		public static void Main()
		{
			SerialPort serialDevice;

			Debug.WriteLine("devMobile.IoT.NetCore.Rak811.pHatSerial starting");

			Debug.WriteLine(String.Join(",", SerialPort.GetPortNames()));

			try
			{
				serialDevice = new SerialPort(SerialPortId);

				// set parameters
#if DEFAULT_BAUDRATE
				serialDevice.BaudRate = 115200;
#else
            serialDevice.BaudRate = 9600;
#endif
				serialDevice.Parity = Parity.None;
				serialDevice.DataBits = 8;
				serialDevice.StopBits = StopBits.One;
				serialDevice.Handshake = Handshake.None;

				serialDevice.Open();

#if DEFAULT_BAUDRATE
				Debug.WriteLine("RAK811 baud rate set to 9600");
				serialDevice.Write("at+set_config=device:uart:1:9600\r\n");
#endif

#if SERIAL_ASYNC_READ
				serialDevice.DataReceived += SerialDevice_DataReceived;
#endif

				while (true)
				{
					serialDevice.Write("at+version\r\n");

#if SERIAL_SYNC_READ
               Thread.Sleep(250);

					string response = serialDevice.ReadLine();

					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length} read from {serialDevice.PortName}");
#endif

					Thread.Sleep(20000);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

#if SERIAL_ASYNC_READ
		private static void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort serialDevice = (SerialPort)sender;

			switch (e.EventType)
			{
				case SerialData.Chars:
					string response = serialDevice.ReadExisting();

					Debug.WriteLine($"RX :{response.Trim()} bytes:{response.Length} read from {serialDevice.PortName}");
					break;

				case SerialData.Eof:
					Debug.WriteLine("RX :EoF");
					break;
				default:
					Debug.Assert(false, $"e.EventType {e.EventType} unknown");
					break;
			}
		}
#endif
	}
}