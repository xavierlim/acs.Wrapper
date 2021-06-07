//===================================================================================
// Copyright (c) CyberOptics Corporation. All rights reserved. The
// copyright to the computer program herein is the property of CyberOptics.
// The program may be used or copied or both only with the written permission
// of CyberOptics or in accordance with the terms and conditions stipulated
// in the agreement or contract under which the program has been supplied.
// This copyright notice must not be removed.
//===================================================================================

using System;
using System.Collections.Generic;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;
using CO.Systems.Services.Robot.Interface;
using CO.Systems.Services.Robot.RobotBase;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public interface IAcsWrapper
    {
        bool IsConnected { get; }
        string FirmwareVersion { get; }
        uint NETLibraryVersion { get; }

        /// <summary>
        /// State of Conveyor Operation
        /// </summary>
        ConveyorStatusCode ConveyorStatus { get; }

        /// <summary>
        /// true when there's any error in the ACS controller
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// true when there's error caused by conveyor control in the ACS controller
        /// </summary>
        bool HasConveyorError { get; }

        /// <summary>
        /// true when there's error caused by robot control in the ACS controller
        /// </summary>
        bool HasRobotError { get; }

        /// <summary>
        /// Error Code
        /// </summary>
        ConveyorErrorCode ErrorCode { get; }

        event Action<bool> ConnectionStatusChanged;
        event Action<GantryAxes, bool> IdleChanged;
        event Action<GantryAxes, bool> EnabledChanged;
        event Action<GantryAxes, bool> ReadyChanged;
        event Action<GantryAxes, double> PositionUpdated;
        event Action<GantryAxes, double> VelocityUpdated;
        event Action<GantryAxes, bool> StopDone;
        event Action<GantryAxes, bool> AbortDone;
        event Action<GantryAxes, bool> AtHomeSensorChanged;
        event Action<GantryAxes, bool> AtPositiveHwLimitChanged;
        event Action<GantryAxes, bool> AtNegativeHwLimitChanged;
        event Action<GantryAxes, bool> AtPositiveSwLimitChanged;
        event Action<GantryAxes, bool> AtNegativeSwLimitChanged;
        event Action<GantryAxes> MovementBegin;
        event Action<GantryAxes, bool> MovementEnd;
        event Action<GantryAxes> OnAxisHomingBegin;
        event Action<GantryAxes, bool> OnAxisHomingEnd;
        event Action ScanningBegin;
        event Action HardwareNotifySingleMoveMotionCompleteReceived;
        event Action HardwareNotifySingleMovePSXAckReceived;
        event Action<int> ScanningIndexChange;
        event Action ScanningEnd;
        void Connect();
        bool Disconnect();
        bool IsIdle(GantryAxes axis);
        bool Enabled(GantryAxes axis);
        bool Homed(GantryAxes axis);
        bool Ready(GantryAxes axis);
        double Position(GantryAxes axis);
        double Velocity(GantryAxes axis);
        bool AtHomeSensor(GantryAxes axis);
        bool AtPositiveHwLimit(GantryAxes axis);
        bool AtNegativeHwLimit(GantryAxes axis);
        bool AtPositiveSwLimit(GantryAxes axis);
        bool AtNegativeSwLimit(GantryAxes axis);

        bool PrepareScanning(
            List<IPvTuple3D> pvTuple3DList,
            int triggerToCameraStartPort,
            int triggerToCameraStartBit,
            int triggerFromCameraContinuePort,
            int triggerFromCameraContinueBit,
            int triggerFromCameraTimeOut);

        bool StartScanning(AxesScanParameters scanParameters);
        bool StartConveyorBuffer(AcsBuffers buffer);
        bool SetReleaseCommandReceived(bool commandReceived);
        bool InitConveyorBufferParameters(BypassModeBufferParameters parameters);
        bool InitConveyorBufferParameters(ChangeWidthBufferParameters parameters);
        bool InitConveyorBufferParameters(FreePanelBufferParameters parameters);
        bool InitConveyorBufferParameters(InternalMachineLoadBufferParameters parameters);
        bool InitConveyorBufferParameters(LoadPanelBufferParameters parameters);
        bool InitConveyorBufferParameters(PowerOnRecoverFromEmergencyStopBufferParameters parameters);
        bool InitConveyorBufferParameters(PreReleasePanelBufferParameters parameters);
        bool InitConveyorBufferParameters(ReleasePanelBufferParameters parameters);
        bool InitConveyorBufferParameters(ReloadPanelBufferParameters parameters);
        bool InitConveyorBufferParameters(SecurePanelBufferParameters parameters);
        bool InitConveyorBufferParameters(HomeConveyorWidthParameters parameters);
        bool InitConveyorBufferParameters(DBufferParameters parameters);
        void Reset();
        void ClearError(GantryAxes axis);
        bool Enable(GantryAxes axis);
        bool Disable(GantryAxes axis);
        bool ReloadConfigParameters(bool forZOnly = false);
        bool ReloadConfigParameters(GantryAxes axis);
        bool Init(bool forZOnly = false);
        bool Init(List<AxisInitParameters> initParameters, bool forZOnly = false);
        bool Init(GantryAxes axis);
        bool Init(AxisInitParameters initParameters);
        bool MoveAbsolute(List<AxisMoveParameters> axesToMove);

        bool MoveAbsolute(
            GantryAxes axis,
            double targetPos,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0);

        bool MoveRelative(List<AxisMoveParameters> axesToMove);

        bool MoveRelative(
            GantryAxes axis,
            double relativePosition,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0);

        bool Jog(GantryAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0);
        bool StopAll();
        bool Stop(GantryAxes axis);
        bool Abort(GantryAxes axis);
        void SetRPos(GantryAxes axis, double pos);
        void StartPanelLoad(LoadPanelBufferParameters parameters, double panelLength, int timeout);
        void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout);
        void StopPanelLoad();
        void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout);
        void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout);
        IoStatus GetIoStatus();
        void SetOutputs(SetOutputParameters outputs);
        void ChangeConveyorWidth(ChangeWidthBufferParameters parameters, int timeout);
        void ApplicationError();
        void ResetError();
        double GetConveyorWidthAxisPosition();

        void PowerOnRecoverFromEmergencyStop(PowerOnRecoverFromEmergencyStopBufferParameters parameter,
            int timeout);

        PanelButtons GetPanelButtonsStatus();
        ClampSensors GetClampSensorsStatus();
        PresentSensors GetPresentSensorsStatus();
        SmemaIo GetSmemaIoStatus();

        bool IsBypassSignalSet();
        void BypassModeOn(BypassModeBufferParameters parameter);
        void BypassModeOff();
        void SetTowerLightRed(AcsIndicatorState state);
        void SetTowerLightYellow(AcsIndicatorState state);
        void SetTowerLightGreen(AcsIndicatorState state);
        void SetTowerLightBlue(AcsIndicatorState state);
        void SetTowerLightBuzzer(AcsIndicatorState state);
        void SetStartButtonIndicator(AcsIndicatorState state);
        void SetStopButtonIndicator(AcsIndicatorState state);
        void SetMachineReady();
        void ResetMachineReady();

        bool IsConveyorAxisEnable();
        bool IsConveyorWidthAxisEnable();
        void HomeConveyorWidthAxis(HomeConveyorWidthParameters parameter);
        void EnableConveyorAxis();
        void DisableConveyorAxis();
        void EnableConveyorWidthAxis();
        void DisableConveyorWidthAxis();
        void JogConveyorAxisLeftToRight(double velocity, double acceleration, double deceleration);
        void JogConveyorAxisRightToLeft(double velocity, double acceleration, double deceleration);
        void StopConveyorAxis();
        void EnableConveyorLifterAxis();
        void DisableConveyorLifterAxis();
        void HomeConveyorLifterAxis();
        void MoveConveyorLifter(double targetPosition);
        bool IsConveyorLifterAxisEnabled();
        double GetConveyorLifterAxisPosition();
        void SetAdditionalSettlingTime(int settlingTime);
        void SetBeforeMoveDelay(int beforeMoveDelay);
    }
}