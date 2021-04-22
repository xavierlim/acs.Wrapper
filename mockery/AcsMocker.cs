using System;
using System.Collections.Generic;
using CO.Systems.Services.Acs.AcsWrapper.wrapper;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;
using CO.Systems.Services.Robot.Interface;
using CO.Systems.Services.Robot.RobotBase;

namespace CO.Systems.Services.Acs.AcsWrapper.mockery
{
    internal class AcsMocker : IAcsWrapper
    {
        #region Implementation of IAcsWrapper

        public bool IsConnected { get; }
        public string FirmwareVersion { get; }
        public uint NETLibraryVersion { get; }
        public ConveyorStatusCode ConveyorStatus { get; }
        public bool HasError { get; }
        public bool HasConveyorError { get; set; }
        public bool HasRobotError { get; set; }
        public ConveyorErrorCode ErrorCode { get; }
        public event Action<bool> ConnectionStatusChanged;
        public event Action<GantryAxes, bool> IdleChanged;
        public event Action<GantryAxes, bool> EnabledChanged;
        public event Action<GantryAxes, bool> ReadyChanged;
        public event Action<GantryAxes, double> PositionUpdated;
        public event Action<GantryAxes, double> VelocityUpdated;
        public event Action<GantryAxes, bool> StopDone;
        public event Action<GantryAxes, bool> AbortDone;
        public event Action<GantryAxes, bool> AtHomeSensorChanged;
        public event Action<GantryAxes, bool> AtPositiveHWLimitChanged;
        public event Action<GantryAxes, bool> AtNegativeHWLimitChanged;
        public event Action<GantryAxes, bool> AtPositiveSWLimitChanged;
        public event Action<GantryAxes, bool> AtNegativeSWLimitChanged;
        public event Action<GantryAxes> MovementBegin;
        public event Action<GantryAxes, bool> MovementEnd;
        public event Action<GantryAxes> AxisHomingBegin;
        public event Action<GantryAxes, bool> AxisHomingEnd;
        public event Action ScanningBegin;
        public event Action HardwareNotifySingleMoveMotionCompleteRecvd;
        public event Action HardwareNotifySingleMovePSXAckRecvd;
        public event Action<int> ScanningIndexChange;
        public event Action ScanningEnd;
        public void Connect(string ip)
        {
            throw new NotImplementedException();
        }

        public bool DisConnect()
        {
            throw new NotImplementedException();
        }

        public bool IsIdle(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Enabled(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Homed(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Ready(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public double Position(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public double Velocity(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool AtHomeSensor(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool AtPositiveHWLimit(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool AtNegativeHWLimit(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool AtPositiveSWLimit(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool AtNegativeSWLimit(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool PrepareScanning(List<IPvTuple3D> pvTuple3DList, int triggerToCameraStartPort, int triggerToCameraStartBit,
            int triggerFromCameraContinuePort, int triggerFromCameraContinueBit, int triggerFromCameraTimeOut)
        {
            throw new NotImplementedException();
        }

        public bool StartScanning()
        {
            throw new NotImplementedException();
        }

        public bool StartConveyorBuffer(AcsBuffers buffer)
        {
            throw new NotImplementedException();
        }

        public bool SetReleaseCommandReceived(bool commandReceived)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(BypassModeBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(ChangeWidthBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(FreePanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(InternalMachineLoadBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(LoadPanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(PowerOnRecoverFromEmergencyStopBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(PreReleasePanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(ReleasePanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(ReloadPanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(SecurePanelBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(HomeConveyorWidthParameters parameters)
        {
            throw new NotImplementedException();
        }

        public bool InitConveyorBufferParameters(DBufferParameters parameters)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void ClearError(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Enable(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Disable(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool ReloadConfigParameters(bool forZOnly = false)
        {
            throw new NotImplementedException();
        }

        public bool ReloadConfigParameters(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Init(bool forZOnly = false)
        {
            throw new NotImplementedException();
        }

        public bool Init(List<AxisInitParameters> initParameters, bool forZOnly = false)
        {
            throw new NotImplementedException();
        }

        public bool Init(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Init(AxisInitParameters initParameters)
        {
            throw new NotImplementedException();
        }

        public bool MoveAbsolute(List<AxisMoveParameters> axesToMove)
        {
            throw new NotImplementedException();
        }

        public bool MoveAbsolute(GantryAxes axis, double targetPos, double vel = 0, double acc = 0, double dec = 0)
        {
            throw new NotImplementedException();
        }

        public bool MoveRelative(List<AxisMoveParameters> axesToMove)
        {
            throw new NotImplementedException();
        }

        public bool MoveRelative(GantryAxes axis, double relativePosition, double vel = 0, double acc = 0, double dec = 0)
        {
            throw new NotImplementedException();
        }

        public bool Jog(GantryAxes axis, double vel = 0, double acc = 0, double dec = 0)
        {
            throw new NotImplementedException();
        }

        public bool StopAll()
        {
            throw new NotImplementedException();
        }

        public bool Stop(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public bool Abort(GantryAxes axis)
        {
            throw new NotImplementedException();
        }

        public void SetRPos(GantryAxes axis, double pos)
        {
            throw new NotImplementedException();
        }

        public void ReadAxesSettignsFromConfig()
        {
            throw new NotImplementedException();
        }

        public void StartPanelLoad(LoadPanelBufferParameters parameters,
            double panelLength,
            int timeout)
        {
            throw new NotImplementedException();
        }

        public void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            throw new NotImplementedException();
        }

        public void StopPanelLoad()
        {
            throw new NotImplementedException();
        }

        public void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout)
        {
            throw new NotImplementedException();
        }

        public void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout)
        {
            throw new NotImplementedException();
        }

        public IoStatus GetIoStatus()
        {
            throw new NotImplementedException();
        }

        public void SetOutputs(SetOutputParameters outputs)
        {
            throw new NotImplementedException();
        }

        public void ChangeConveyorWidth(ChangeWidthBufferParameters parameters, int timeout)
        {
            throw new NotImplementedException();
        }

        public void ApplicationError()
        {
            throw new NotImplementedException();
        }

        public void ResetError()
        {
            throw new NotImplementedException();
        }

        public double GetConveyorWidthAxisPosition()
        {
            throw new NotImplementedException();
        }

        public void PowerOnRecoverFromEmergencyStop(PowerOnRecoverFromEmergencyStopBufferParameters parameter, int timeout)
        {
            throw new NotImplementedException();
        }

        public PanelButtons GetPanelButtonsStatus()
        {
            throw new NotImplementedException();
        }

        public ClampSensors GetClampSensorsStatus()
        {
            throw new NotImplementedException();
        }

        public PresentSensors GetPresentSensorsStatus()
        {
            throw new NotImplementedException();
        }

        public SmemaIo GetSmemaIoStatus()
        {
            throw new NotImplementedException();
        }

        public bool IsBypassSignalSet()
        {
            throw new NotImplementedException();
        }

        public void BypassModeOn(BypassModeBufferParameters parameter)
        {
            throw new NotImplementedException();
        }

        public void BypassModeOff()
        {
            throw new NotImplementedException();
        }

        public void SetTowerLightRed(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetTowerLightYellow(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetTowerLightGreen(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetTowerLightBlue(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetTowerLightBuzzer(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetStartButtonIndicator(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public void SetStopButtonIndicator(AcsIndicatorState state)
        {
            throw new NotImplementedException();
        }

        public bool IsConveyorAxisEnable()
        {
            throw new NotImplementedException();
        }

        public bool IsConveyorWidthAxisEnable()
        {
            throw new NotImplementedException();
        }

        public void HomeConveyorWidthAxis(HomeConveyorWidthParameters parameter)
        {
            throw new NotImplementedException();
        }

        public void EnableConveyorAxis()
        {
            throw new NotImplementedException();
        }

        public void DisableConveyorAxis()
        {
            throw new NotImplementedException();
        }

        public void EnableConveyorWidthAxis()
        {
            throw new NotImplementedException();
        }

        public void DisableConveyorWidthAxis()
        {
            throw new NotImplementedException();
        }

        public void JogConveyorAxisLeftToRight(double velocity, double acceleration, double deceleration)
        {
            throw new NotImplementedException();
        }

        public void JogConveyorAxisRightToLeft(double velocity, double acceleration, double deceleration)
        {
            throw new NotImplementedException();
        }

        public void StopConveyorAxis()
        {
            throw new NotImplementedException();
        }

        public void EnableConveyorLifterAxis()
        {
            throw new NotImplementedException();
        }

        public void DisableConveyorLifterAxis()
        {
            throw new NotImplementedException();
        }

        public void HomeConveyorLifterAxis()
        {
            throw new NotImplementedException();
        }

        public void MoveConveyorLifter(double targetPosition)
        {
            throw new NotImplementedException();
        }

        public bool IsConveyorLifterAxisEnabled()
        {
            throw new NotImplementedException();
        }

        public double GetConveyorLifterAxisPosition()
        {
            throw new NotImplementedException();
        }

        public void SetAdditionalSettlingTime(int settlingTime)
        {
            throw new NotImplementedException();
        }

        public void SetBeforeMoveDelay(int beforeMoveDelay)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}