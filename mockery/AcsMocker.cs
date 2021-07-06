using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CO.Systems.Services.Acs.AcsWrapper.wrapper;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;
using CO.Systems.Services.Robot.Interface;
using CO.Systems.Services.Robot.RobotBase;

namespace CO.Systems.Services.Acs.AcsWrapper.mockery
{
    internal class AcsMocker : IAcsWrapper
    {
        private readonly List<IPvTuple3D> motionPaths;
        private readonly Dictionary<GantryAxes, double> axesPosition;

        public AcsMocker()
        {
            ConveyorStatus = ConveyorStatusCode.SAFE_STATUS;
            ErrorCode = ConveyorErrorCode.ErrorSafe;

            HasError = HasConveyorError = HasRobotError = false;

            motionPaths = new List<IPvTuple3D>();
            axesPosition = new Dictionary<GantryAxes, double>
            {
                {GantryAxes.X, double.MinValue}, {GantryAxes.Y, double.MinValue}, {GantryAxes.Z, double.MinValue}
            };
        }

        #region Implementation of IAcsWrapper

        public bool IsSimulation => true;
        public bool IsConnected => true;
        public string FirmwareVersion => "Mocked";
        public uint NETLibraryVersion => 0;

        public ConveyorStatusCode ConveyorStatus { get; }
        public bool HasError { get; }
        public bool HasConveyorError { get; }
        public bool HasRobotError { get; }
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
        public event Action<GantryAxes, bool> AtPositiveHwLimitChanged;
        public event Action<GantryAxes, bool> AtNegativeHwLimitChanged;
        public event Action<GantryAxes, bool> AtPositiveSwLimitChanged;
        public event Action<GantryAxes, bool> AtNegativeSwLimitChanged;
        public event Action<GantryAxes> MovementBegin;
        public event Action<GantryAxes, bool> MovementEnd;
        public event Action<GantryAxes> OnAxisHomingBegin;
        public event Action<GantryAxes, bool> OnAxisHomingEnd;
        public event Action ScanningBegin;
        public event Action<int> HardwareNotifySingleMoveMotionCompleteReceived;
        public event Action<int> HardwareNotifySingleMovePSXAckReceived;
        public event Action<int> ScanningIndexChange;
        public event Action ScanningEnd;

        public void Connect()
        {
        }

        public bool Disconnect()
        {
            return true;
        }

        public bool IsIdle(GantryAxes axis)
        {
            return true;
        }

        public bool Enabled(GantryAxes axis)
        {
            return true;
        }

        public bool Homed(GantryAxes axis)
        {
            return true;
        }

        public bool Ready(GantryAxes axis)
        {
            return true;
        }

        public double Position(GantryAxes axis)
        {
            return axesPosition[axis];
        }

        public double Velocity(GantryAxes axis)
        {
            return 0;
        }

        public bool AtHomeSensor(GantryAxes axis)
        {
            return false;
        }

        public bool AtPositiveHwLimit(GantryAxes axis)
        {
            return false;
        }

        public bool AtNegativeHwLimit(GantryAxes axis)
        {
            return false;
        }

        public bool AtPositiveSwLimit(GantryAxes axis)
        {
            return false;
        }

        public bool AtNegativeSwLimit(GantryAxes axis)
        {
            return false;
        }

        public bool PrepareScanning(List<IPvTuple3D> pvTuple3DList, int triggerToCameraStartPort, int triggerToCameraStartBit,
            int triggerFromCameraContinuePort, int triggerFromCameraContinueBit, int triggerFromCameraTimeOut)
        {
            motionPaths.Clear();
            motionPaths.AddRange(pvTuple3DList);
            return true;
        }

        public bool StartScanning(AxesScanParameters scanParameters)
        {
            int scanningIndex = 0;

            Task.Run(() =>
            {
                foreach (var path in motionPaths) {
                    Thread.Sleep(200);

                    axesPosition[GantryAxes.X] = path.PvTuple[(int) GantryAxes.X].Position;
                    axesPosition[GantryAxes.Y] = path.PvTuple[(int) GantryAxes.Y].Position;
                    axesPosition[GantryAxes.Z] = path.PvTuple[(int) GantryAxes.Z].Position;
                    ScanningIndexChange?.Invoke(++scanningIndex);
                    HardwareNotifySingleMoveMotionCompleteReceived?.Invoke(scanningIndex);

                    Thread.Sleep(50);
                    HardwareNotifySingleMovePSXAckReceived?.Invoke(scanningIndex);
                }

                ScanningEnd?.Invoke();
            });

            return true;
        }

        public bool StartConveyorBuffer(AcsBuffers buffer)
        {
            return true;
        }

        public bool SetReleaseCommandReceived(bool commandReceived)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(BypassModeBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(ChangeWidthBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(FreePanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(InternalMachineLoadBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(LoadPanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(PowerOnRecoverFromEmergencyStopBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(PreReleasePanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(ReleasePanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(ReloadPanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(SecurePanelBufferParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(HomeConveyorWidthParameters parameters)
        {
            return true;
        }

        public bool InitConveyorBufferParameters(DBufferParameters parameters)
        {
            return true;
        }

        public void Reset()
        {
        }

        public void ClearError(GantryAxes axis)
        {
        }

        public bool Enable(GantryAxes axis)
        {
            return true;
        }

        public bool Disable(GantryAxes axis)
        {
            return true;
        }

        public bool ReloadConfigParameters(bool forZOnly = false)
        {
            return true;
        }

        public bool ReloadConfigParameters(GantryAxes axis)
        {
            return true;
        }

        public bool Init(bool forZOnly = false)
        {
            return true;
        }

        public bool Init(List<AxisInitParameters> initParameters, bool forZOnly = false)
        {
            Task.Run(() =>
            {
                OnAxisHomingBegin?.Invoke(GantryAxes.X);
                OnAxisHomingBegin?.Invoke(GantryAxes.Y);
                OnAxisHomingBegin?.Invoke(GantryAxes.Z);

                Thread.Sleep(5000);

                OnAxisHomingEnd?.Invoke(GantryAxes.X, true);
                OnAxisHomingEnd?.Invoke(GantryAxes.Y, true);
                OnAxisHomingEnd?.Invoke(GantryAxes.Z, true);
            });

            return true;
        }

        public bool Init(GantryAxes axis)
        {
            return true;
        }

        public bool Init(AxisInitParameters initParameters)
        {
            return true;
        }

        public bool MoveAbsolute(List<AxisMoveParameters> axesToMove)
        {
            foreach (var parameter in axesToMove) {
                axesPosition[parameter.Axis] = parameter.TargetPos;
            }
            return true;
        }

        public bool MoveAbsolute(GantryAxes axis, double targetPos, double vel = 0, double acc = 0, double dec = 0)
        {
            axesPosition[axis] = targetPos;
            return true;
        }

        public bool MoveRelative(List<AxisMoveParameters> axesToMove)
        {
            foreach (var parameter in axesToMove) {
                axesPosition[parameter.Axis] += parameter.TargetPos;
            }
            return true;
        }

        public bool MoveRelative(GantryAxes axis, double relativePosition, double vel = 0, double acc = 0, double dec = 0)
        {
            axesPosition[axis] += relativePosition;
            return true;
        }

        public bool Jog(GantryAxes axis, double vel = 0, double acc = 0, double dec = 0)
        {
            return true;
        }

        public bool StopAll()
        {
            return true;
        }

        public bool Stop(GantryAxes axis)
        {
            return true;
        }

        public bool Abort(GantryAxes axis)
        {
            return true;
        }

        public void SetRPos(GantryAxes axis, double pos)
        {
            axesPosition[axis] = pos;
        }

        public void StartPanelLoad(LoadPanelBufferParameters parameters,
            double panelLength,
            int timeout)
        {
            Thread.Sleep(1000);
        }

        public void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            Thread.Sleep(1000);
        }

        public void StopPanelLoad()
        {
        }

        public void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout)
        {
            Thread.Sleep(1000);
        }

        public void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout)
        {
            Thread.Sleep(1000);
        }

        public IoStatus GetIoStatus()
        {
            return new IoStatus();
        }

        public void SetOutputs(SetOutputParameters outputs)
        {
        }

        public void ChangeConveyorWidth(ChangeWidthBufferParameters parameters, int timeout)
        {
        }

        public void ApplicationError()
        {
        }

        public void ResetError()
        {
        }

        public double GetConveyorWidthAxisPosition()
        {
            return 0;
        }

        public void PowerOnRecoverFromEmergencyStop(PowerOnRecoverFromEmergencyStopBufferParameters parameter, int timeout)
        {
        }

        public PanelButtons GetPanelButtonsStatus()
        {
            return new PanelButtons();
        }

        public ClampSensors GetClampSensorsStatus()
        {
            return new ClampSensors();
        }

        public PresentSensors GetPresentSensorsStatus()
        {
            return new PresentSensors();
        }

        public SmemaIo GetSmemaIoStatus()
        {
            return new SmemaIo();
        }

        public bool IsBypassSignalSet()
        {
            return false;
        }

        public void BypassModeOn(BypassModeBufferParameters parameter)
        {
        }

        public void BypassModeOff()
        {
        }

        public void SetTowerLightRed(AcsIndicatorState state)
        {
        }

        public void SetTowerLightYellow(AcsIndicatorState state)
        {
        }

        public void SetTowerLightGreen(AcsIndicatorState state)
        {
        }

        public void SetTowerLightBlue(AcsIndicatorState state)
        {
        }

        public void SetTowerLightBuzzer(AcsIndicatorState state)
        {
        }

        public void SetStartButtonIndicator(AcsIndicatorState state)
        {
        }

        public void SetStopButtonIndicator(AcsIndicatorState state)
        {
        }

        public void SetMachineReady()
        {
        }

        public void ResetMachineReady()
        {
        }

        public bool IsConveyorAxisEnable()
        {
            return true;
        }

        public bool IsConveyorWidthAxisEnable()
        {
            return true;
        }

        public void HomeConveyorWidthAxis(HomeConveyorWidthParameters parameter)
        {
        }

        public void EnableConveyorAxis()
        {
        }

        public void DisableConveyorAxis()
        {
        }

        public void EnableConveyorWidthAxis()
        {
        }

        public void DisableConveyorWidthAxis()
        {
        }

        public void JogConveyorAxisLeftToRight(double velocity, double acceleration, double deceleration)
        {
        }

        public void JogConveyorAxisRightToLeft(double velocity, double acceleration, double deceleration)
        {
        }

        public void StopConveyorAxis()
        {
        }

        public void EnableConveyorLifterAxis()
        {
        }

        public void DisableConveyorLifterAxis()
        {
        }

        public void HomeConveyorLifterAxis()
        {
        }

        public void MoveConveyorLifter(double targetPosition)
        {
        }

        public bool IsConveyorLifterAxisEnabled()
        {
            return true;
        }

        public double GetConveyorLifterAxisPosition()
        {
            return 0;
        }

        public void SetAdditionalSettlingTime(int settlingTime)
        {
        }

        public void SetBeforeMoveDelay(int beforeMoveDelay)
        {
        }

        #endregion
    }
}