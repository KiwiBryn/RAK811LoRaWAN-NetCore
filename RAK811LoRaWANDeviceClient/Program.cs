//---------------------------------------------------------------------------------
// Copyright (c) Setpember 2021, devMobile Software
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
//  PAYLOAD_HEX vs. PAYLOAD_BYTES
//  OTAA vs. ABP
//  CONFIRMED
//  POWER_SAVE
//---------------------------------------------------------------------------------
namespace devMobile.IoT.LoRaWAN.NetCore.RAK811
{
	using System;
	using System.IO.Ports;
	using System.Threading;
	using System.Diagnostics;

	public class Program
	{
		private const string SerialPortId = "/dev/ttyS0";
		private const LoRaWANDeviceClass Class = LoRaWANDeviceClass.A;
		private const string Region = "AS923";
		private const int JoinTimeoutmSec = 25000;
		private const int SendTimeoutmSec = 10000;
		private static readonly TimeSpan MessageSendTimerDue = new TimeSpan(0, 0, 15);
		private static readonly TimeSpan MessageSendTimerPeriod = new TimeSpan(0, 5, 0);
		private static Timer MessageSendTimer;
		private const byte MessagePort = 1;
#if PAYLOAD_HEX
      private const string PayloadHex = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN in HEX
#endif
#if PAYLOAD_BYTES
		private static readonly byte[] PayloadBytes = { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x4c, 0x6f, 0x52, 0x61, 0x57, 0x41, 0x4e }; // Hello LoRaWAN in bytes
#endif

		public static void Main()
		{
			Result result;

			Debug.WriteLine("devMobile.IoT.NetCore.Rak811.Rak811LoRaWanDeviceClient starting");

			Debug.WriteLine($"Ports :{String.Join(",", SerialPort.GetPortNames())}");

			try
			{
				using (Rak811LoRaWanDevice device = new Rak811LoRaWanDevice())
				{
					result = device.Initialise(SerialPortId, 9600, Parity.None, 8, StopBits.One);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Initialise failed {result}");
						return;
					}

					MessageSendTimer = new Timer(MessageSendTimerCallback, device, Timeout.Infinite, Timeout.Infinite);

					device.OnJoinCompletion += OnJoinCompletionHandler;
					device.OnReceiveMessage += OnReceiveMessageHandler;
#if CONFIRMED
					device.OnMessageConfirmation += OnMessageConfirmationHandler;
#endif


					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Class {Class}");
					result = device.Class(Class);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Class failed {result}");
						return;
					}

					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Region {Region}");
					result = device.Region(Region);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Region failed {result}");
						return;
					}

					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} ADR On");
					result = device.AdrOn();
					if (result != Result.Success)
					{
						Debug.WriteLine($"ADR on failed {result}");
						return;
					}

#if CONFIRMED
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Confirmed");
					result = device.UplinkMessageConfirmationOn();
					if (result != Result.Success)
					{
						Debug.WriteLine($"Confirm on failed {result}");
						return;
					}
#else
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Unconfirmed");
					result = device.UplinkMessageConfirmationOff();
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Confirm off failed {result}");
                  return;
               }
#endif

#if OTAA
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} OTAA");
					result = device.OtaaInitialise(Config.AppEui, Config.AppKey);
					if (result != Result.Success)
					{
						Debug.WriteLine($"OTAA Initialise failed {result}");
						return;
					}
#endif

#if ABP
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} ABP");
               result = device.AbpInitialise(Config.DevAddress, Config.NwksKey, Config.AppsKey);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"ABP Initialise failed {result}");
                  return;
               }
#endif

					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join start Timeout:{JoinTimeoutmSec}mSec");
					result = device.Join(JoinTimeoutmSec);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Join failed {result}");
						return;
					}
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join Started");

					Thread.Sleep(Timeout.Infinite);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		static void OnJoinCompletionHandler(bool result)
		{
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join finished:{result}");

			if (result)
			{
				MessageSendTimer.Change(MessageSendTimerDue, MessageSendTimerPeriod);
			}
		}

		static void MessageSendTimerCallback(object state)
		{
			Rak811LoRaWanDevice device = (Rak811LoRaWanDevice)state;
			Result result;

#if POWER_SAVE
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Wakeup");
			result = device.Wakeup();
			if (result != Result.Success)
			{
				Debug.WriteLine($"Wakeup failed {result}");
				return;
			}
#endif

#if PAYLOAD_HEX
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} port:{MessagePort} payload HEX:{PayloadHex}");
			result = device.Send(MessagePort, PayloadHex, SendTimeoutmSec);
#endif
#if PAYLOAD_BYTES
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} port:{MessagePort} payload Bytes:{Rak811LoRaWanDevice.BytesToHex(PayloadBytes)}");
			result = device.Send(MessagePort, PayloadBytes, SendTimeoutmSec);
#endif
			if (result != Result.Success)
			{
				Debug.WriteLine($"Send failed {result}");
			}

#if POWER_SAVE
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Sleep");
			result = device.Sleep();
			if (result != Result.Success)
			{
				Debug.WriteLine($"Sleep failed {result}");
				return;
			}
#endif
		}

#if CONFIRMED
		static void OnMessageConfirmationHandler(int rssi, int snr)
		{
			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Confirm RSSI:{rssi} SNR:{snr}");
		}
#endif

		static void OnReceiveMessageHandler(byte port, int rssi, int snr, string payload)
		{
			byte[] payloadBytes = Rak811LoRaWanDevice.HexToByes(payload);

			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Receive Message RSSI:{rssi} SNR:{snr} Port:{port} Payload:{payload} PayLoadBytes:{BitConverter.ToString(payloadBytes)}");
		}
	}
}
