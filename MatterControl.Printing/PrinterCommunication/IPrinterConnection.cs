﻿using System;
using System.Diagnostics;
using System.IO;
using MatterControl.Common.Repository;
using MatterControl.Printing.Pipelines;
using MatterHackers.MatterControl;
using MatterHackers.SerialPortCommunication.FrostedSerial;
using MatterHackers.VectorMath;

namespace MatterControl.Printing
{
	public interface IPrinterConnection
	{
		int ActiveExtruderIndex { get; }

		string ActivePrintName { get; }

		PrintJob ActivePrintTask { get; set; }

		double ActualBedTemperature { get; }

		void AllowLeveling(bool allowLeveling);

		bool AnyHeatIsOn { get; }

		bool AtxPowerEnabled { get; set; }

		bool CalibrationPrint { get; }

		CommunicationStates CommunicationState { get; set; }

		bool ContinueHoldingTemperature { get; set; }

		Vector3 CurrentDestination { get; }

		double CurrentExtruderDestination { get; }

		int CurrentlyPrintingLayer { get; }

		DetailedPrintingState DetailedPrintingState { get; set; }

		string DeviceCode { get; }

		int ExtruderCount { get; }

		double FanSpeed0To255 { get; set; }

		FirmwareTypes FirmwareType { get; }

		string FirmwareVersion { get; }

		Vector3 HomingPosition { get; }

		bool IsConnected { get; }

		Vector3 LastReportedPosition { get; }

		int NumQueuedCommands { get; }

		bool Paused { get; }

		double PercentComplete { get; }

		PrintHostConfig Printer { get; }

		bool Printing { get; }

		double RatioIntoCurrentLayerInstructions { get; }

		double RatioIntoCurrentLayerSeconds { get; }

		int SecondsPrinted { get; }

		int SecondsToEnd { get; }

		double SecondsToHoldTemperature { get; }

		IFrostedSerialPort serialPort { get; }

		double TargetBedTemperature { get; set; }

		int TimeToHoldTemperature { get; set; }

		int TotalLayersInPrint { get; }

		event EventHandler AtxPowerStateChanged;

		event EventHandler BedTargetTemperatureChanged;

		event EventHandler BedTemperatureRead;

		event EventHandler CommunicationStateChanged;

		event EventHandler<ConnectFailedEventArgs> ConnectionFailed;

		event EventHandler ConnectionSucceeded;

		event EventHandler DestinationChanged;

		event EventHandler DetailedPrintingStateChanged;

		event EventHandler Disposed;

		event EventHandler<DeviceErrorArgs> ErrorReported;

		event EventHandler FanSpeedSet;

		event EventHandler<PrintPauseEventArgs> FilamentRunout;

		event EventHandler FirmwareVersionRead;

		event EventHandler HomingPositionChanged;

		event EventHandler<int> HotendTargetTemperatureChanged;

		event EventHandler HotendTemperatureRead;

		event EventHandler<string> LineReceived;

		event EventHandler<string> LineSent;

		event EventHandler<PrintPauseEventArgs> PauseOnLayer;

		event EventHandler PrintCanceled;

		event EventHandler<string> PrintFinished;

		event EventHandler TemporarilyHoldingTemp;

		void ArduinoDtrReset();

		void Connect();

		void DeleteFileFromSdCard(string fileName);

		void Disable();

		void Dispose();

		double GetActualHotendTemperature(int hotendIndex0Based);

		double GetTargetHotendTemperature(int hotendIndex0Based);

		void HaltConnectionThread();

		void HomeAxis(PrinterAxis axis);

		void MacroCancel();

		void MacroStart();

		void MoveAbsolute(PrinterAxis axis, double axisPositionMm, double feedRateMmPerMinute);

		void MoveAbsolute(Vector3 position, double feedRateMmPerMinute);

		void MoveExtruderRelative(double moveAmountMm, double feedRateMmPerMinute, int extruderNumber = 0);

		void MoveRelative(PrinterAxis axis, double moveAmountMm, double feedRateMmPerMinute);

		void QueueLine(string lineToWrite, bool forceTopOfQueue = false);

		void ReadPosition(PositionReadType positionReadType = PositionReadType.Other, bool forceToTopOfQueue = false);

		void RebootBoard();

		void ReleaseMotors(bool forceRelease = false);

		void RequestPause();

		void Resume();

		void SetTargetHotendTemperature(int hotendIndex0Based, double temperature, bool forceSend = false);

		void StartPrint(PrintJob printTask, bool calibrationPrint = false);

		void StartPrint(Stream gcodeStream, PrintJob printTask, bool calibrationPrint = false);

		bool StartSdCardPrint(string m23FileName);

		void Stop(bool markPrintCanceled = true);

		void SwitchToGCode(string gCodeFilePath);

		void TurnOffBedAndExtruders(TurnOff turnOffTime);
	}
}