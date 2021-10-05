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
namespace devMobile.IoT.LoRaWAN.NetCore.RAK811
{
	using System;
#if DIAGNOSTICS
	using System.Diagnostics;
#endif
	using System.IO.Ports;
	using System.Threading;

	/// <summary>
	/// The LoRaWAN device classes. From The Things Network definitions
	/// </summary>
	public enum LoRaWANDeviceClass
	{
		Undefined = 0,
		/// <summary>
		/// Class A devices support bi-directional communication between a device and a gateway. Uplink messages (from 
		/// the device to the server) can be sent at any time. The device then opens two receive windows at specified 
		/// times (RX1 Delay and RX2 Delay) after an uplink transmission. If the server does not respond in either of 
		/// these receive windows, the next opportunity will be after the next uplink transmission from the device. 
		A,
		/// <summary>
		/// Class B devices extend Class A by adding scheduled receive windows for downlink messages from the server. 
		/// Using time-synchronized beacons transmitted by the gateway, the devices periodically open receive windows. 
		/// The time between beacons is known as the beacon period, and the time during which the device is available 
		/// to receive downlinks is a “ping slot.”
		/// </summary>
		B,
		/// <summary>
		/// Class C devices extend Class A by keeping the receive windows open unless they are transmitting, as shown 
		/// in the figure below. This allows for low-latency communication but is many times more energy consuming than 
		/// Class A devices.
		/// </summary>
		C
	}

	public enum LoRaConfirmType
	{
		Undefined = 0,
		Unconfirmed,
		Confirmed,
		Multicast,
		Proprietary
	}

	/// <summary>
	/// Possible results of library methods (combination of RAK3172 AT command and state machine errors)
	/// </summary>
	public enum Result
	{
		Undefined = 0,
		/// <summary>
		/// Command executed without error.
		/// </summary>
		Success,
		/// <summary>
		/// The BLE works in an invalid state, so that it can’t be operated.
		/// </summary>
		ResponseInvalid,
		/// <summary>
		/// Command failed to complete in configured duration.
		/// </summary>
		Timeout,
		/// <summary>
		/// The last command received is an unsupported AT command.
		/// </summary>
		ATCommandUnsuported,
		/// <summary>
		/// Invalid parameter in the AT command.
		/// </summary>
		ATCommandInvalidParameter,
		/// <summary>
		/// There is an error when reading or writing the flash memory.
		/// </summary>
		ErrorReadingOrWritingFlash,
		/// <summary>
		/// The LoRa transceiver is busy, could not process a new command.
		/// </summary>
		LoRaBusy,
		/// <summary>
		/// LoRa service is unknown. Unknown MAC command received by node. Execute commands that are not supported in the current state,
		/// </summary>
		LoRaServiceIsUnknown,
		/// <summary>
		/// The LoRa parameters are invalid.
		/// </summary>
		LoRaParameterInvalid,
		/// <summary>
		/// The LoRa frequency parameters are invalid.
		/// </summary>
		LoRaFrequencyInvalid,
		/// <summary>
		/// The LoRa data rate (DR) is invalid.
		/// </summary>
		LoRaDataRateInvalid,
		/// <summary>
		/// The LoRa frequency and data rate are invalid.
		/// </summary>
		LoRaFrequencyAndDataRateInvalid,
		/// <summary>
		/// The device has not joined into a LoRa network.
		/// </summary>
		LoRaDeviceNotJoinedNetwork,
		/// <summary>
		/// The length of the packet exceeded that maximum allowed by the LoRa protocol.
		/// </summary>
		LoRaPacketToLong,
		/// <summary>
		/// Service is closed by the server. Due to the limitation of duty cycle, the server will 
		/// send "SRV_MAC_DUTY_CYCLE_REQ" MAC command to close the service.
		/// </summary>
		LoRaServiceIsClosedByServer,
		/// <summary>
		/// This is an unsupported region code.
		/// </summary>
		LoRaRegionUnsupported,
		/// <summary>
		/// Duty cycle is restricted. Due to duty cycle, data cannot be sent at this time until the time limit is removed.
		/// </summary>
		LoRaDutyCycleRestricted,
		/// <summary>
		/// No valid LoRa channel could be found.
		/// </summary>
		LoRaNoValidChannelFound,
		/// <summary>
		/// No available LoRa channel could be found.
		/// </summary>
		LoRaNoFreeChannelFound,
		/// <summary>
		/// Status is error. Generally, the internal state of the protocol stack is wrong.
		/// </summary>
		StatusIsError,
		/// <summary>
		/// Time out reached while sending the packet through the LoRa transceiver.
		/// </summary>
		LoRaTransmitTimeout,
		/// <summary>
		/// Time out reached while waiting for a packet in the LoRa RX1 window.
		/// </summary>
		LoRaRX1Timeout,
		/// <summary>
		/// Time out reached while waiting for a packet in the LoRa RX2 window.
		/// </summary>
		LoRaRX2Timeout,
		/// <summary>
		/// There is an error while receiving a packet during the LoRa RX1 window.
		/// </summary>
		LoRaRX1ReceiveError,
		/// <summary>
		/// There is an error while receiving a packet during the LoRa RX2 window.
		/// </summary>
		LoRaRX2ReceiveError,
		/// <summary>
		/// Failed to join into a LoRa network.
		/// </summary>
		LoRaJoinFailed,
		/// <summary>
		/// Duplicated down-link message detected. A message with an invalid down-link count was received.
		/// </summary>
		LoRaDownlinkRepeated,
		/// <summary>
		///  Payload size is not valid for the current data rate (DR).
		/// </summary>
		LoRaPayloadSizeNotValidForDataRate,
		/// <summary>
		/// There many down-link packets were lost.
		/// </summary>
		LoRaTooManyDownlinkFramesLost,
		/// <summary>
		///  Address fail. The address of the received packet does not match the address of the current node.
		/// </summary>
		LoRaAddressFail,
		/// <summary>
		/// Invalid MIC was detected in the LoRa message.
		/// </summary>
		LoRaMicVerifyError,
	}

	public sealed class Rak811LoRaWanDevice : IDisposable
	{
		public const ushort RegionIDLength = 5;
		public const ushort DevEuiLength = 16;
		public const ushort AppEuiLength = 16;
		public const ushort AppKeyLength = 32;
		public const ushort DevAddrLength = 8;
		public const ushort NwsKeyLength = 32;
		public const ushort AppsKeyLength = 32;
		public const ushort MessagePortMinimumValue = 1;
		public const ushort MessagePortMaximumValue = 223;

		private SerialPort serialDevice = null;
		private Thread CommandResponsesProcessorThread = null;
		private Boolean CommandProcessResponses = true;
		private const int CommandTimeoutDefaultmSec = 1500;
		private const int ReceiveTimeoutDefaultmSec = 10000;
		private readonly AutoResetEvent atExpectedEvent;
		private Result result;

		public delegate void JoinCompletionHandler(bool result);
		public JoinCompletionHandler OnJoinCompletion;
		public delegate void MessageConfirmationHandler(int rssi, int snr);
		public MessageConfirmationHandler OnMessageConfirmation;
		public delegate void ReceiveMessageHandler(int port, int rssi, int snr, string payload);
		public ReceiveMessageHandler OnReceiveMessage;

		public Rak811LoRaWanDevice()
		{
			this.atExpectedEvent = new AutoResetEvent(false);
		}

		public Result Initialise(string serialPortId, int baudRate, Parity serialParity = Parity.None, ushort dataBits = 8, StopBits stopBits = StopBits.One)
		{
			serialDevice = new SerialPort(serialPortId);

			// set parameters
			serialDevice.BaudRate = baudRate;
			serialDevice.Parity = serialParity;
			serialDevice.DataBits = dataBits;
			serialDevice.StopBits = stopBits;
			serialDevice.Handshake = Handshake.None;

			serialDevice.NewLine = "\r\n";

			serialDevice.ReadTimeout = ReceiveTimeoutDefaultmSec;

			serialDevice.Open();
			serialDevice.ReadExisting();

			// Only start up the serial port polling thread if the port opened successfuly
			CommandResponsesProcessorThread = new Thread(SerialPortProcessor);
			CommandResponsesProcessorThread.Start();

			// Set the Working mode to LoRaWAN
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:work_mode LoRaWAN");
#endif
			Result result = SendCommand("at+set_config=lora:work_mode:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:work_mode failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Class(LoRaWANDeviceClass deviceClass)
		{
			string command;

			switch (deviceClass)
			{
				case LoRaWANDeviceClass.A:
					command = "at+set_config=lora:class:0";
					break;
				// Currently ClassB unsupported
				//case LoRaWANDeviceClass.B;
				//   command = "at+set_config=lora:class:1";
				//   break;
				case LoRaWANDeviceClass.C:
					command = "at+set_config=lora:class:2";
					break;
				default:
					throw new ArgumentException($"LoRa class value {deviceClass} invalid", nameof(deviceClass));
			}

			// Set the class
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:class:{deviceClass}");
#endif
			Result result = SendCommand(command);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:class failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Confirm(LoRaConfirmType loRaConfirmType)
		{
			string command;

			switch (loRaConfirmType)
			{
				case LoRaConfirmType.Unconfirmed:
					command = "at+set_config=lora:confirm:0";
					break;
				case LoRaConfirmType.Confirmed:
					command = "at+set_config=lora:confirm:1";
					break;
				case LoRaConfirmType.Multicast:
					command = "at+set_config=lora:confirm:2";
					break;
				case LoRaConfirmType.Proprietary:
					command = "at+set_config=lora:confirm:3";
					break;
				default:
					throw new ArgumentException($"LoRa confirm type value {loRaConfirmType} invalid", nameof(loRaConfirmType));
			}

			// Set the confirmation type
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:confirm:{loRaConfirmType}");
#endif
			Result result = SendCommand(command);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:confirm failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Region(string regionID)
		{
			if (regionID == null)
			{
				throw new ArgumentNullException(nameof(regionID), $"RegionID is invalid");
			}

			if (regionID.Length != RegionIDLength)
			{
				throw new ArgumentException($"RegionID {regionID} length {regionID.Length} invalid", nameof(regionID));
			}

#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:region:{regionID}");
#endif
			Result result = SendCommand($"at+set_config=lora:region:{regionID}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:region failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Sleep()
		{
			// Put the RAK module to sleep
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep:1");
#endif
			Result result = SendCommand($"at+set_config=device:sleep:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Wakeup()
		{
			// Wakeup the RAK Module
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep:0");
#endif
			Result result = SendCommand($"at+set_config=device:sleep:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} device:sleep failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result AdrOff()
		{
			// Adaptive Data Rate off
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr:0");
#endif
			Result result = SendCommand($"at+set_config=lora:adr:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result AdrOn()
		{
			// Adaptive Data Rate on
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr:1");
#endif
			Result result = SendCommand($"at+set_config=lora:adr:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:adr failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result AbpInitialise(string devAddr, string nwksKey, string appsKey)
		{
			Result result;

			if (devAddr == null)
			{
				throw new ArgumentNullException(nameof(devAddr));
			}

			if (devAddr.Length != DevAddrLength)
			{
				throw new ArgumentException($"devAddr invalid length must be {DevAddrLength} characters", nameof(devAddr));
			}

			if (nwksKey == null)
			{
				throw new ArgumentNullException(nameof(nwksKey));
			}

			if (nwksKey.Length != NwsKeyLength)
			{
				throw new ArgumentException($"nwksKey invalid length must be {NwsKeyLength} characters", nameof(nwksKey));
			}

			if (appsKey == null)
			{
				throw new ArgumentNullException(nameof(appsKey));
			}

			if (appsKey.Length != AppsKeyLength)
			{
				throw new ArgumentException($"appsKey invalid length must be {AppsKeyLength} characters", nameof(appsKey));
			}

			// Set the JoinMode to ABP
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode:1");
#endif
			result = SendCommand($"at+set_config=lora:join_mode:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode failed {result}");
#endif
				return result;
			}

			// set the devAddr
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:devAddr {devAddr}");
#endif
			result = SendCommand($"at+set_config=lora:dev_addr:{devAddr}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:dev_addr failed {result}");
#endif
				return result;
			}

			// Set the nwsKey
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:nwks_Key:{nwksKey}");
#endif
			result = SendCommand($"at+set_config=lora:nwks_key:{nwksKey}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:nwksKey failed {result}");
#endif
				return result;
			}

			// Set the appsKey
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:apps_key:{appsKey}");
#endif
			result = SendCommand($"at+set_config=lora:apps_key:{appsKey}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:apps_key failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result OtaaInitialise(string appEui, string appKey)
		{
			Result result;

			if (appEui == null)
			{
				throw new ArgumentNullException(nameof(appEui));
			}

			if (appEui.Length != AppEuiLength)
			{
				throw new ArgumentException($"appEui invalid length must be {AppEuiLength} characters", nameof(appEui));
			}

			if (appKey == null)
			{
				throw new ArgumentNullException(nameof(appKey));
			}

			if (appKey.Length != AppKeyLength)
			{
				throw new ArgumentException($"appKey invalid length must be {AppKeyLength} characters", nameof(appKey));
			}

			// Set the JoinMode to OTAA
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode:0");
#endif
			result = SendCommand($"at+set_config=lora:join_mode:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:join_mode failed {result}");
#endif
				return result;
			}

			// Set the appEUI
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_eui:{appEui}");
#endif
			result = SendCommand($"at+set_config=lora:app_eui:{appEui}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:devEui failed {result}");
#endif
				return result;
			}

			// Set the appKey
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_key:{appKey}");
#endif
			result = SendCommand($"at+set_config=lora:app_key:{appKey}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_key failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Join(int timeoutmSec)
		{
			Result result;

#if AS923_HACK
			result = SendCommand("at+set_config=lora:ch_mask:2:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:2:0 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:3:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:3:0 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:4:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:4:0 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:5:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:5:0 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:6:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:6:0 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:7:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:7:0 {result}");
#endif
				return result;
			}
#endif

			// Join the network
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} join");
#endif
			result = SendCommand($"at+join", timeoutmSec);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} join failed {result}");
#endif
				return result;
			}

#if AS923_HACK
			result = SendCommand("at+set_config=lora:ch_mask:2:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:2:1 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:3:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:3:1 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:4:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:4:1 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:5:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:5:1 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:6:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:6:1 {result}");
#endif
				return result;
			}

			result = SendCommand("at+set_config=lora:ch_mask:7:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:ch_mask:7:1 {result}");
#endif
				return result;
			}
#endif

			return Result.Success;
		}

		public Result Send(ushort port, string payload, int timeoutmSec)
		{
			Result result;

			if ((port < MessagePortMinimumValue) || (port > MessagePortMaximumValue))
			{
				throw new ArgumentException($"port invalid must be greater than or equal to {MessagePortMinimumValue} and less than or equal to {MessagePortMaximumValue}", nameof(port));
			}

			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			if ((payload.Length % 2) != 0)
			{
				throw new ArgumentException("Payload length invalid must be a multiple of 2", nameof(payload));
			}

			// Send message the network
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} Send port:{port} payload {payload} timeout {timeoutmSec} mSec");
#endif
			result = SendCommand($"at+send=lora:{port}:{payload}", timeoutmSec);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} send failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		public Result Send(ushort port, byte[] payload, int timeoutmSec)
		{
			Result result;

			if ((port < MessagePortMinimumValue) || (port > MessagePortMaximumValue))
			{
				throw new ArgumentException($"port invalid must be greater than or equal to {MessagePortMinimumValue} and less than or equal to {MessagePortMaximumValue}", nameof(port));
			}

			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			string payloadHex = Rak811LoRaWanDevice.BytesToHex(payload);

			// Send message the network
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} Send port:{port} payload:{payloadHex} timeout:{timeoutmSec} mSec");
#endif
			result = SendCommand($"at+send=lora:{port}:{payloadHex}");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} send failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		private Result SendCommand(string command, int timeoutmSec = CommandTimeoutDefaultmSec)
		{
			if (command == null)
			{
				throw new ArgumentNullException(nameof(command));
			}

			if (command == string.Empty)
			{
				throw new ArgumentException($"command invalid length cannot be empty", nameof(command));
			}

			serialDevice.WriteLine(command);

			this.atExpectedEvent.Reset();

			if (!this.atExpectedEvent.WaitOne(timeoutmSec, false))
			{
				return Result.Timeout;
			}

			return result;
		}

		public void SerialPortProcessor()
		{
			string line;

			while (CommandProcessResponses)
			{
				try
				{
#if DIAGNOSTICS
					Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} ReadLine before");
#endif
					line = serialDevice.ReadLine().Trim('\0').Trim();
#if DIAGNOSTICS
					Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} ReadLine after:{line}");
#endif
					// consume empty lines
					if (String.IsNullOrWhiteSpace(line))
					{
						continue;
					}

					// Consume the response from set work mode
					if (line.StartsWith("?LoRa (R)") || line.StartsWith("RAK811 ") || line.StartsWith("UART1 ") || line.StartsWith("UART3 ") || line.StartsWith("LoRa work mode"))
					{
						continue;
					}

					// See if device successfully joined network
					if (line.StartsWith("OK Join Success"))
					{
						OnJoinCompletion?.Invoke(true);

						atExpectedEvent.Set();

						continue;
					}

					if (line.StartsWith("at+recv="))
					{
						string[] fields = line.Split("=,:".ToCharArray());

						int port = int.Parse(fields[1]);
						int rssi = int.Parse(fields[2]);
						int snr = int.Parse(fields[3]);
						int length = int.Parse(fields[4]);

						if (this.OnMessageConfirmation != null)
						{
							OnMessageConfirmation?.Invoke(rssi, snr);
						}
						if (length > 0)
						{
							string payload = fields[5];

							if (this.OnReceiveMessage != null)
							{
								OnReceiveMessage.Invoke(port, rssi, snr, payload);
							}
						}
						continue;
					}

					switch (line)
					{
						case "OK":
						case "Initialization OK":
						case "OK Wake Up":
						case "OK Sleep":
							result = Result.Success;
							break;

						case "ERROR: 1":
							result = Result.ATCommandUnsuported;
							break;
						case "ERROR: 2":
							result = Result.ATCommandInvalidParameter;
							break;
						case "ERROR: 3": //There is an error when reading or writing flash.
						case "ERROR: 4": //There is an error when reading or writing through IIC.
							result = Result.ErrorReadingOrWritingFlash;
							break;
						case "ERROR: 5": //There is an error when sending through UART
							result = Result.ATCommandInvalidParameter;
							break;
						case "ERROR: 41": //The BLE works in an invalid state, so that it can’t be operated.
							result = Result.ResponseInvalid;
							break;
						case "ERROR: 80":
							result = Result.LoRaBusy;
							break;
						case "ERROR: 81":
							result = Result.LoRaServiceIsUnknown;
							break;
						case "ERROR: 82":
							result = Result.LoRaParameterInvalid;
							break;
						case "ERROR: 83":
							result = Result.LoRaFrequencyInvalid;
							break;
						case "ERROR: 84":
							result = Result.LoRaDataRateInvalid;
							break;
						case "ERROR: 85":
							result = Result.LoRaFrequencyAndDataRateInvalid;
							break;
						case "ERROR: 86":
							result = Result.LoRaDeviceNotJoinedNetwork;
							break;
						case "ERROR: 87":
							result = Result.LoRaPacketToLong;
							break;
						case "ERROR: 88":
							result = Result.LoRaServiceIsClosedByServer;
							break;
						case "ERROR: 89":
							result = Result.LoRaRegionUnsupported;
							break;
						case "ERROR: 90":
							result = Result.LoRaDutyCycleRestricted;
							break;
						case "ERROR: 91":
							result = Result.LoRaNoValidChannelFound;
							break;
						case "ERROR: 92":
							result = Result.LoRaNoFreeChannelFound;
							break;
						case "ERROR: 93":
							result = Result.StatusIsError;
							break;
						case "ERROR: 94":
							result = Result.LoRaTransmitTimeout;
							break;
						case "ERROR: 95":
							result = Result.LoRaRX1Timeout;
							break;
						case "ERROR: 96":
							result = Result.LoRaRX2Timeout;
							break;
						case "ERROR: 97":
							result = Result.LoRaRX1ReceiveError;
							break;
						case "ERROR: 98":
							result = Result.LoRaRX2ReceiveError;
							break;
						case "ERROR: 99":
							result = Result.LoRaJoinFailed;
							break;
						case "ERROR: 100":
							result = Result.LoRaDownlinkRepeated;
							break;
						case "ERROR: 101":
							result = Result.LoRaPayloadSizeNotValidForDataRate;
							break;
						case "ERROR: 102":
							result = Result.LoRaTooManyDownlinkFramesLost;
							break;
						case "ERROR: 103":
							result = Result.LoRaAddressFail;
							break;
						case "ERROR: 104":
							result = Result.LoRaMicVerifyError;
							break;
						default:
							result = Result.ResponseInvalid;
							break;
					}
				}
				catch (TimeoutException)
				{
					result = Result.Timeout;
				}

				atExpectedEvent.Set();
			}
		}

		// Utility functions for clients for processing messages payloads to be send, ands messages payloads received.
		/// <summary>
		/// Converts an array of byes to a hexadecimal string.
		/// </summary>
		/// <param name="payloadBytes"></param>
		/// <exception cref="ArgumentNullException">The array of bytes is null.</exception>
		/// <returns>String containing hex encoded bytes</returns>
		public static string BytesToHex(byte[] payloadBytes)
		{
			if (payloadBytes == null)
			{
				throw new ArgumentNullException(nameof(payloadBytes));
			}

			return BitConverter.ToString(payloadBytes).Replace("-", "");
		}

		/// <summary>
		/// Converts a hexadecimal string to an array of bytes.
		/// </summary>
		/// <param name="payload">array of bytes encoded as hex</param>
		/// <exception cref="ArgumentNullException">The Hexadecimal string is null.</exception>
		/// <exception cref="ArgumentException">The Hexadecimal string is not at even number of characters.</exception>
		/// <exception cref="System.FormatException">The Hexadecimal string contains some invalid characters.</exception>
		/// <returns>Array of bytes parsed from Hexadecimal string.</returns>
		public static byte[] HexToByes(string payload)
		{
			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}
			if (payload.Length % 2 != 0)
			{
				throw new ArgumentException($"Payload invalid length must be an even number", nameof(payload));
			}

			Byte[] payloadBytes = new byte[payload.Length / 2];

			char[] chars = payload.ToCharArray();

			for (int index = 0; index < payloadBytes.Length; index++)
			{
				byte byteHigh = Convert.ToByte(chars[index * 2].ToString(), 16);
				byte byteLow = Convert.ToByte(chars[(index * 2) + 1].ToString(), 16);

				payloadBytes[index] += (byte)(byteHigh * 16);
				payloadBytes[index] += byteLow;
			}

			return payloadBytes;
		}

		public void Dispose()
		{
			CommandProcessResponses = false;

			if (CommandResponsesProcessorThread != null)
			{
				CommandResponsesProcessorThread.Join();
				CommandResponsesProcessorThread = null;
			}

			if (serialDevice != null)
			{
				if (serialDevice.IsOpen)
				{
					serialDevice.Dispose();
				}
				serialDevice = null;
			}
		}
	}
}
