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
		/// <summary>
		/// LoRaWAN Alliance regional frequency plan 5 character identifier.
		/// </summary>
		public const byte RegionIDLength = 5;
		/// <summary>
		/// The DevEUI is a 64-bit globally-unique Extended Unique Identifier (EUI-64) assigned by the manufacturer, or
		/// the owner, of the end-device. This is represented by a 16 character long string
		/// </summary>
		public const byte DevEuiLength = 16;
		/// <summary>
		/// The JoinEUI(formerly known as AppEUI) is a 64-bit globally-unique Extended Unique Identifier (EUI-64).Each 
		/// Join Server, which is used for authenticating the end-devices, is identified by a 64-bit globally unique 
		/// identifier, JoinEUI, that is assigned by either the owner or the operator of that server. This is 
		/// represented by a 16 character long string.
		/// </summary>	
		public const byte JoinEuiLength = 16;
		/// <summary>
		/// The AppKey is the encryption key between the source of the message (based on the DevEUI) and the destination 
		/// of the message (based on the AppEUI). This key must be unique for each device. This is represented by a 32 
		/// character long string
		/// </summary>
		public const byte AppKeyLength = 32;
		/// <summary>
		/// The DevAddr is composed of two parts: the address prefix and the network address. The address prefix is 
		/// allocated by the LoRa Alliance® and is unique to each network that has been granted a NetID. This is 
		/// represented by an 8 character long string.
		/// </summary>
		public const byte DevAddrLength = 8;
		/// <summary>
		/// After activation, the Network Session Key(NwkSKey) is used to secure messages which do not carry a payload.
		/// </summary>
		public const byte NwsKeyLength = 32;
		/// <summary>
		/// The AppSKey is an application session key specific for the end-device. It is used by both the application 
		/// server and the end-device to encrypt and decrypt the payload field of application-specific data messages.
		/// This is represented by an 32 character long string
		/// </summary>
		public const byte AppsKeyLength = 32;
		/// <summary>
		/// The minimum supported port number. Port 0 is used for FRMPayload which contains MAC commands only.
		/// </summary>
		public const byte MessagePortMinimumValue = 1;
		/// <summary>
		/// The maximum supported port number. Port 224 is used for the LoRaWAN Mac layer test protocol. Ports 
		/// 223…255 are reserved for future application extensions.
		/// </summary>
		public const byte MessagePortMaximumValue = 223;

		private SerialPort SerialDevice = null;
		private Thread CommandResponsesProcessorThread = null;
		private Boolean CommandProcessResponses = true;
		private const int CommandTimeoutDefaultmSec = 1500;
		private const int ReceiveTimeoutDefaultmSec = 10000;
		private readonly AutoResetEvent CommandResponseExpectedEvent;
		private Result CommandResult;

		/// <summary>
		/// Event handler called when network join process completed.
		/// </summary>
		/// <param name="joinSuccessful">Was the network join attempt successful</param>
		public delegate void JoinCompletionHandler(bool result);
		public JoinCompletionHandler OnJoinCompletion;
		/// <summary>
		/// Event handler called when uplink message delivery to network confirmed
		/// </summary>
		public delegate void MessageConfirmationHandler(int rssi, int snr);
		public MessageConfirmationHandler OnMessageConfirmation;
		/// <summary>
		/// Event handler called when downlink message received.
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="rssi">Received Signal Strength Indicator(RSSI).</param>
		/// <param name="snr">Signal to Noise Ratio(SNR).</param>
		/// <param name="payload">Hexadecimal representation of payload.</param>
		public delegate void ReceiveMessageHandler(byte port, int rssi, int snr, string payload);
		public ReceiveMessageHandler OnReceiveMessage;

		public Rak811LoRaWanDevice()
		{
			this.CommandResponseExpectedEvent = new AutoResetEvent(false);
		}

		/// <summary>
		/// Initializes a new instance of the devMobile.IoT.LoRaWAN.NetCore.RAK3172.Rak3172LoRaWanDevice class using the
		/// specified port name, baud rate, parity bit, data bits, and stop bit.
		/// </summary>
		/// <param name="serialPortId">The port to use (for example, COM1).</param>
		/// <param name="baudRate">The baud rate, 600 to 115K2.</param>
		/// <param name="serialParity">One of the System.IO.Ports.SerialPort.Parity values, defaults to None.</param>
		/// <param name="dataBits">The data bits value, defaults to 8.</param>
		/// <param name="stopBits">One of the System.IO.Ports.SerialPort.StopBits values, defaults to One.</param>
		/// <exception cref="System.IO.IOException">The serial port could not be found or opened.</exception>
		/// <exception cref="UnauthorizedAccessException">The application does not have the required permissions to open the serial port.</exception>
		/// <exception cref="ArgumentNullException">The serialPortId is null.</exception>
		/// <exception cref="ArgumentException">The specified serialPortId, baudRate, serialParity, dataBits, or stopBits is invalid.</exception>
		/// <exception cref="InvalidOperationException">The attempted operation was invalid e.g. the port was already open.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Initialise(string serialPortId, int baudRate, Parity serialParity = Parity.None, ushort dataBits = 8, StopBits stopBits = StopBits.One)
		{
			SerialDevice = new SerialPort(serialPortId);

			// set parameters
			SerialDevice.BaudRate = baudRate;
			SerialDevice.Parity = serialParity;
			SerialDevice.DataBits = dataBits;
			SerialDevice.StopBits = stopBits;
			SerialDevice.Handshake = Handshake.None;

			SerialDevice.NewLine = "\r\n";

			SerialDevice.ReadTimeout = ReceiveTimeoutDefaultmSec;

			SerialDevice.Open();
			SerialDevice.ReadExisting();

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

		/// <summary>
		/// Sets the LoRaWAN device class.
		/// </summary>
		/// <param name="loRaClass" cref="LoRaWANDeviceClass">The LoRaWAN device class</param>
		/// <exception cref="System.IO.ArgumentException">The loRaClass is invalid.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Disables uplink message confirmations.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result UplinkMessageConfirmationOff()
		{
			// Set the confirmation type
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:0");
#endif
			Result result = SendCommand("at+set_config=lora:confirm:0");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:0 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Enables uplink message confirmations.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result UplinkMessageConfirmationOn()
		{
			// Set the confirmation type
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:1");
#endif
			Result result = SendCommand("at+set_config=lora:confirm:1");
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:1 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sets the region w.g. AS923, AU915, ... EU868, US915 etc.
		/// </summary>
		/// <param name="regionID">The LoRaWAN region code.</param>
		/// <exception cref="ArgumentNullException">The band value is null.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Puts the device into power conservation mode.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Returns the device from power conservation mode.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Disables Adaptive Data Rate(ADR) support.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Enables Adaptive Data Rate(ADR) support
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Configures the device to use Activation By Personalisation(ABP) to connect to the LoRaWAN network
		/// </summary>
		/// <param name="devAddr">The device address<see cref="DevAddrLength"></param>
		/// <param name="nwksKey">The network sessions key<see cref="NwsKeyLength"> </param>
		/// <param name="appsKey">The application session key <see cref="AppsKeyLength"/></param>
		/// <exception cref="System.IO.ArgumentNullException">The devAddr, nwksKey or appsKey is null.</exception>
		/// <exception cref="System.IO.ArgumentException">The devAddr, nwksKey or appsKey length is incorrect.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Configures the device to use Over The Air Activation(OTAA) to connect to the LoRaWAN network
		/// </summary>
		/// <param name="joinEui">The join server unique identifier <see cref="JoinEuiLength"/></param>
		/// <param name="appKey">The application key<see cref="AppKeyLength"/> </param>
		/// <exception cref="System.IO.ArgumentNullException">The joinEui or appKey is null.</exception>
		/// <exception cref="System.IO.ArgumentException">The joinEui or appKey length is incorrect.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result OtaaInitialise(string joinEui, string appKey)
		{
			Result result;

			if (joinEui == null)
			{
				throw new ArgumentNullException(nameof(joinEui));
			}

			if (joinEui.Length != JoinEuiLength)
			{
				throw new ArgumentException($"joinEui invalid length must be {JoinEuiLength} characters", nameof(joinEui));
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
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} lora:app_eui:{joinEui}");
#endif
			result = SendCommand($"at+set_config=lora:app_eui:{joinEui}");
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

		/// <summary>
		/// Starts the process which Joins device to the LoRaWAN network
		/// </summary>
		/// <param name="timeoutmSec">Maximum duration allowed for join</param>
		/// <returns><see cref="Result"/> of the operation.</returns>
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

		/// <summary>
		/// Sends an uplink message in Hexadecimal format
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="payload">Hexadecimal encoded bytes to send</param>
		/// <exception cref="ArgumentNullException">The payload string is null.</exception>
		/// <exception cref="ArgumentException">The payload string must be a multiple of 2 characters long.</exception>
		/// <exception cref="ArgumentException">The port is number is out of range must be <see cref="MessagePortMinimumValue"/> to <see cref="MessagePortMaximumValue"/>.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Send(byte port, string payload, int timeoutmSec)
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

		/// <summary>
		/// Sends an uplink message of array of bytes with a sepcified port number.
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="payload">Array of bytes to send</param>
		/// <exception cref="ArgumentNullException">The payload array is null.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Send(byte port, byte[] payload, int timeoutmSec)
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
			result = SendCommand($"at+send=lora:{port}:{payloadHex}", timeoutmSec);
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

			SerialDevice.WriteLine(command);

			this.CommandResponseExpectedEvent.Reset();

			if (!this.CommandResponseExpectedEvent.WaitOne(timeoutmSec, false))
			{
				return Result.Timeout;
			}

			return CommandResult;
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
					line = SerialDevice.ReadLine().Trim('\0').Trim();
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

						CommandResponseExpectedEvent.Set();

						continue;
					}

					if (line.StartsWith("at+recv="))
					{
						string[] payloadFields = line.Split("=,:".ToCharArray());

						byte port = byte.Parse(payloadFields[1]);
						int rssi = int.Parse(payloadFields[2]);
						int snr = int.Parse(payloadFields[3]);
						int length = int.Parse(payloadFields[4]);

						if (this.OnMessageConfirmation != null)
						{
							OnMessageConfirmation?.Invoke(rssi, snr);
						}
						if (length > 0)
						{
							string payload = payloadFields[5];

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
							CommandResult = Result.Success;
							break;

						case "ERROR: 1":
							CommandResult = Result.ATCommandUnsuported;
							break;
						case "ERROR: 2":
							CommandResult = Result.ATCommandInvalidParameter;
							break;
						case "ERROR: 3": //There is an error when reading or writing flash.
						case "ERROR: 4": //There is an error when reading or writing through IIC.
							CommandResult = Result.ErrorReadingOrWritingFlash;
							break;
						case "ERROR: 5": //There is an error when sending through UART
							CommandResult = Result.ATCommandInvalidParameter;
							break;
						case "ERROR: 41": //The BLE works in an invalid state, so that it can’t be operated.
							CommandResult = Result.ResponseInvalid;
							break;
						case "ERROR: 80":
							CommandResult = Result.LoRaBusy;
							break;
						case "ERROR: 81":
							CommandResult = Result.LoRaServiceIsUnknown;
							break;
						case "ERROR: 82":
							CommandResult = Result.LoRaParameterInvalid;
							break;
						case "ERROR: 83":
							CommandResult = Result.LoRaFrequencyInvalid;
							break;
						case "ERROR: 84":
							CommandResult = Result.LoRaDataRateInvalid;
							break;
						case "ERROR: 85":
							CommandResult = Result.LoRaFrequencyAndDataRateInvalid;
							break;
						case "ERROR: 86":
							CommandResult = Result.LoRaDeviceNotJoinedNetwork;
							break;
						case "ERROR: 87":
							CommandResult = Result.LoRaPacketToLong;
							break;
						case "ERROR: 88":
							CommandResult = Result.LoRaServiceIsClosedByServer;
							break;
						case "ERROR: 89":
							CommandResult = Result.LoRaRegionUnsupported;
							break;
						case "ERROR: 90":
							CommandResult = Result.LoRaDutyCycleRestricted;
							break;
						case "ERROR: 91":
							CommandResult = Result.LoRaNoValidChannelFound;
							break;
						case "ERROR: 92":
							CommandResult = Result.LoRaNoFreeChannelFound;
							break;
						case "ERROR: 93":
							CommandResult = Result.StatusIsError;
							break;
						case "ERROR: 94":
							CommandResult = Result.LoRaTransmitTimeout;
							break;
						case "ERROR: 95":
							CommandResult = Result.LoRaRX1Timeout;
							break;
						case "ERROR: 96":
							CommandResult = Result.LoRaRX2Timeout;
							break;
						case "ERROR: 97":
							CommandResult = Result.LoRaRX1ReceiveError;
							break;
						case "ERROR: 98":
							CommandResult = Result.LoRaRX2ReceiveError;
							break;
						case "ERROR: 99":
							CommandResult = Result.LoRaJoinFailed;
							break;
						case "ERROR: 100":
							CommandResult = Result.LoRaDownlinkRepeated;
							break;
						case "ERROR: 101":
							CommandResult = Result.LoRaPayloadSizeNotValidForDataRate;
							break;
						case "ERROR: 102":
							CommandResult = Result.LoRaTooManyDownlinkFramesLost;
							break;
						case "ERROR: 103":
							CommandResult = Result.LoRaAddressFail;
							break;
						case "ERROR: 104":
							CommandResult = Result.LoRaMicVerifyError;
							break;
						default:
							CommandResult = Result.ResponseInvalid;
							break;
					}
				}
				catch (TimeoutException)
				{
					// Intentionally ignored, not certain this is a good idea
				}

				CommandResponseExpectedEvent.Set();
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

		/// <summary>
		/// Ensures unmanaged serial port and thread resources are released in a "responsible" manner.
		/// </summary>
		public void Dispose()
		{
			CommandProcessResponses = false;

			if (CommandResponsesProcessorThread != null)
			{
				CommandResponsesProcessorThread.Join();
				CommandResponsesProcessorThread = null;
			}

			if (SerialDevice != null)
			{
				if (SerialDevice.IsOpen)
				{
					SerialDevice.Dispose();
				}
				SerialDevice = null;
			}
		}
	}
}
