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
//  PAYLOAD_BCD vs. PAYLOAD_BCD
//  OTAA vs. ABP
//  CONFIRMED
//---------------------------------------------------------------------------------
namespace devMobile.IoT.NetCore.Rak811.LoRaWanDeviceClient
{
   using System;
   using System.IO.Ports;
   using System.Threading;
   using System.Diagnostics;

	using devMobile.IoT.NetCore.Rak811.LoRaWan;

	public class Program
   {
      private const string SerialPortId = "/dev/ttyS0";
      private const string Region = "AS923";
      private static readonly TimeSpan JoinTimeOut = new TimeSpan(0, 0, 10);
      private static readonly TimeSpan SendTimeout = new TimeSpan(0, 0, 20);
      private const byte MessagePort = 1;
#if PAYLOAD_BCD
      private const string PayloadBcd = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN in BCD
#endif
#if PAYLOAD_BYTES
      private static readonly byte[] PayloadBytes = { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x4c, 0x6f, 0x52, 0x61, 0x57, 0x41, 0x4e}; // Hello LoRaWAN in bytes
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

#if CONFIRMED
               device.OnMessageConfirmation += OnMessageConfirmationHandler;
#endif
               device.OnReceiveMessage += OnReceiveMessageHandler;

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
               result = device.Confirm(LoRaConfirmType.Confirmed);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Confirm on failed {result}");
                  return;
               }
#else
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Unconfirmed");
               result = device.Confirm(LoRaConfirmType.Unconfirmed);
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
               result = device.AbpInitialise(DevAddress, NwksKey, AppsKey);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"ABP Initialise failed {result}");
                  return;
               }
#endif

               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join start Timeout:{JoinTimeOut.TotalMilliseconds}mSec");
               result = device.Join(JoinTimeOut);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Join failed {result}");
                  return;
               }
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join finish");

               while (true)
               {
#if PAYLOAD_BCD
                  Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Timeout:{JoinTimeOut.Seconds}s port:{MessagePort} payload BCD:{PayloadBcd}");
                  result = device.Send(MessagePort, PayloadBcd, SendTimeout);
#endif
#if PAYLOAD_BYTES
                  Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Timeout:{JoinTimeOut.Seconds}s port:{MessagePort} payload Bytes:{BitConverter.ToString(PayloadBytes)}");
                  result = device.Send(MessagePort, PayloadBytes, SendTimeout);
#endif
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Send failed {result}");
                  }

                  // if we sleep module too soon response is missed
                  Thread.Sleep(new TimeSpan(0, 0, 5));

                  Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Sleep");
                  result = device.Sleep();
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Sleep failed {result}");
                     return;
                  }

                  Thread.Sleep(new TimeSpan(0, 5, 0));

                  Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Wakeup");
                  result = device.Wakeup();
                  if (result != Result.Success)
                  {
                     Debug.WriteLine($"Wakeup failed {result}");
                     return;
                  }

                  // if we send too soon after wakeup failure
                  Thread.Sleep(new TimeSpan(0, 0, 5));
               }
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

#if CONFIRMED
      static void OnMessageConfirmationHandler(int rssi, int snr)
      {
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Confirm RSSI:{rssi} SNR:{snr}");
      }
#endif

      static void OnReceiveMessageHandler(int port, int rssi, int snr, string payloadBcd)
      {
         byte[] payloadBytes = Rak811LoRaWanDevice.BcdToByes(payloadBcd);

         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Receive Message RSSI:{rssi} SNR:{snr} Port:{port} Payload:{payloadBcd} PayLoadBytes:{BitConverter.ToString(payloadBytes)}");
      }
   }
}
