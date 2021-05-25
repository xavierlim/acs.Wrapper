
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ACS.SPiiPlusNET;
using CO.Common.Logger;
using CO.Systems.Services.Acs.AcsWrapper.config;
using CO.Systems.Services.Acs.AcsWrapper.util;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.exceptions;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;
using CO.Systems.Services.Configuration.Settings;
using CO.Systems.Services.Robot.Interface;
using CO.Systems.Services.Robot.RobotBase;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public class AcsWrapper : IAcsWrapper
    {
        private readonly Api api;
        private readonly AcsUtils acsUtils;
        private readonly BufferHelper bufferHelper;
        private readonly IRobotControlSetting robotSettings;
        private readonly ILogger logger;

        private readonly AutoResetEvent waitExitFromPoling = new AutoResetEvent(false);

        private bool scanLoopRunning;
        private const int SleepInterval = 50;

        private readonly Dictionary<GantryAxes, AcsAxis> axesCache = new Dictionary<GantryAxes, AcsAxis>();
        private readonly Dictionary<ConveyorAxes, AcsAxis> conveyorAxesCache = new Dictionary<ConveyorAxes, AcsAxis>();
        private readonly object lockObject = new object();

        private readonly bool isSimulation;
        private bool isScanningBufferRun;
        private bool isConveyorBufferRun;
        private bool isConnected;
        private int currentScanningIndex = -1;
        private int currentMotionCompleteReceived;
        private int currentMovePsxAckReceived;

        internal AcsWrapper(ILogger logger, IRobotControlSetting robotSettings)
        {
            isSimulation = AcsSimHelper.IsEnable();
            this.robotSettings = robotSettings;
            this.logger = logger;

            api = new Api();
            acsUtils = new AcsUtils(api);
            bufferHelper = new BufferHelper(api, acsUtils, isSimulation);
        }

        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if (value == isConnected) return;
                isConnected = value;
                ConnectionStatusChanged?.Invoke(isConnected);
            }
        }

        public string FirmwareVersion => api.GetFirmwareVersion();

        public uint NETLibraryVersion => api.GetNETLibraryVersion();

        public ConveyorStatusCode ConveyorStatus { get; private set; }

        public ConveyorErrorCode ErrorCode { get; private set; }

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

        public event Action HardwareNotifySingleMoveMotionCompleteReceived;

        public event Action HardwareNotifySingleMovePSXAckReceived;

        public event Action<int> ScanningIndexChange;

        public event Action ScanningEnd;

        public event Action<ConveyorAxes, bool> ConveyorAxisIdleChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisEnabledChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisReadyChanged;

        public event Action<ConveyorAxes, double> ConveyorAxisPositionUpdated;

        public event Action<ConveyorAxes, double> ConveyorAxisVelocityUpdated;

        public event Action<ConveyorAxes, bool> ConveyorAxisStopDone;

        public event Action<ConveyorAxes, bool> ConveyorAxisAbortDone;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtHomeSensorChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtPositiveHwLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtNegativeHwLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtPositiveSwLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtNegativeSwLimitChanged;

        public event Action<ConveyorAxes> ConveyorAxisMovementBegin;

        public event Action<ConveyorAxes, bool> ConveyorAxisMovementEnd;

        public event Action<ConveyorAxes> ConveyorAxisHomingBegin;

        public event Action<ConveyorAxes, bool> ConveyorAxisHomingEnd;

        public void Connect()
        {
            string ip = isSimulation ? "localhost" : "10.0.0.100";
            logger.Info($"AcsWrapper: Connect. IP {ip}");

            lock (lockObject) {
                teminateOldConnections();
                try {
                    api.OpenCommEthernet(ip, 700);
                }
                catch (Exception e) {
                    logger.Error($"AcsWrapper: Connect. Connection attempt exception [{e.Message}]");
                    throw new AcsException(e.Message);
                }

                IsConnected = api.IsConnected;
                if (!IsConnected) {
                    logger.Error("AcsWrapper:Connect. Controller not connected");
                    throw new AcsException("Controller not connected");
                }

                EnableAcsEvents();
                bufferHelper.InitDBuffer();
                InitAxesCache();
                InitConveyorAxesCache();
                InitAxisNumbersAtController();
                InitBuffers();
                ThreadPool.QueueUserWorkItem(s => ScanLoop());
            }
        }

        public bool Disconnect()
        {
            logger.Info("AcsWrapper: Disconnect");
            lock (lockObject) {
                if (api == null) {
                    IsConnected = false;
                    return true;
                }

                try {
                    api.CloseComm();
                    IsConnected = api.IsConnected;
                }
                catch (Exception e) {
                    logger.Error($"AcsWrapper: Disconnect Exception: {e.Message}");
                }

                scanLoopRunning = false;
                waitExitFromPoling.WaitOne(5000);
                return !IsConnected;
            }
        }

        public bool IsIdle(GantryAxes axis)
        {
            logger.Info(string.Format("IsIdle(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Idle;
            logger.Info("Controller not connected");
            return false;
        }

        public bool IsIdle(ConveyorAxes axis)
        {
            logger.Info(string.Format("IsIdle(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].Idle;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Enabled(GantryAxes axis)
        {
            logger.Info(string.Format("Enabled(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Enabled;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Enabled(ConveyorAxes axis)
        {
            logger.Info(string.Format("Enabled(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].Enabled;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Homed(GantryAxes axis)
        {
            logger.Info(string.Format("Homed(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Homed;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Homed(ConveyorAxes axis)
        {
            logger.Info(string.Format("Homed(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].Homed;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Ready(GantryAxes axis)
        {
            logger.Info(string.Format("Ready(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Ready;
            logger.Info("Controller not connected");
            return false;
        }

        public bool Ready(ConveyorAxes axis)
        {
            logger.Info(string.Format("Ready(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].Ready;
            logger.Info("Controller not connected");
            return false;
        }

        public double Position(GantryAxes axis)
        {
            logger.Info(string.Format("Position(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Position;
            logger.Info("Controller not connected");
            return 0.0;
        }

        public double Position(ConveyorAxes axis)
        {
            logger.Info(string.Format("Position(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].Position;
            logger.Info("Controller not connected");
            return 0.0;
        }

        public double Velocity(GantryAxes axis)
        {
            logger.Info(string.Format("Velocity(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].CurrentVelocity;
            logger.Info("Controller not connected");
            return 0.0;
        }

        public double Velocity(ConveyorAxes axis)
        {
            logger.Info(string.Format("Velocity(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].CurrentVelocity;
            logger.Info("Controller not connected");
            return 0.0;
        }

        public bool AtHomeSensor(GantryAxes axis)
        {
            logger.Info(string.Format("AtHomeSensor(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].AtHomeSensor;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtHomeSensor(ConveyorAxes axis)
        {
            logger.Info(string.Format("AtHomeSensor(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].AtHomeSensor;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtPositiveHwLimit(GantryAxes axis)
        {
            logger.Info(string.Format("AtPositiveHWLimit(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].AtPositiveHwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtPositiveHwLimit(ConveyorAxes axis)
        {
            logger.Info(string.Format("AtPositiveHWLimit(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].AtPositiveHwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtNegativeHwLimit(GantryAxes axis)
        {
            logger.Info(string.Format("AtNegativeHWLimit(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].AtNegativeHwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtNegativeHwLimit(ConveyorAxes axis)
        {
            logger.Info(string.Format("AtNegativeHWLimit(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].AtNegativeHwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtPositiveSwLimit(GantryAxes axis)
        {
            logger.Info(string.Format("AtPositiveSWLimit(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].AtPositiveSwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtPositiveSwLimit(ConveyorAxes axis)
        {
            logger.Info(string.Format("AtPositiveSWLimit(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].AtPositiveSwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtNegativeSwLimit(GantryAxes axis)
        {
            logger.Info(string.Format("AtNegativeSWLimit(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].AtNegativeSwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        public bool AtNegativeSwLimit(ConveyorAxes axis)
        {
            logger.Info(string.Format("AtNegativeSWLimit(axis = {0})", axis));

            if (IsConnected) return conveyorAxesCache[axis].AtNegativeSwLimit;
            logger.Info("Controller not connected");
            return false;
        }

        private bool GetInput(int port, int bit)
        {
            logger.Info(string.Format("GetInput(port = {0},bit = {1})", port, bit));

            if (IsConnected) return Convert.ToBoolean(api.GetInput(port, bit));
            logger.Info("Controller not connected");
            return false;
        }

        private bool GetOutput(int port, int bit)
        {
            logger.Info(string.Format("GetOutput(port = {0},bit = {1})", port, bit));

            if (IsConnected) return Convert.ToBoolean(api.GetOutput(port, bit));
            logger.Info("Controller not connected");
            return false;
        }

        private void SetOutput(int port, int bit, bool value)
        {
            logger.Info(string.Format("SetOutput(port = {0},bit = {1},value = {2})", port, bit, value));
            if (IsConnected) {
                api.SetOutput(port, bit, Convert.ToInt32(value));
            }
            else {
                logger.Info("Controller not connected");
            }
        }

        public bool PrepareScanning(List<IPvTuple3D> pvTuple3DList, int triggerToCameraStartPort,
            int triggerToCameraStartBit, int triggerFromCameraContinuePort, int triggerFromCameraContinueBit,
            int triggerFromCameraTimeOut)
        {
            logger.Info(
                string.Format(
                    "AcsWrapper.PrepareScanning: triggerToCameraStartPort = {0},triggerToCameraStartBit = {1},triggerFromCameraContinuePort = {2},triggerFromCameraContinueBit = {3},triggerFromCameraTimeOut = {4}))",
                    triggerToCameraStartPort, triggerToCameraStartBit, triggerFromCameraContinuePort,
                    triggerFromCameraContinueBit, triggerFromCameraTimeOut));

            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            bufferHelper.PrepareScanningBuffer(pvTuple3DList, triggerToCameraStartPort, triggerToCameraStartBit,
                triggerFromCameraContinuePort, triggerFromCameraContinueBit, triggerFromCameraTimeOut);
            return true;
        }

        public bool StartScanning(AxesScanParameters scanParameters)
        {
            logger.Info("AcsWrapper.StartScanning");

            if (!IsConnected) {
                logger.Info("AcsWrapper.StartScanning: Controller not connected");
                return false;
            }

            axesCache[GantryAxes.X].UpdateScanningProfiles(scanParameters.AxisX);
            axesCache[GantryAxes.Y].UpdateScanningProfiles(scanParameters.AxisY);
            axesCache[GantryAxes.Z].UpdateScanningProfiles(scanParameters.AxisZ);

            CurrentScanningIndex = -1;
            CurrentMotionCompleteReceived = 0;
            CurrentMovePsxAckReceived = 0;
            acsUtils.RunBuffer(ProgramBuffer.ACSC_BUFFER_9);
            isScanningBufferRun = acsUtils.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9);

            if (isScanningBufferRun) {
                foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache)
                    keyValuePair.Value.ScanningBufferRun = true;
                ScanningBegin?.Invoke();
            }

            return isScanningBufferRun;
        }

        public bool StartConveyorBuffer(AcsBuffers buffer)
        {
            logger.Info(string.Format("StartConveyorBuffer({0})", buffer));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.RunBuffer((ProgramBuffer) buffer);
            isConveyorBufferRun = acsUtils.IsProgramRunning((ProgramBuffer) buffer);
            return isConveyorBufferRun;
        }

        public bool SetReleaseCommandReceived(bool commandReceived)
        {
            logger.Info(string.Format("SetReleaseCommandReceived({0})", commandReceived));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            int nBuf = 19;
            acsUtils.WriteVariable(commandReceived ? 1 : 0, "ReleaseCommandReceived", nBuf);
            return true;
        }

        public bool InitConveyorBufferParameters(BypassModeBufferParameters parameters)
        {
            logger.Info("InitBypassModeBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "BypassModeBuffer_WaitTimeToSearch");
            acsUtils.WriteVariable(parameters.WaitTimeToAcq, "BypassModeBuffer_WaitTimeToAcq");
            acsUtils.WriteVariable(parameters.WaitTimeToCutout, "BypassModeBuffer_WaitTimeToCutout");
            acsUtils.WriteVariable(parameters.WaitTimeToExit, "BypassModeBuffer_WaitTimeToExit");
            acsUtils.WriteVariable(parameters.WaitTimeToRelease, "BypassModeBuffer_WaitTimeToRelease");
            acsUtils.WriteVariable(parameters.WaitTimeToSmema, "BypassModeBuffer_WaitTimeToSmema");

            return true;
        }

        public bool InitConveyorBufferParameters(ChangeWidthBufferParameters parameters)
        {
            logger.Info("InitChangeWidthBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.ChangeWidth;

            acsUtils.WriteVariable(parameters.ConveyorSpecifiedWidth, "ConveyorSpecifiedWidth", buffer);
            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "ChangeWidthBuffer_WaitTimeToSearch");


            return true;
        }

        public bool InitConveyorBufferParameters(FreePanelBufferParameters parameters)
        {
            logger.Info("InitFreePanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.UnclampLiftDelayTime, "FreePanelBuffer_UnclampLiftDelayTime");
            acsUtils.WriteVariable(parameters.WaitTimeToUnlift, "FreePanelBuffer_WaitTimeToUnlift");
            acsUtils.WriteVariable(parameters.WaitTimeToUnclamp, "FreePanelBuffer_WaitTimeToUnclamp");

            return true;
        }

        public bool InitConveyorBufferParameters(InternalMachineLoadBufferParameters parameters)
        {
            logger.Info("InitInternalMachineLoadBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToSlow, "InternalMachineLoadBuffer_WaitTimeToSlow");
            acsUtils.WriteVariable(parameters.WaitTimeToAlign, "InternalMachineLoadBuffer_WaitTimeToAlign");
            acsUtils.WriteVariable(parameters.SlowDelayTime, "InternalMachineLoadBuffer_SlowDelayTime");

            return true;
        }

        public bool InitConveyorBufferParameters(LoadPanelBufferParameters parameters)
        {
            logger.Info("InitLoadPanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToAcq, "LoadPanelBuffer_WaitTimeToAcq");
            return true;
        }

        public bool InitConveyorBufferParameters(PowerOnRecoverFromEmergencyStopBufferParameters parameters)
        {
            logger.Info("InitPowerOnRecoverFromEmergencyStopBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.PowerOnRecoverFromEmergencyStop;

            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "PowerOnRecoveryBuffer_WaitTimeToSearch");
            acsUtils.WriteVariable(parameters.WaitTimeToExit, "PowerOnRecoveryBuffer_WaitTimeToExit");
            acsUtils.WriteVariable(parameters.WaitTimeToReset, "PowerOnRecoveryBuffer_WaitTimeToReset");
            acsUtils.WriteVariable(parameters.WidthToW_0_Position, "WidthToW_0_Position", buffer);

            return true;
        }

        public bool InitConveyorBufferParameters(PreReleasePanelBufferParameters parameters)
        {
            logger.Info("InitPreReleasePanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToExit, "PreReleasePanelBuffer_WaitTimeToExit");
            return true;
        }

        public bool InitConveyorBufferParameters(ReleasePanelBufferParameters parameters)
        {
            logger.Info("InitReleasePanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToExit, "ReleasePanelBuffer_WaitTimeToExit");
            acsUtils.WriteVariable(parameters.WaitTimeToRelease, "ReleasePanelBuffer_WaitTimeToRelease");
            acsUtils.WriteVariable(parameters.WaitTimeToSmema, "ReleasePanelBuffer_WaitTimeToSmema");
            acsUtils.WriteVariable(parameters.WaitTimeToCutout, "ReleasePanelBuffer_WaitTimeToCutout");
            acsUtils.WriteVariable(parameters.WaitTimeToBeltVacuum, "ReleasePanelBuffer_WaitTimeToBeltVacuum");

            return true;
        }

        public bool InitConveyorBufferParameters(ReloadPanelBufferParameters parameters)
        {
            logger.Info("InitReloadPanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "ReloadPanelBuffer_WaitTimeToSearch");
            acsUtils.WriteVariable(parameters.ReloadDelayTime, "ReloadPanelBuffer_ReloadDelayTime");

            return true;
        }

        public bool InitConveyorBufferParameters(SecurePanelBufferParameters parameters)
        {
            logger.Info("InitSecurePanelBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteVariable(parameters.ClampLiftDelayTime, "SecurePanelBuffer_ClampLiftDelayTime");
            acsUtils.WriteVariable(parameters.WaitTimeToPanelClamped, "SecurePanelBuffer_WaitTimeToPanelClamped");
            acsUtils.WriteVariable(parameters.WaitTimeToLifted, "SecurePanelBuffer_WaitTimeToLifted");
            acsUtils.WriteVariable(parameters.WaitTimeToUnstop, "SecurePanelBuffer_WaitTimeToUnstop");

            return true;
        }

        public bool InitConveyorBufferParameters(HomeConveyorWidthParameters parameters)
        {
            logger.Info("InitHomeConveyorWidthParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteGlobalReal(parameters.HOME_VEL_IN, "HOME_VEL_IN", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HOME_VEL_OUT, "HOME_VEL_OUT", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HOME_OFFSET, "HOME_OFFSET", (int)ConveyorAxes.Width);

            return true;
        }

        public bool InitConveyorBufferParameters(DBufferParameters parameters)
        {
            logger.Info("InitDBufferParameters()");
            if (!IsConnected)
            {
                logger.Info("Controller not connected");
                return false;
            }
            int buffer = acsUtils.GetDBufferIndex();

            acsUtils.WriteVariable(parameters.ConveyorBeltAcquireSpeed, "ConveyorBeltAcquireSpeed", buffer);
            acsUtils.WriteVariable(parameters.ConveyorBeltLoadingSpeed, "ConveyorBeltLoadingSpeed", buffer);
            acsUtils.WriteVariable(parameters.ConveyorBeltSlowSpeed, "ConveyorBeltSlowSpeed", buffer);
            acsUtils.WriteVariable(parameters.ConveyorBeltReleaseSpeed, "ConveyorBeltReleaseSpeed", buffer);
            acsUtils.WriteVariable(parameters.ConveyorBeltUnloadingSpeed, "ConveyorBeltUnloadingSpeed", buffer);
            acsUtils.WriteVariable(Convert.ToInt32(parameters.PingPongMode), "PingPongMode", buffer);

            acsUtils.WriteVariable(parameters.ConveyorDirection, "ConveyorDirection", buffer);
            acsUtils.WriteVariable(parameters.ConveyorWaitTimeToAlign, "InternalMachineLoadBuffer_WaitTimeToAlign", buffer);

            // TODO: enable?
            // acsUtils.WriteVariable(parameters.DistanceBetweenEntryAndStopSensor, "DistanceBetweenEntryAndStopSensor", buffer);
            acsUtils.WriteVariable(parameters.DistanceBetweenSlowPositionAndStopSensor, "DistanceBetweenSlowPositionAndStopSensor", buffer);
            acsUtils.WriteVariable(parameters.DistanceBetweenSlowPositionAndEntrySensor, "DistanceBetweenSlowPositionAndEntrySensor", buffer);
            // acsUtils.WriteVariable(parameters.DistanceBetweenStopSensorAndExitSensor, "DistanceBetweenStopSensorAndExitSensor", buffer);
            // acsUtils.WriteVariable(parameters.DistanceBetweenSlowPositionAndExitSensor, "DistanceBetweenSlowPositionAndExitSensor", buffer);

            acsUtils.WriteVariable(parameters.Stage_1_LifterSpeed, "Stage_1_LifterSpeed", buffer);
            acsUtils.WriteVariable(parameters.Stage_2_LifterSpeed, "Stage_2_LifterSpeed", buffer);
            acsUtils.WriteVariable(parameters.LifterDownSpeed, "LifterDownSpeed", buffer);

            return true;
        }

        public bool InitConveyorBufferParameters(ConveyorDirection conveyorDirection)
        {
            logger.Info("Init ConveyorDirection");
            if (!IsConnected) {
                logger.Error("Controller not connected");
                return false;
            }

            int dBufferIndex = acsUtils.GetDBufferIndex();
            acsUtils.WriteVariable((int) conveyorDirection, "ConveyorDirection", dBufferIndex);
            return true;
        }

        public void Reset()
        {
            logger.Info("Reset()");
            if (!IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                    keyValuePair.Value.ClearError();
                    keyValuePair.Value.RestoreDefaultSettings();
                }
            }
        }

        public void ClearError(GantryAxes axis)
        {
            logger.Info(string.Format("ClearError(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                axesCache[axis].ClearError();
            }
        }

        public bool Enable(GantryAxes axis)
        {
            logger.Info(string.Format("Enable(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Enable();
            logger.Info("Controller not connected");
            return false;
        }

        public bool Disable(GantryAxes axis)
        {
            logger.Info(string.Format("Disable(axis = {0})", axis));

            if (IsConnected) return axesCache[axis].Disable();
            logger.Info("Controller not connected");
            return false;
        }

        public bool ReloadConfigParameters(bool forZOnly = false)
        {
            logger.Info(string.Format("ReloadConfigParameters(forZOnly = {0})", forZOnly));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (forZOnly) {
                return ReloadConfigParameters(GantryAxes.Z);
            }

            foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                ReloadConfigParameters(keyValuePair.Key);
            }
            return true;
        }

        public bool ReloadConfigParameters(GantryAxes axis)
        {
            logger.Info(string.Format("ReloadConfigParameters(axis = {0})", axis));
            if (IsConnected) {
                axesCache[axis].ReloadConfigParameters();
                return true;
            }

            logger.Info("Controller not connected");
            return false;
        }

        public bool Init(bool forZOnly = false)
        {
            logger.Info(string.Format("Init(forZOnly = {0})", forZOnly));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (forZOnly)
                return Init(GantryAxes.Z);
            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                KeyValuePair<GantryAxes, AcsAxis> item = keyValuePair;
                taskList.Add(Task.Run((Action) (() => Init(item.Key))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool Init(List<AxisInitParameters> initParameters, bool forZOnly = false)
        {
            logger.Info($"Init(List<AxisInitParameters> initParameters,forZOnly = {forZOnly})");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (initParameters == null || initParameters.Count == 0)
                return false;
            if (forZOnly) {
                AxisInitParameters initParameters1;
                try {
                    initParameters1 =
                        initParameters.Find(item => item.Axis == GantryAxes.Z);
                }
                catch (Exception ex) {
                    return false;
                }

                return Init(initParameters1);
            }

            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                KeyValuePair<GantryAxes, AcsAxis> item = keyValuePair;
                AxisInitParameters axisInitParams = null;
                try {
                    axisInitParams =
                        initParameters.Find(item2 => item2.Axis == item.Key);
                }
                catch (Exception ex) {
                    continue;
                }

                if (axisInitParams != null)
                    taskList.Add(Task.Run((Action) (() => Init(axisInitParams))));
            }

            if (taskList.Count == 0)
                return false;
            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool Init(GantryAxes axis)
        {
            logger.Info(string.Format("Init(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Init(true);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Init(AxisInitParameters initParameters)
        {
            logger.Info("Init(AxisInitParameters initParameters)");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (initParameters == null)
                return false;
            if (axesCache.ContainsKey(initParameters.Axis))
                return axesCache[initParameters.Axis].Init(initParameters, true);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveAbsolute(List<AxisMoveParameters> axesToMove)
        {
            logger.Info("MoveAbsolute(List<AxisMoveParameters> axesToMove)");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0) {
                if (axesToMove == null)
                    logger.Info("axesToMove = null");
                else
                    logger.Info("axesToMove.Count = 0");
                return false;
            }

            List<Task> taskList = new List<Task>();
            foreach (AxisMoveParameters axisMoveParameters in axesToMove) {
                AxisMoveParameters axisToMove = axisMoveParameters;
                taskList.Add(Task.Run((Action) (() => MoveAbsolute(axisToMove.Axis, axisToMove.TargetPos,
                    axisToMove.Velocity, axisToMove.Accel, axisToMove.Decel))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool MoveAbsolute(GantryAxes axis, double targetPos, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            logger.Info(string.Format("MoveAbsolute(axis = {0},targetPos= {1}, vel= {2}, acc= {3}, dec= {4})", axis,
                targetPos, vel, acc, dec));

            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveRelative(List<AxisMoveParameters> axesToMove)
        {
            logger.Info("MoveRelative(List<AxisMoveParameters> axesToMove)");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0)
                return false;
            List<Task> taskList = new List<Task>();
            foreach (AxisMoveParameters axisMoveParameters in axesToMove) {
                AxisMoveParameters axisToMove = axisMoveParameters;
                taskList.Add(Task.Run((Action) (() => MoveRelative(axisToMove.Axis, axisToMove.TargetPos,
                    axisToMove.Velocity, axisToMove.Accel, axisToMove.Decel))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool MoveRelative(GantryAxes axis, double relativePosition, double vel = 0.0, double acc = 0.0,
            double dec = 0.0)
        {
            logger.Info(string.Format("MoveRelative(axis = {0},relativePosition= {1}, vel= {2}, acc= {3}, dec= {4})",
                axis, relativePosition, vel, acc, dec));

            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Jog(GantryAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            logger.Info(string.Format("Jog(axis = {0}, vel= {1}, acc= {2}, dec= {3})", axis, vel, acc, dec));

            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Jog(true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool StopAll()
        {
            logger.Info("StopAll()");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
            foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache)
                keyValuePair.Value.Stop();
            return true;
        }

        public bool Stop(GantryAxes axis)
        {
            logger.Info(string.Format("Stop(axis={0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Stop();
            throw new ArgumentException("Axis not exist ");
        }

        public bool Abort(GantryAxes axis)
        {
            logger.Info(string.Format("Abort(axis={0})", axis), 1476, nameof(Abort),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1479, nameof(Abort),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Abort();
            throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(GantryAxes axis, double pos)
        {
            logger.Info(string.Format("SetRPos(axis={0},pos={1})", axis, pos), 1499,
                nameof(SetRPos),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1502, nameof(SetRPos),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (axesCache.ContainsKey(axis))
                    axesCache[axis].SetRPos(pos);
                throw new ArgumentException("Axis not exist ");
            }
        }

        public void ResetConveyorAxes()
        {
            logger.Info("Reset()", 1877, nameof(ResetConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1880, nameof(ResetConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                foreach (KeyValuePair<ConveyorAxes, AcsAxis> keyValuePair in conveyorAxesCache) {
                    keyValuePair.Value.ClearError();
                    keyValuePair.Value.RestoreDefaultSettings();
                }
            }
        }

        public void ClearError(ConveyorAxes axis)
        {
            logger.Info(string.Format("ClearError(axis = {0})", axis), 1896, nameof(ClearError),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1899, nameof(ClearError),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (conveyorAxesCache.ContainsKey(axis))
                    conveyorAxesCache[axis].ClearError();
                throw new ArgumentException("Axis not exist ");
            }
        }

        public bool Enable(ConveyorAxes axis)
        {
            logger.Info(string.Format("Enable(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Enable();
            throw new ArgumentException("Axis not exist ");
        }

        public bool Disable(ConveyorAxes axis)
        {
            logger.Info(string.Format("Disable(axis = {0})", axis), 1936, nameof(Disable),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1939, nameof(Disable),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Disable();
            throw new ArgumentException("Axis not exist ");
        }

        public bool ReloadConfigParameters(ConveyorAxes axis)
        {
            logger.Info(string.Format("ReloadConfigParameters(axis = {0})", axis), 1952,
                nameof(ReloadConfigParameters),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1955, nameof(ReloadConfigParameters),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (!conveyorAxesCache.ContainsKey(axis))
                throw new ArgumentException("Axis not exist ");
            conveyorAxesCache[axis].ReloadConfigParameters();
            return true;
        }

        public bool InitConveyorAxes()
        {
            logger.Info("Init()", 1977, nameof(InitConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 1980, nameof(InitConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<ConveyorAxes, AcsAxis> keyValuePair in conveyorAxesCache) {
                KeyValuePair<ConveyorAxes, AcsAxis> item = keyValuePair;
                taskList.Add(Task.Run((Action) (() => Init(item.Key))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool Init(ConveyorAxes axis)
        {
            logger.Info(string.Format("Init(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Init(true);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveAbsolute(List<ConveyorAxesMoveParameters> axesToMove)
        {
            logger.Info("MoveAbsolute(List<ConveyorAxesMoveParameters> axesToMove)", 2035, nameof(MoveAbsolute),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2038, nameof(MoveAbsolute),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0) {
                if (axesToMove == null)
                    logger.Info("axesToMove = null", 2045, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                else
                    logger.Info("axesToMove.Count = 0", 2047, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            List<Task> taskList = new List<Task>();
            foreach (ConveyorAxesMoveParameters axesMoveParameters in axesToMove) {
                ConveyorAxesMoveParameters axisToMove = axesMoveParameters;
                taskList.Add(Task.Run((Action) (() => MoveAbsolute(axisToMove.Axis, axisToMove.TargetPos,
                    axisToMove.Velocity, axisToMove.Accel, axisToMove.Decel))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool MoveAbsolute(
            ConveyorAxes axis,
            double targetPos,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            logger.Info(
                string.Format("MoveAbsolute(axis = {0},targetPos= {1}, vel= {2}, acc= {3}, dec= {4})", axis,
                    targetPos, vel, acc, dec), 2076, nameof(MoveAbsolute),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2079, nameof(MoveAbsolute),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveRelative(List<ConveyorAxesMoveParameters> axesToMove)
        {
            logger.Info("MoveRelative(List<AxisMoveParameters> axesToMove)", 2102, nameof(MoveRelative),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2105, nameof(MoveRelative),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0)
                return false;
            List<Task> taskList = new List<Task>();
            foreach (ConveyorAxesMoveParameters axesMoveParameters in axesToMove) {
                ConveyorAxesMoveParameters axisToMove = axesMoveParameters;
                taskList.Add(Task.Run((Action) (() => MoveRelative(axisToMove.Axis, axisToMove.TargetPos,
                    axisToMove.Velocity, axisToMove.Accel, axisToMove.Decel))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool MoveRelative(
            ConveyorAxes axis,
            double relativePosition,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            logger.Info(
                string.Format("MoveRelative(axis = {0},relativePosition= {1}, vel= {2}, acc= {3}, dec= {4})",
                    axis, relativePosition, vel, acc, dec), 2137,
                nameof(MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2140, nameof(MoveRelative),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Jog(ConveyorAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            logger.Info(
                string.Format("Jog(axis = {0}, vel= {1}, acc= {2}, dec= {3})", axis, vel,
                    acc, dec), 2160, nameof(Jog),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2163, nameof(Jog),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Jog(true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool StopAllConveyorAxes()
        {
            logger.Info("StopAll()", 2179, nameof(StopAllConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2182, nameof(StopAllConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            for (AcsBuffers acsBuffers = AcsBuffers.ConveyorHoming;
                acsBuffers <= AcsBuffers.InternalErrorExit;
                ++acsBuffers)
                acsUtils.StopBuffer((ProgramBuffer) acsBuffers);
            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_55);
            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_56);
            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_57);
            foreach (KeyValuePair<ConveyorAxes, AcsAxis> keyValuePair in conveyorAxesCache)
                keyValuePair.Value.Stop();
            return true;
        }

        public bool Stop(ConveyorAxes axis)
        {
            logger.Info(string.Format("Stop(axis={0})", axis), 2210, nameof(Stop),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2213, nameof(Stop),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Stop();
            throw new ArgumentException("Axis not exist ");
        }

        public bool Abort(ConveyorAxes axis)
        {
            logger.Info(string.Format("Abort(axis={0})", axis), 2231, nameof(Abort),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2234, nameof(Abort),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Abort();
            throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(ConveyorAxes axis, double pos)
        {
            logger.Info(string.Format("SetRPos(axis={0},pos={1})", axis, pos), 2254,
                nameof(SetRPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                logger.Info("Controller not connected", 2257, nameof(SetRPos),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (conveyorAxesCache.ContainsKey(axis))
                    conveyorAxesCache[axis].SetRPos(pos);
                throw new ArgumentException("Axis not exist ");
            }
        }

        private void ScanLoop()
        {
            scanLoopRunning = true;
            while (scanLoopRunning) {
                lock (lockObject) {
                    try {
                        IsConnected = api.IsConnected;
                    }
                    catch (Exception ex1) {
                        var acsException1 = ex1 as ACSException;
                        if (acsException1 != null) {
                            logger.Info(acsException1.Message);
                        }

                        try {
                            IsConnected = api.IsConnected;
                        }
                        catch (Exception ex2) {
                            var acsException = ex2 as ACSException;
                            if (acsException != null)
                                logger.Info(acsException.Message);
                            IsConnected = false;
                        }
                    }

                    if (!IsConnected) continue;
                }

                if (isScanningBufferRun) {
                    CurrentScanningIndex =
                        Convert.ToInt32(acsUtils.ReadVar("CURRENT_STEP_INDEX", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentMotionCompleteReceived =
                        Convert.ToInt32(
                            acsUtils.ReadVar("MOVE_MOTION_COMPLETE_RECVD", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentMovePsxAckReceived =
                        Convert.ToInt32(acsUtils.ReadVar("MOVE_PSX_ACK_RECVD", ProgramBuffer.ACSC_BUFFER_9));

                    if (!acsUtils.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9)) {
                        isScanningBufferRun = false;
                        ScanningEnd?.Invoke();
                        foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                            keyValuePair.Value.ScanningBufferRun = false;
                        }
                    }
                }

                foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                    keyValuePair.Value.GetDataFromController();
                }
                foreach (KeyValuePair<ConveyorAxes, AcsAxis> keyValuePair in conveyorAxesCache) {
                    keyValuePair.Value.GetDataFromController();
                }

                Thread.Sleep(SleepInterval);
            }

            waitExitFromPoling.Set();
        }

        private void EnableAcsEvents()
        {
            if (api == null) return;

            api.EnableEvent(Interrupts.ACSC_INTR_EMERGENCY);
            api.EnableEvent(Interrupts.ACSC_INTR_ETHERCAT_ERROR);
            api.EnableEvent(Interrupts.ACSC_INTR_MESSAGE);
            api.EnableEvent(Interrupts.ACSC_INTR_MOTION_FAILURE);
            api.EnableEvent(Interrupts.ACSC_INTR_MOTOR_FAILURE);
            api.EnableEvent(Interrupts.ACSC_INTR_SYSTEM_ERROR);
            api.EnableEvent(Interrupts.ACSC_INTR_COMMAND);
            api.EMERGENCY += ApiEmergency;
            api.SYSTEMERROR += ApiSystemerror;
            api.MOTORFAILURE += ApiMotorfailure;
            api.MOTIONFAILURE += ApiMotionfailure;
            api.ETHERCATERROR += ApiEthercaterror;
            api.MESSAGE += ApiMessage;
            api.ACSPLPROGRAMEX += ApiAcsplprogramex;
        }

        private void ApiMessage(ulong Param)
        {
        }

        private void ApiEthercaterror(ulong Param)
        {
        }

        private void ApiMotionfailure(AxisMasks Param)
        {
        }

        private void ApiMotorfailure(AxisMasks Param)
        {
        }

        private void ApiSystemerror(ulong Param)
        {
            int num = api.GetLastError() + 1;
            try {
                api.GetErrorString(api.GetLastError());
            }
            catch (ACSException ex) {
            }
        }

        private void ApiEmergency(ulong Param)
        {
        }

        private void ApiAcsplprogramex(ulong Param)
        {
        }

        private void InitAxesCache()
        {
            axesCache.Clear();
            for (var gantryAxes = GantryAxes.Z; gantryAxes < GantryAxes.All; ++gantryAxes) {
                var acsAxis = new AcsAxis(api, acsUtils, gantryAxes, GetAcsAxisIndex(gantryAxes), robotSettings);

                axesCache[gantryAxes] = acsAxis;
                acsAxis.IdleChanged += axisIdleChanged;
                acsAxis.EnabledChanged += axisEnabledChanged;
                acsAxis.ReadyChanged += axisReadyChanged;
                acsAxis.PositionUpdated += axisPositionUpdated;
                acsAxis.VelocityUpdated += axisVelocityUpdated;
                acsAxis.MovementBegin += axisMovementBegin;
                acsAxis.MovementEnd += axisMovementEnd;
                acsAxis.StopDone += axisStopDone;
                acsAxis.AbortDone += axisAbortDone;
                acsAxis.AtHomeSensorChanged += axisAtHomeSensorChanged;
                acsAxis.AtPositiveHwLimitChanged += axisAtPositiveHWLimitChanged;
                acsAxis.AtNegativeHwLimitChanged += axisAtNegativeHWLimitChanged;
                acsAxis.AtPositiveSwLimitChanged += axisAtPositiveSWLimitChanged;
                acsAxis.AtNegativeSwLimitChanged += axisAtNegativeSWLimitChanged;
                acsAxis.AxisHomingBegin += Axis_AxisHomingBegin;
                acsAxis.AxisHomingEnd += Axis_AxisHomingEnd;

                if (acsAxis.AcsAxisId >= Axis.ACSC_AXIS_0) {
                    api.Halt(acsAxis.AcsAxisId);
                }
            }
        }

        private void InitConveyorAxesCache()
        {
            conveyorAxesCache.Clear();
            for (var conveyorAxes = ConveyorAxes.Conveyor; conveyorAxes <= ConveyorAxes.Lifter; ++conveyorAxes) {
                var acsAxis = new AcsAxis(api, acsUtils, conveyorAxes, GetAcsAxisIndex(conveyorAxes));

                conveyorAxesCache[conveyorAxes] = acsAxis;
                acsAxis.IdleChanged += conveyorAxisIdleChanged;
                acsAxis.EnabledChanged += conveyorAxisEnabledChanged;
                acsAxis.ReadyChanged += conveyorAxisReadyChanged;
                acsAxis.PositionUpdated += conveyorAxisPositionUpdated;
                acsAxis.VelocityUpdated += conveyorAxisVelocityUpdated;
                acsAxis.MovementBegin += conveyorAxisMovementBegin;
                acsAxis.MovementEnd += conveyorAxisMovementEnd;
                acsAxis.StopDone += conveyorAxisStopDone;
                acsAxis.AbortDone += conveyorAxisAbortDone;
                acsAxis.AtHomeSensorChanged += conveyorAxisAtHomeSensorChanged;
                acsAxis.AtPositiveHwLimitChanged += conveyorAxisAtPositiveHWLimitChanged;
                acsAxis.AtNegativeHwLimitChanged += conveyorAxisAtNegativeHWLimitChanged;
                acsAxis.AtPositiveSwLimitChanged += conveyorAxisAtPositiveSWLimitChanged;
                acsAxis.AtNegativeSwLimitChanged += conveyorAxisAtNegativeSWLimitChanged;
                acsAxis.AxisHomingBegin += conveyorAxis_AxisHomingBegin;
                acsAxis.AxisHomingEnd += conveyorAxis_AxisHomingEnd;

                if (acsAxis.AcsAxisId >= Axis.ACSC_AXIS_0) {
                    api.Halt(acsAxis.AcsAxisId);
                }
            }
        }

        private Axis GetAcsAxisIndex(GantryAxes gantryAxes)
        {
            switch (gantryAxes) {
                case GantryAxes.Z:
                    return Axis.ACSC_AXIS_4;
                case GantryAxes.X:
                    return Axis.ACSC_AXIS_0;
                case GantryAxes.Y:
                    return Axis.ACSC_AXIS_1;
                case GantryAxes.All:
                    return Axis.ACSC_NONE;
                case GantryAxes.Invalid:
                    return Axis.ACSC_NONE;
                default:
                    return Axis.ACSC_NONE;
            }
        }

        private Axis GetAcsAxisIndex(ConveyorAxes conveyorAxes)
        {
            switch (conveyorAxes) {
                case ConveyorAxes.Conveyor:
                    return Axis.ACSC_AXIS_5;
                case ConveyorAxes.Width:
                    return Axis.ACSC_AXIS_6;
                case ConveyorAxes.Lifter:
                    return Axis.ACSC_AXIS_7;
                default:
                    return Axis.ACSC_NONE;
            }
        }

        private void InitAxisNumbersAtController()
        {
            acsUtils.WriteVariable(axesCache[GantryAxes.X].AcsAxisId, "X_AXIS", from1: 0, to1: 0);
            acsUtils.WriteVariable(axesCache[GantryAxes.Y].AcsAxisId, "Y_AXIS", from1: 0, to1: 0);
            acsUtils.WriteVariable(axesCache[GantryAxes.Z].AcsAxisId, "Z_AXIS", from1: 0, to1: 0);
            acsUtils.WriteVariable(conveyorAxesCache[ConveyorAxes.Conveyor].AcsAxisId,
                "CONVEYOR_AXIS", from1: 0, to1: 0);
            acsUtils.WriteVariable(conveyorAxesCache[ConveyorAxes.Width].AcsAxisId,
                "CONVEYOR_WIDTH_AXIS", from1: 0, to1: 0);
            acsUtils.WriteVariable(conveyorAxesCache[ConveyorAxes.Lifter].AcsAxisId, "LIFTER_AXIS",
                from1: 0, to1: 0);
        }

        private void InitBuffers()
        {
            try {
                bufferHelper.StopAllBuffers();
                bufferHelper.InitGantryHomingBuffers();
                bufferHelper.InitIoBuffer();

                bufferHelper.InitConveyorBuffers();
                bufferHelper.InitConveyorHomingBuffers();
                bufferHelper.InitConveyorResetBuffers();

                bufferHelper.FlashAllBuffers();

                acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.initIO);
            }
            catch (Exception e) {
                logger.Error("AcsWrapper: initBuffers Exception: " + e.Message);
                throw;
            }
        }

        private bool teminateOldConnections()
        {
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            try {
                ACSC_CONNECTION_DESC[] connectionsList = api.GetConnectionsList();
                for (int index = 0; index < connectionsList.Length; ++index) {
                    if (connectionsList[index].Application.Contains(fileName))
                        api.TerminateConnection(connectionsList[index]);
                }
            }
            catch (Exception ex) {
                return false;
            }

            return true;
        }

        private void axisIdleChanged(int axis, bool isIdle)
        {
            logger.Info(string.Format("axisIdleChanged {0} {1}", (GantryAxes) axis, isIdle),
                3053, nameof(axisIdleChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> idleChanged = IdleChanged;
            if (idleChanged == null)
                return;
            idleChanged((GantryAxes) axis, isIdle);
        }

        private void axisEnabledChanged(int axis, bool isEnabled)
        {
            logger.Info(
                string.Format("axisEnabledChanged {0} {1}", (GantryAxes) axis, isEnabled), 3058,
                nameof(axisEnabledChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> enabledChanged = EnabledChanged;
            if (enabledChanged == null)
                return;
            enabledChanged((GantryAxes) axis, isEnabled);
        }

        private void axisReadyChanged(int axis, bool isReady)
        {
            logger.Info(string.Format("axisReadyChanged {0} {1}", (GantryAxes) axis, isReady),
                3063, nameof(axisReadyChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> readyChanged = ReadyChanged;
            if (readyChanged == null)
                return;
            readyChanged((GantryAxes) axis, isReady);
        }

        private void axisPositionUpdated(int axis, double pos)
        {
            Action<GantryAxes, double> positionUpdated = PositionUpdated;
            if (positionUpdated == null)
                return;
            positionUpdated((GantryAxes) axis, pos);
        }

        private void axisVelocityUpdated(int axis, double vel)
        {
            Action<GantryAxes, double> velocityUpdated = VelocityUpdated;
            if (velocityUpdated == null)
                return;
            velocityUpdated((GantryAxes) axis, vel);
        }

        private void axisMovementBegin(int axis)
        {
            logger.Info(string.Format("Axis_MovementBegin {0} ", (GantryAxes) axis), 3079,
                nameof(axisMovementBegin),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes> movementBegin = MovementBegin;
            if (movementBegin == null)
                return;
            movementBegin((GantryAxes) axis);
        }

        private void axisMovementEnd(int axis, bool res)
        {
            logger.Info(string.Format("axisMovementEnd {0} {1}", (GantryAxes) axis, res), 3084,
                nameof(axisMovementEnd),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> movementEnd = MovementEnd;
            if (movementEnd == null)
                return;
            movementEnd((GantryAxes) axis, res);
        }

        private void axisStopDone(int axis, bool res)
        {
            logger.Info(string.Format("axisStopDone {0} {1}", (GantryAxes) axis, res), 3089,
                nameof(axisStopDone),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> stopDone = StopDone;
            if (stopDone == null)
                return;
            stopDone((GantryAxes) axis, res);
        }

        private void axisAbortDone(int axis, bool res)
        {
            logger.Info(string.Format("axisAbortDone {0} {1}", (GantryAxes) axis, res), 3094,
                nameof(axisAbortDone),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> abortDone = AbortDone;
            if (abortDone == null)
                return;
            abortDone((GantryAxes) axis, res);
        }

        private void axisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            logger.Info(
                string.Format("axisAtHomeSensorChanged {0} {1}", (GantryAxes) axis, isAtHomeSensor),
                3099, nameof(axisAtHomeSensorChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> homeSensorChanged = AtHomeSensorChanged;
            if (homeSensorChanged == null)
                return;
            homeSensorChanged((GantryAxes) axis, isAtHomeSensor);
        }

        private void axisAtPositiveHWLimitChanged(int axis, bool isAtPositiveHWLimit)
        {
            logger.Info(
                string.Format("axisAtPositiveHWLimitChanged {0} {1}", (GantryAxes) axis,
                    isAtPositiveHWLimit), 3104, nameof(axisAtPositiveHWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> positiveHwLimitChanged = AtPositiveHWLimitChanged;
            if (positiveHwLimitChanged == null)
                return;
            positiveHwLimitChanged((GantryAxes) axis, isAtPositiveHWLimit);
        }

        private void axisAtNegativeHWLimitChanged(int axis, bool isAtNegativeHWLimit)
        {
            logger.Info(
                string.Format("axisAtNegativeHWLimitChanged {0} {1}", (GantryAxes) axis,
                    isAtNegativeHWLimit), 3109, nameof(axisAtNegativeHWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> negativeHwLimitChanged = AtNegativeHWLimitChanged;
            if (negativeHwLimitChanged == null)
                return;
            negativeHwLimitChanged((GantryAxes) axis, isAtNegativeHWLimit);
        }

        private void axisAtPositiveSWLimitChanged(int axis, bool isAtPositiveSWLimit)
        {
            logger.Info(
                string.Format("axisAtPositiveSWLimitChanged {0} {1}", (GantryAxes) axis,
                    isAtPositiveSWLimit), 3114, nameof(axisAtPositiveSWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> positiveSwLimitChanged = AtPositiveSWLimitChanged;
            if (positiveSwLimitChanged == null)
                return;
            positiveSwLimitChanged((GantryAxes) axis, isAtPositiveSWLimit);
        }

        private void axisAtNegativeSWLimitChanged(int axis, bool isAtNegativeSWLimit)
        {
            logger.Info(
                string.Format("axisAtNegativeSWLimitChanged {0} {1}", (GantryAxes) axis,
                    isAtNegativeSWLimit), 3119, nameof(axisAtNegativeSWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> negativeSwLimitChanged = AtNegativeSWLimitChanged;
            if (negativeSwLimitChanged == null)
                return;
            negativeSwLimitChanged((GantryAxes) axis, isAtNegativeSWLimit);
        }

        private void Axis_AxisHomingBegin(int axis)
        {
            logger.Info(string.Format("Axis_AxisHomingBegin {0} ", (GantryAxes) axis), 3125,
                nameof(Axis_AxisHomingBegin),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes> axisHomingBegin = AxisHomingBegin;
            if (axisHomingBegin == null)
                return;
            axisHomingBegin((GantryAxes) axis);
        }

        private void Axis_AxisHomingEnd(int axis, bool res)
        {
            logger.Info(string.Format("Axis_AxisHomingEnd {0} {1}", (GantryAxes) axis, res),
                3130, nameof(Axis_AxisHomingEnd),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> axisHomingEnd = AxisHomingEnd;
            if (axisHomingEnd == null)
                return;
            axisHomingEnd((GantryAxes) axis, res);
        }

        private void conveyorAxisIdleChanged(int axis, bool isIdle)
        {
            logger.Info(string.Format("axisIdleChanged {0} {1}", (ConveyorAxes) axis, isIdle));
            Action<ConveyorAxes, bool> conveyorAxisIdleChanged = ConveyorAxisIdleChanged;
            if (conveyorAxisIdleChanged == null)
                return;
            conveyorAxisIdleChanged((ConveyorAxes) axis, isIdle);
        }

        private void conveyorAxisEnabledChanged(int axis, bool isEnabled)
        {
            logger.Info(
                string.Format("axisEnabledChanged {0} {1}", (ConveyorAxes) axis, isEnabled), 3146,
                nameof(conveyorAxisEnabledChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> axisEnabledChanged = ConveyorAxisEnabledChanged;
            if (axisEnabledChanged == null)
                return;
            axisEnabledChanged((ConveyorAxes) axis, isEnabled);
        }

        private void conveyorAxisReadyChanged(int axis, bool isReady)
        {
            logger.Info(string.Format("axisReadyChanged {0} {1}", (ConveyorAxes) axis, isReady),
                3151, nameof(conveyorAxisReadyChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> axisReadyChanged = ConveyorAxisReadyChanged;
            if (axisReadyChanged == null)
                return;
            axisReadyChanged((ConveyorAxes) axis, isReady);
        }

        private void conveyorAxisPositionUpdated(int axis, double pos)
        {
            logger.Info(string.Format("axisPositionUpdated {0} {1}", (ConveyorAxes) axis, pos),
                3156, nameof(conveyorAxisPositionUpdated),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, double> axisPositionUpdated = ConveyorAxisPositionUpdated;
            if (axisPositionUpdated != null)
                axisPositionUpdated((ConveyorAxes) axis, pos);
        }

        private void conveyorAxisVelocityUpdated(int axis, double vel)
        {
            logger.Info(string.Format("axisVelocityUpdated {0} {1}", (ConveyorAxes) axis, vel),
                3166, nameof(conveyorAxisVelocityUpdated),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, double> axisVelocityUpdated = ConveyorAxisVelocityUpdated;
            if (axisVelocityUpdated == null)
                return;
            axisVelocityUpdated((ConveyorAxes) axis, vel);
        }

        private void conveyorAxisMovementBegin(int axis)
        {
            logger.Info(string.Format("Axis_MovementBegin {0} ", (ConveyorAxes) axis), 3171,
                nameof(conveyorAxisMovementBegin),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes> axisMovementBegin = ConveyorAxisMovementBegin;
            if (axisMovementBegin == null)
                return;
            axisMovementBegin((ConveyorAxes) axis);
        }

        private void conveyorAxisMovementEnd(int axis, bool res)
        {
            logger.Info(string.Format("axisMovementEnd {0} {1}", (ConveyorAxes) axis, res));
            Action<ConveyorAxes, bool> conveyorAxisMovementEnd = ConveyorAxisMovementEnd;
            if (conveyorAxisMovementEnd == null)
                return;
            conveyorAxisMovementEnd((ConveyorAxes) axis, res);
        }

        private void conveyorAxisStopDone(int axis, bool res)
        {
            logger.Info(string.Format("axisStopDone {0} {1}", (ConveyorAxes) axis, res));
            Action<ConveyorAxes, bool> conveyorAxisStopDone = ConveyorAxisStopDone;
            if (conveyorAxisStopDone == null)
                return;
            conveyorAxisStopDone((ConveyorAxes) axis, res);
        }

        private void conveyorAxisAbortDone(int axis, bool res)
        {
            logger.Info(string.Format("axisAbortDone {0} {1}", (ConveyorAxes) axis, res));
            Action<ConveyorAxes, bool> conveyorAxisAbortDone = ConveyorAxisAbortDone;
            if (conveyorAxisAbortDone == null)
                return;
            conveyorAxisAbortDone((ConveyorAxes) axis, res);
        }

        private void conveyorAxisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            logger.Info(
                string.Format("axisAtHomeSensorChanged {0} {1}", (ConveyorAxes) axis, isAtHomeSensor),
                3191, nameof(conveyorAxisAtHomeSensorChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> homeSensorChanged = ConveyorAxisAtHomeSensorChanged;
            if (homeSensorChanged == null)
                return;
            homeSensorChanged((ConveyorAxes) axis, isAtHomeSensor);
        }

        private void conveyorAxisAtPositiveHWLimitChanged(int axis, bool isAtPositiveHWLimit)
        {
            logger.Info(
                string.Format("axisAtPositiveHWLimitChanged {0} {1}", (ConveyorAxes) axis,
                    isAtPositiveHWLimit), 3196, nameof(conveyorAxisAtPositiveHWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> positiveHwLimitChanged = ConveyorAxisAtPositiveHwLimitChanged;
            if (positiveHwLimitChanged == null)
                return;
            positiveHwLimitChanged((ConveyorAxes) axis, isAtPositiveHWLimit);
        }

        private void conveyorAxisAtNegativeHWLimitChanged(int axis, bool isAtNegativeHWLimit)
        {
            logger.Info(
                string.Format("axisAtNegativeHWLimitChanged {0} {1}", (ConveyorAxes) axis,
                    isAtNegativeHWLimit), 3201, nameof(conveyorAxisAtNegativeHWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> negativeHwLimitChanged = ConveyorAxisAtNegativeHwLimitChanged;
            if (negativeHwLimitChanged == null)
                return;
            negativeHwLimitChanged((ConveyorAxes) axis, isAtNegativeHWLimit);
        }

        private void conveyorAxisAtPositiveSWLimitChanged(int axis, bool isAtPositiveSWLimit)
        {
            logger.Info(
                string.Format("axisAtPositiveSWLimitChanged {0} {1}", (ConveyorAxes) axis,
                    isAtPositiveSWLimit), 3206, nameof(conveyorAxisAtPositiveSWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> positiveSwLimitChanged = ConveyorAxisAtPositiveSwLimitChanged;
            if (positiveSwLimitChanged == null)
                return;
            positiveSwLimitChanged((ConveyorAxes) axis, isAtPositiveSWLimit);
        }

        private void conveyorAxisAtNegativeSWLimitChanged(int axis, bool isAtNegativeSWLimit)
        {
            logger.Info(
                string.Format("axisAtNegativeSWLimitChanged {0} {1}", (ConveyorAxes) axis,
                    isAtNegativeSWLimit), 3211, nameof(conveyorAxisAtNegativeSWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> negativeSwLimitChanged = ConveyorAxisAtNegativeSwLimitChanged;
            if (negativeSwLimitChanged == null)
                return;
            negativeSwLimitChanged((ConveyorAxes) axis, isAtNegativeSWLimit);
        }

        private void conveyorAxis_AxisHomingBegin(int axis)
        {
            logger.Info(string.Format("Axis_AxisHomingBegin {0} ", (ConveyorAxes) axis), 3217,
                nameof(conveyorAxis_AxisHomingBegin),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes> conveyorAxisHomingBegin = ConveyorAxisHomingBegin;
            if (conveyorAxisHomingBegin == null)
                return;
            conveyorAxisHomingBegin((ConveyorAxes) axis);
        }

        private void conveyorAxis_AxisHomingEnd(int axis, bool res)
        {
            logger.Info(string.Format("Axis_AxisHomingEnd {0} {1}", (ConveyorAxes) axis, res),
                3222, nameof(conveyorAxis_AxisHomingEnd),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> conveyorAxisHomingEnd = ConveyorAxisHomingEnd;
            if (conveyorAxisHomingEnd == null)
                return;
            conveyorAxisHomingEnd((ConveyorAxes) axis, res);
        }

        private int CurrentScanningIndex
        {
            get { return currentScanningIndex; }
            set
            {
                if (value == currentScanningIndex)
                    return;
                currentScanningIndex = value;
                if (currentScanningIndex >= 0) {
                    Action<int> scanningIndexChange = ScanningIndexChange;
                    if (scanningIndexChange != null)
                        scanningIndexChange(currentScanningIndex);
                }
            }
        }

        private int CurrentMotionCompleteReceived
        {
            get { return currentMotionCompleteReceived; }
            set
            {
                if (value == currentMotionCompleteReceived)
                    return;
                currentMotionCompleteReceived = value;
                if (currentMotionCompleteReceived == 1) {
                    Action motionCompleteRecvd = HardwareNotifySingleMoveMotionCompleteReceived;
                    if (motionCompleteRecvd != null)
                        motionCompleteRecvd();
                    acsUtils.WriteVariable(0, "MOVE_MOTION_COMPLETE_RECVD", 9);
                    currentMotionCompleteReceived = 0;
                }
            }
        }

        private int CurrentMovePsxAckReceived
        {
            get { return currentMovePsxAckReceived; }
            set
            {
                if (value == currentMovePsxAckReceived)
                    return;
                currentMovePsxAckReceived = value;
                if (currentMovePsxAckReceived == 1) {
                    Action singleMovePsxAckRecvd = HardwareNotifySingleMovePSXAckReceived;
                    if (singleMovePsxAckRecvd != null)
                        singleMovePsxAckRecvd();
                    acsUtils.WriteVariable(0, "MOVE_PSX_ACK_RECVD", 9);
                    currentMovePsxAckReceived = 0;
                }
            }
        }

        public bool HasError => HasConveyorError || HasRobotError;
        public bool HasConveyorError => ErrorCode != ConveyorErrorCode.ErrorSafe;
        public bool HasRobotError { get; private set; }

        public void ApplicationError()
        {
            // application layer trigger error
            // application layer will call this method to notify ACS controller to handle operation halt accordingly
        }

        public void ResetError()
        {
            // reset controller errors
            // to run buffer 7 for conveyor axes to clear error and enable all gantry axes to clear error.
        }

        public int GetCurrentStatus()
        {
            return new CurrentandErrorStatusfromACS
            {
                CurrentStatus = Convert.ToInt16(acsUtils.ReadVar("CURRENT_STATUS"))
            }.CurrentStatus;
        }

        public int GetErrorCode()
        {
            return new CurrentandErrorStatusfromACS
            {
                ErrorCode = Convert.ToInt16(acsUtils.ReadVar("ERROR_CODE"))
            }.ErrorCode;
        }

        public double GetConveyorWidthAxisPosition()
        {
            var width = Position(ConveyorAxes.Width);
            return width;
        }

        public double GetConveyorLifterAxisPosition()
        {
            var lifter = Position(ConveyorAxes.Lifter);
            return lifter;
        }

        public void SetAdditionalSettlingTime(int settlingTime)
        {
            acsUtils.WriteVariable(settlingTime, "MotionSettlingTimeBeforeScan");
        }

        public void SetBeforeMoveDelay(int beforeMoveDelay)
        {
            acsUtils.WriteVariable(beforeMoveDelay, "BeforeMoveDelay ");
        }


        public void StartPanelLoad(LoadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);
            WriteLifterDistances(parameters.Stage_1_LifterOnlyDistance, parameters.Stage_2_LifterAndClamperDistance);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.LoadPanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.LoadPanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);
            WriteLifterDistances(parameters.Stage_1_LifterOnlyDistance, parameters.Stage_2_LifterAndClamperDistance);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.ReloadPanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReloadPanel, timeout);

            UpdateConveyorStatus();
        }

        private void WritePanelLength(double panelLength)
        {
            acsUtils.WriteVariable(panelLength, "PanelLength");
        }

        private void WriteLifterDistances(double lifterOnlyDistance, double lifterAndClamperDistance)
        {
            acsUtils.WriteVariable(lifterOnlyDistance, "Stage_1_LifterOnlyDistance");
            acsUtils.WriteVariable(lifterAndClamperDistance, "Stage_2_LifterAndClamperDistance");
        }

        public void StopPanelLoad()
        {
            acsUtils.StopBuffer((ProgramBuffer) AcsBuffers.LoadPanel);
        }

        public void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout)
        {
            InitConveyorBufferParameters(parameters);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.PreReleasePanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.PreReleasePanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout)
        {
            InitConveyorBufferParameters(parameters);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.ReleasePanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReleasePanel, timeout);

            UpdateConveyorStatus();
        }

        public IoStatus GetIoStatus()
        {
            return new IoStatus
            {
                EntryOpto = Convert.ToBoolean(acsUtils.ReadVar("EntryOpto_Bit")),
                ExitOpto = Convert.ToBoolean(acsUtils.ReadVar("ExitOpto_Bit")),
                LifterLowered = Convert.ToBoolean(acsUtils.ReadVar("LifterLowered_Bit")),
                BoardStopPanelAlignSensor = Convert.ToBoolean(acsUtils.ReadVar("BoardStopPanelAlignSensor_Bit")),
                StopperArmUp = Convert.ToBoolean(acsUtils.ReadVar("StopperArmUp_Bit")),
                StopperArmDown = Convert.ToBoolean(acsUtils.ReadVar("StopperArmDown_Bit")),
                StopperLocked = Convert.ToBoolean(acsUtils.ReadVar("StopperLocked_Bit")),
                StopperUnlocked = Convert.ToBoolean(acsUtils.ReadVar("StopperUnlocked_Bit")),
                RearClampUp = Convert.ToBoolean(acsUtils.ReadVar("RearClampUp_Bit")),
                FrontClampUp = Convert.ToBoolean(acsUtils.ReadVar("FrontClampUp_Bit")),
                RearClampDown = Convert.ToBoolean(acsUtils.ReadVar("RearClampDown_Bit")),
                FrontClampDown = Convert.ToBoolean(acsUtils.ReadVar("FrontClampDown_Bit")),
                ResetButton = Convert.ToBoolean(acsUtils.ReadVar("Reset_Button_Bit")),
                StartButton = Convert.ToBoolean(acsUtils.ReadVar("Start_Button_Bit")),
                StopButton = Convert.ToBoolean(acsUtils.ReadVar("Stop_Button_Bit")),
                AlarmCancelPushButton = Convert.ToBoolean(acsUtils.ReadVar("AlarmCancelPushButton_Bit")),
                UpstreamBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamBoardAvailableSignal_Bit")),
                UpstreamFailedBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamFailedBoardAvailableSignal_Bit")),
                DownstreamMachineReadySignal = Convert.ToBoolean(acsUtils.ReadVar("DownstreamMachineReadySignal_Bit")),
                BypassNormal = Convert.ToBoolean(acsUtils.ReadVar("BypassNormal_Bit")),
                EstopAndDoorOpenFeedback = Convert.ToBoolean(acsUtils.ReadVar("EstopAndDoorOpenFeedback_Bit")),

                LockStopper = Convert.ToBoolean(acsUtils.ReadVar("LockStopper_Bit")),
                RaiseBoardStopStopper = Convert.ToBoolean(acsUtils.ReadVar("RaiseBoardStopStopper_Bit")),
                ClampPanel = Convert.ToBoolean(acsUtils.ReadVar("ClampPanel_Bit")),
                ResetButtonLight = Convert.ToBoolean(acsUtils.ReadVar("ResetButtonLight_Bit")),
                StartButtonLight = Convert.ToBoolean(acsUtils.ReadVar("StartButtonLight_Bit")),
                StopButtonLight = Convert.ToBoolean(acsUtils.ReadVar("StopButtonLight_Bit")),
                TowerLightRed = Convert.ToBoolean(acsUtils.ReadVar("TowerLightRed_Bit")),
                TowerLightYellow = Convert.ToBoolean(acsUtils.ReadVar("TowerLightYellow_Bit")),
                TowerLightGreen = Convert.ToBoolean(acsUtils.ReadVar("TowerLightGreen_Bit")),
                TowerLightBlue = Convert.ToBoolean(acsUtils.ReadVar("TowerLightBlue_Bit")),
                TowerLightBuzzer = Convert.ToBoolean(acsUtils.ReadVar("TowerLightBuzzer_Bit")),
                SensorPower = Convert.ToBoolean(acsUtils.ReadVar("SensorPowerOnOff_Bit")),
                BeltShroudVaccumOn = Convert.ToBoolean(acsUtils.ReadVar("BeltShroudVaccumON_Bit")),
                StopSensor = Convert.ToBoolean(acsUtils.ReadVar("StopSensor_Bit")),
                SmemaUpStreamMachineReady = Convert.ToBoolean(acsUtils.ReadVar("SmemaUpStreamMachineReady_Bit")),
                DownStreamBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("DownStreamBoardAvailable_Bit")),
                SmemaDownStreamFailedBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("SmemaDownStreamFailedBoardAvailable_Bit")),
            };
        }

        public void SetOutputs(SetOutputParameters outputs)
        {
            acsUtils.WriteVariable(Convert.ToInt32(outputs.LockStopper), "LockStopper_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.RaiseBoardStopStopper), "RaiseBoardStopStopper_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.ClampPanel), "ClampPanel_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.ResetButtonLight), "ResetButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StartButtonLight), "StartButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StopButtonLight), "StopButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightRed), "TowerLightRed_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightYellow), "TowerLightYellow_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightGreen), "TowerLightGreen_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightBlue), "TowerLightBlue_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightBuzzer), "TowerLightBuzzer_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StopSensor), "StopSensor_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.SmemaUpStreamMachineReady), "SmemaUpStreamMachineReady_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.DownStreamBoardAvailable), "DownStreamBoardAvailable_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.SmemaDownStreamFailedBoardAvailable),
                "SmemaDownStreamFailedBoardAvailable_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.BeltShroudVacuumOn), "BeltShroudVaccumON_Bit");
        }

        public void ChangeConveyorWidth(ChangeWidthBufferParameters parameters, int timeout)
        {
            // change conveyor width
            InitConveyorBufferParameters(parameters);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.ChangeWidth);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.ChangeWidth, timeout);

            UpdateConveyorStatus();
        }

        private void UpdateConveyorStatus()
        {
            ConveyorStatus = (ConveyorStatusCode) Convert.ToInt16(acsUtils.ReadVar("CURRENT_STATUS"));
            ErrorCode = (ConveyorErrorCode) Convert.ToInt16(acsUtils.ReadVar("ERROR_CODE"));
        }

        public void PowerOnRecoverFromEmergencyStop(PowerOnRecoverFromEmergencyStopBufferParameters parameter,
            int timeout)
        {
            try {
                InitConveyorBufferParameters(parameter);

                acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.PowerOnRecoverFromEmergencyStop);
                api.WaitProgramEnd((ProgramBuffer) AcsBuffers.PowerOnRecoverFromEmergencyStop, timeout);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        public PanelButtons GetPanelButtonsStatus()
        {
            return new PanelButtons
            {
                StartButton = Convert.ToBoolean(acsUtils.ReadVar("Start_Button_Bit")),
                StopButton = Convert.ToBoolean(acsUtils.ReadVar("Stop_Button_Bit")),
                ResetButton = Convert.ToBoolean(acsUtils.ReadVar("Reset_Button_Bit")),
                EStopButton = Convert.ToBoolean(acsUtils.ReadVar("EstopAndDoorOpenFeedback_Bit")),
            };
        }

        public ClampSensors GetClampSensorsStatus()
        {
            return new ClampSensors
            {
                FrontClampUp = Convert.ToBoolean(acsUtils.ReadVar("StopperUnlocked_Bit")),
                RearClampUp = Convert.ToBoolean(acsUtils.ReadVar("FrontClampUp_Bit")),
                FrontClampDown = Convert.ToBoolean(acsUtils.ReadVar("RearClampDown_Bit")),
                RearClampDown = Convert.ToBoolean(acsUtils.ReadVar("FrontClampDown_Bit"))
            };
        }

        public PresentSensors GetPresentSensorsStatus()
        {
            return new PresentSensors
            {
                EntryOpto = Convert.ToBoolean(acsUtils.ReadVar("EntryOpto_Bit")),
                ExitOpto = Convert.ToBoolean(acsUtils.ReadVar("ExitOpto_Bit")),
                BoardStopPanelAlignSensor = Convert.ToBoolean(acsUtils.ReadVar("BoardStopPanelAlignSensor_Bit"))
            };
        }

        public SmemaIo GetSmemaIoStatus()
        {
            return new SmemaIo
            {
                UpstreamBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamBoardAvailableSignal_Bit")),
                UpstreamFailedBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamFailedBoardAvailableSignal_Bit")),
                DownstreamMachineReadySignal = Convert.ToBoolean(acsUtils.ReadVar("DownstreamMachineReadySignal_Bit")),

                SmemaUpStreamMachineReady = Convert.ToBoolean(acsUtils.ReadVar("SmemaUpStreamMachineReady_Bit")),
                DownStreamBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("DownStreamBoardAvailable_Bit")),
                SmemaDownStreamFailedBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("SmemaDownStreamFailedBoardAvailable_Bit")),
            };
        }

        public bool IsBypassSignalSet()
        {
            return Convert.ToBoolean(acsUtils.ReadVar("BypassNormal_Bit"));
        }

        public void BypassModeOn(BypassModeBufferParameters parameter)
        {
            // activate Bypass mode
            try {
                InitConveyorBufferParameters(parameter);
                acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.BypassMode);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        public void BypassModeOff()
        {
            // deactivate Bypass mode
            try {
                acsUtils.StopBuffer((ProgramBuffer) AcsBuffers.BypassMode);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        private void SetIndicatorState(AcsIndicatorState state, string flashingVariable, string outputVariable)
        {
            switch (state) {
                case AcsIndicatorState.Flashing:
                    acsUtils.WriteVariable(1, flashingVariable);
                    break;
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, flashingVariable);
                    acsUtils.WriteVariable(0, outputVariable);
                    break;
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, outputVariable);
                    break;
            }
        }

        public void SetTowerLightRed(AcsIndicatorState state)
        {
            logger.Info($"SetTowerLightRed flash state {state}");
            SetIndicatorState(state, "TowerLightRedFlashing_Bit", "TowerLightRed_Bit");
        }

        public void SetTowerLightYellow(AcsIndicatorState state)
        {
            logger.Info($"SetTowerLightYellow flash state {state}");
            SetIndicatorState(state, "TowerLightYellowFlashing_Bit", "TowerLightYellow_Bit");
        }

        public void SetTowerLightGreen(AcsIndicatorState state)
        {
            logger.Info($"SetTowerLightGreen flash state {state}");
            SetIndicatorState(state, "TowerLightGreenFlashing_Bit", "TowerLightGreen_Bit");
        }

        public void SetTowerLightBlue(AcsIndicatorState state)
        {
            logger.Info($"SetTowerLightBlue flash state {state}");
            SetIndicatorState(state, "TowerLightBlueFlashing_Bit", "TowerLightBlue_Bit");
        }

        public void SetTowerLightBuzzer(AcsIndicatorState state)
        {
            logger.Info($"SetTowerLightBuzzer flash state {state}");
            switch (state) {
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, "TowerLightBuzzer_Bit");
                    break;
                case AcsIndicatorState.Flashing:
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, "TowerLightBuzzer_Bit");
                    break;
            }
        }

        public void SetStartButtonIndicator(AcsIndicatorState state)
        {
            logger.Info($"SetStartButtonIndicator state {state}");
            switch (state) {
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, "StartButtonLight_Bit");
                    break;
                case AcsIndicatorState.Flashing:
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, "StartButtonLight_Bit");
                    break;
            }
        }

        public void SetStopButtonIndicator(AcsIndicatorState state)
        {
            logger.Info($"SetStopButtonIndicator state {state}");
            switch (state) {
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, "StopButtonLight_Bit");
                    break;
                case AcsIndicatorState.Flashing:
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, "StopButtonLight_Bit");
                    break;
            }
        }

        public void SetMachineReady()
        {
            acsUtils.WriteVariable(1, "SmemaUpStreamMachineReady_Bit");
        }

        public void ResetMachineReady()
        {
            acsUtils.WriteVariable(0, "SmemaUpStreamMachineReady_Bit");
        }

        public bool IsConveyorAxisEnable()
        {
            return Enabled(ConveyorAxes.Conveyor);
        }

        public bool IsConveyorWidthAxisEnable()
        {
            return Enabled(ConveyorAxes.Width);
        }

        public void HomeConveyorWidthAxis(HomeConveyorWidthParameters parameter)
        {
            try {
                InitConveyorBufferParameters(parameter);
                acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.WidthHoming);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        public void EnableConveyorAxis()
        {
            Enable(ConveyorAxes.Conveyor);
        }

        public void DisableConveyorAxis()
        {
            Disable(ConveyorAxes.Conveyor);
        }

        public void EnableConveyorWidthAxis()
        {
            Enable(ConveyorAxes.Width);
        }

        public void DisableConveyorWidthAxis()
        {
            Disable(ConveyorAxes.Width);
        }

        public void JogConveyorAxisLeftToRight(double velocity, double acceleration, double deceleration)
        {
            Jog(ConveyorAxes.Conveyor, velocity, acceleration, deceleration);
        }

        public void JogConveyorAxisRightToLeft(double velocity, double acceleration, double deceleration)
        {
            Jog(ConveyorAxes.Conveyor, velocity * -1, acceleration, deceleration);
        }

        public void StopConveyorAxis()
        {
            Stop(ConveyorAxes.Conveyor);
        }

        public void EnableConveyorLifterAxis()
        {
            Enable(ConveyorAxes.Lifter);
        }

        public void DisableConveyorLifterAxis()
        {
            Disable(ConveyorAxes.Lifter);
        }

        public void HomeConveyorLifterAxis()
        {
            try {
                acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.LifterHoming);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        public void MoveConveyorLifter(double targetPosition)
        {
            MoveAbsolute(ConveyorAxes.Lifter, targetPosition, 10, 10, 10);
        }

        public bool IsConveyorLifterAxisEnabled()
        {
            return Enabled(ConveyorAxes.Lifter);
        }
    }
}