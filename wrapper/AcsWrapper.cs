
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
using CO.Systems.Services.Acs.AcsWrapper.wrapper.status;
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
        private readonly MachineCalibrationSetting machineCalSettings;
        private readonly ILogger logger;

        private readonly AutoResetEvent waitExitFromPoling = new AutoResetEvent(false);

        private bool scanLoopRunning;
        private const int SleepInterval = 1;
        private const int DataRefreshInterval = 100;
        private const int DataRefreshCounter = DataRefreshInterval / SleepInterval;

        private readonly Dictionary<GantryAxes, AcsAxis> axesCache = new Dictionary<GantryAxes, AcsAxis>();
        private readonly Dictionary<ConveyorAxes, AcsAxis> conveyorAxesCache = new Dictionary<ConveyorAxes, AcsAxis>();
        private readonly object lockObject = new object();

        private bool isScanningBufferRun;
        private bool isConveyorBufferRun;
        private bool isConnected;
        private int currentScanningIndex = -1;
        private int currentMotionCompleteReceived;
        private int currentMovePsxAckReceived;

        private Thread acsPollingThread;

        internal AcsWrapper(ILogger logger, IRobotControlSetting robotSettings,
            MachineCalibrationSetting machineCalSettings)
        {
            IsSimulation = AcsSimHelper.IsEnable();
            this.robotSettings = robotSettings;
            this.machineCalSettings = machineCalSettings;
            this.logger = logger;

            api = new Api();
            acsUtils = new AcsUtils(api, logger);
            bufferHelper = new BufferHelper(api, acsUtils, logger, IsSimulation);
        }

        public bool IsSimulation { get; }

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

        public GantryStatusCode GantryStatus { get; private set; }

        public GantryErrorCode GantryErrorCode { get; private set; }

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

        public event Action<ConveyorAxes, bool> OnConveyorAxisIdleChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisEnabledChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisReadyChanged;

        public event Action<ConveyorAxes, double> OnConveyorAxisPositionUpdated;

        public event Action<ConveyorAxes, double> OnConveyorAxisVelocityUpdated;

        public event Action<ConveyorAxes, bool> OnConveyorAxisStopDone;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAbortDone;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAtHomeSensorChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAtPositiveHwLimitChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAtNegativeHwLimitChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAtPositiveSwLimitChanged;

        public event Action<ConveyorAxes, bool> OnConveyorAxisAtNegativeSwLimitChanged;

        public event Action<ConveyorAxes> OnConveyorAxisMovementBegin;

        public event Action<ConveyorAxes, bool> OnConveyorAxisMovementEnd;

        public event Action<ConveyorAxes> OnConveyorAxisHomingBegin;

        public event Action<ConveyorAxes, bool> OnConveyorAxisHomingEnd;

        public void Connect()
        {
            string ip = IsSimulation ? "localhost" : "10.0.0.100";
            logger.Info($"AcsWrapper: Connect. IP {ip}");

            lock (lockObject) {
                TerminateOldConnections();
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
                StartPolling();
            }
        }

        private void StartPolling()
        {
            logger.Debug($"AcsWrapper: StartPolling");
            if (acsPollingThread == null)
            {
                logger.Debug($"AcsWrapper: StartPolling; _acsPollingThread is null so spawn thread to run ScanLoop");
                acsPollingThread = new Thread(ScanLoop);
                acsPollingThread.IsBackground = true;
                acsPollingThread.Start();
            }
            else
            {
                logger.Debug($"AcsWrapper: StartPolling; _acsPollingThread is running....");
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
                acsPollingThread = null;
                return !IsConnected;
            }
        }

        public void Disengage() {
            if (!IsConnected) return;
            
            Disable(GantryAxes.X);
            Disable(GantryAxes.Y);
            Disable(GantryAxes.Z);

            Disable(ConveyorAxes.Conveyor);
            Disable(ConveyorAxes.Lifter);
            Disable(ConveyorAxes.Width);

            Disconnect();
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

        public double GetGantryPosition(GantryAxes axis)
        {
            logger.Info(string.Format("GetGantryPosition(axis = {0})", axis));

            return axesCache[axis].GetPosition();
        }

        public double GetConveyorPosition(ConveyorAxes axis)
        {
            logger.Info(string.Format("GetConveyorPosition(axis = {0})", axis));

            return conveyorAxesCache[axis].GetPosition();
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

        public bool PrepareScanning(List<IPvTuple3D> motionPaths, int triggerToCameraStartPort,
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

            bufferHelper.PrepareScanningBuffer(motionPaths, triggerToCameraStartPort, triggerToCameraStartBit,
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
            bufferHelper.RunBuffer(ProgramBuffer.ACSC_BUFFER_9);
            isScanningBufferRun = bufferHelper.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9);

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

            bufferHelper.RunBuffer((ProgramBuffer) buffer);
            isConveyorBufferRun = bufferHelper.IsProgramRunning((ProgramBuffer) buffer);
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

            acsUtils.WriteVariable(Convert.ToInt32(parameters.AutoWidthEnable), "AutoWidthEnable", acsUtils.GetDBufferIndex());
            acsUtils.WriteGlobalReal(parameters.HomeInVelocity, "HOME_VEL_IN", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HomeOutVelocity, "HOME_VEL_OUT", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HomeOffset, "HOME_OFFSET", (int)ConveyorAxes.Width);

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
            acsUtils.WriteVariable(Convert.ToInt32(parameters.ConveyorSimultaneousLoadUnload), "ConveyorSimultaneousLoadUnload", buffer);
            
            acsUtils.WriteVariable(Convert.ToInt32(parameters.OperationMode), "OperationMode", buffer);
            acsUtils.WriteVariable(parameters.SmemaFailedBoardMode, "SmemaFailedBoardMode", buffer);

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
                catch (Exception) {
                    return false;
                }

                return Init(initParameters1);
            }

            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<GantryAxes, AcsAxis> keyValuePair in axesCache) {
                KeyValuePair<GantryAxes, AcsAxis> item = keyValuePair;
                AxisInitParameters axisInitParams;
                try {
                    axisInitParams =
                        initParameters.Find(item2 => item2.Axis == item.Key);
                }
                catch (Exception) {
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
            UpdateRobotStatus();
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

            if (axesCache.ContainsKey(axis)) {
                var  moveResult = axesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
                UpdateRobotStatus();
                return  moveResult;
            }

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
            UpdateRobotStatus();
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

            if (axesCache.ContainsKey(axis)) {
                var moveResult = axesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
                UpdateRobotStatus();
                return moveResult;
            }

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

            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
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
            logger.Info(string.Format("Abort(axis={0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Abort();
            throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(GantryAxes axis, double pos)
        {
            logger.Info(string.Format("SetRPos(axis={0},pos={1})", axis, pos));
            if (!IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                if (axesCache.ContainsKey(axis))
                    axesCache[axis].SetRPos(pos);
                throw new ArgumentException("Axis not exist ");
            }
        }

        public void ResetConveyorAxes()
        {
            logger.Info("Reset()");
            if (!IsConnected) {
                logger.Info("Controller not connected");
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
            logger.Info(string.Format("ClearError(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
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
            logger.Info(string.Format("Disable(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Disable();
            throw new ArgumentException("Axis not exist ");
        }

        public bool ReloadConfigParameters(ConveyorAxes axis)
        {
            logger.Info(string.Format("ReloadConfigParameters(axis = {0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (!conveyorAxesCache.ContainsKey(axis))
                throw new ArgumentException("Axis not exist ");
            conveyorAxesCache[axis].ReloadConfigParameters();
            return true;
        }

        public bool InitConveyorAxes()
        {
            logger.Info("Init()");
            if (!IsConnected) {
                logger.Info("Controller not connected");
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
            logger.Info("MoveAbsolute(List<ConveyorAxesMoveParameters> axesToMove)");
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

            var taskList = new List<Task>();
            foreach (ConveyorAxesMoveParameters axesMoveParameters in axesToMove) {
                ConveyorAxesMoveParameters axisToMove = axesMoveParameters;
                taskList.Add(Task.Run((Action) (() => MoveAbsolute(axisToMove.Axis, axisToMove.TargetPos,
                    axisToMove.Velocity, axisToMove.Accel, axisToMove.Decel))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool MoveAbsolute(ConveyorAxes axis, double targetPos, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            logger.Info(string.Format("MoveAbsolute(axis = {0},targetPos= {1}, vel= {2}, acc= {3}, dec= {4})", axis,
                targetPos, vel, acc, dec));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveRelative(List<ConveyorAxesMoveParameters> axesToMove)
        {
            logger.Info("MoveRelative(List<AxisMoveParameters> axesToMove)");
            if (!IsConnected) {
                logger.Info("Controller not connected");
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

        public bool MoveRelative(ConveyorAxes axis, double relativePosition, double vel = 0.0, double acc = 0.0,
            double dec = 0.0)
        {
            logger.Info(string.Format("MoveRelative(axis = {0},relativePosition= {1}, vel= {2}, acc= {3}, dec= {4})",
                axis, relativePosition, vel, acc, dec));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Jog(ConveyorAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            logger.Info(string.Format("Jog(axis = {0}, vel= {1}, acc= {2}, dec= {3})", axis, vel, acc, dec));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Jog(false, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool StopAllConveyorAxes()
        {
            logger.Info("StopAll()");
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            for (AcsBuffers acsBuffers = AcsBuffers.ConveyorHoming;
                acsBuffers <= AcsBuffers.InternalErrorExit;
                ++acsBuffers) {
                bufferHelper.StopBuffer((ProgramBuffer) acsBuffers);
            }

            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_55);
            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_56);
            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_57);
            foreach (KeyValuePair<ConveyorAxes, AcsAxis> keyValuePair in conveyorAxesCache)
                keyValuePair.Value.Stop();
            return true;
        }

        public bool Stop(ConveyorAxes axis)
        {
            logger.Info(string.Format("Stop(axis={0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Stop();
            throw new ArgumentException("Axis not exist ");
        }

        public bool Abort(ConveyorAxes axis)
        {
            logger.Info(string.Format("Abort(axis={0})", axis));
            if (!IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            bufferHelper.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
            if (conveyorAxesCache.ContainsKey(axis))
                return conveyorAxesCache[axis].Abort();
            throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(ConveyorAxes axis, double pos)
        {
            logger.Info(string.Format("SetRPos(axis={0},pos={1})", axis, pos));
            if (!IsConnected) {
                logger.Info("Controller not connected");
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
            var refreshCounter = DataRefreshCounter;
            logger.Debug($"+AcsWrapper:ScanLoop starts running....");
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
                    CurrentMotionCompleteReceived =
                        Convert.ToInt32(
                            acsUtils.ReadVar("MOVE_MOTION_COMPLETE_RECVD", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentMovePsxAckReceived =
                        Convert.ToInt32(acsUtils.ReadVar("MOVE_PSX_ACK_RECVD", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentScanningIndex =
                        Convert.ToInt32(acsUtils.ReadVar("CURRENT_STEP_INDEX", ProgramBuffer.ACSC_BUFFER_9));

                    if (!bufferHelper.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9)) {
                        isScanningBufferRun = false;
                        ScanningEnd?.Invoke();
                        foreach (var keyValuePair in axesCache) {
                            keyValuePair.Value.ScanningBufferRun = false;
                        }
                    }
                }

                if (--refreshCounter < 0) {
                    foreach (var keyValuePair in axesCache) {
                        keyValuePair.Value.GetDataFromController();
                    }
                    foreach (var keyValuePair in conveyorAxesCache) {
                        keyValuePair.Value.GetDataFromController();
                    }
                    refreshCounter = DataRefreshCounter;
                }

                Thread.Sleep(SleepInterval);
            }
            waitExitFromPoling.Set();
            logger.Debug($"-AcsWrapper:ScanLoop exits");
        }

        private void EnableAcsEvents()
        {
            api.EnableEvent(Interrupts.ACSC_INTR_EMERGENCY);
            api.EnableEvent(Interrupts.ACSC_INTR_ETHERCAT_ERROR);
            api.EnableEvent(Interrupts.ACSC_INTR_MESSAGE);
            api.EnableEvent(Interrupts.ACSC_INTR_MOTION_FAILURE);
            api.EnableEvent(Interrupts.ACSC_INTR_MOTOR_FAILURE);
            api.EnableEvent(Interrupts.ACSC_INTR_SYSTEM_ERROR);
            api.EnableEvent(Interrupts.ACSC_INTR_COMMAND);

            api.EMERGENCY += ApiEmergency;
            api.SYSTEMERROR += ApiSystemError;
            api.MOTORFAILURE += ApiMotorFailure;
            api.MOTIONFAILURE += ApiMotionFailure;
            api.ETHERCATERROR += ApiEtherCatError;
            api.MESSAGE += ApiMessage;
            api.ACSPLPROGRAMEX += ApiAcsplProgramEx;
        }

        private void ApiMessage(ulong param)
        {
            logger.Info($"AcsWrapper.ApiMessage: {param}");
        }

        private void ApiEtherCatError(ulong param)
        {
            logger.Info($"AcsWrapper.ApiEtherCatError: {param}");
        }

        private void ApiMotionFailure(AxisMasks param)
        {
            logger.Info($"AcsWrapper.ApiMotionFailure: {param}");
        }

        private void ApiMotorFailure(AxisMasks param)
        {
            logger.Info($"AcsWrapper.ApiMotorFailure: {param}");
        }

        private void ApiSystemError(ulong param)
        {
            try {
                var lastError = api.GetLastError();
                logger.Info($"AcsWrapper.ApiSystemError: {param}, {lastError}");

                if (lastError > 100) {
                    logger.Info($"AcsWrapper.ApiSystemError: {param}, {api.GetErrorString(lastError)}");
                }
            }
            catch (ACSException ex) {
                logger.Error($"AcsWrapper.ApiSystemError: {ex.ErrorCode}: " + ex.Message);
            }
        }

        private void ApiEmergency(ulong param)
        {
            logger.Info($"AcsWrapper.ApiEmergency: {param}");
        }

        private void ApiAcsplProgramEx(ulong param)
        {
            logger.Info($"AcsWrapper.ApiAcsplProgramEx: {param}");
        }

        private void InitAxesCache()
        {
            axesCache.Clear();
            for (var gantryAxes = GantryAxes.Z; gantryAxes < GantryAxes.All; ++gantryAxes) {
                var acsAxis = new AcsAxis(api, acsUtils, bufferHelper, gantryAxes, GetAcsAxisIndex(gantryAxes), robotSettings);

                axesCache[gantryAxes] = acsAxis;
                acsAxis.IdleChanged += AxisIdleChanged;
                acsAxis.EnabledChanged += AxisEnabledChanged;
                acsAxis.ReadyChanged += AxisReadyChanged;
                acsAxis.PositionUpdated += AxisPositionUpdated;
                acsAxis.VelocityUpdated += AxisVelocityUpdated;
                acsAxis.MovementBegin += AxisMovementBegin;
                acsAxis.MovementEnd += AxisMovementEnd;
                acsAxis.StopDone += AxisStopDone;
                acsAxis.AbortDone += AxisAbortDone;
                acsAxis.AtHomeSensorChanged += AxisAtHomeSensorChanged;
                acsAxis.AtPositiveHwLimitChanged += AxisAtPositiveHwLimitChanged;
                acsAxis.AtNegativeHwLimitChanged += AxisAtNegativeHwLimitChanged;
                acsAxis.AtPositiveSwLimitChanged += AxisAtPositiveSwLimitChanged;
                acsAxis.AtNegativeSwLimitChanged += AxisAtNegativeSwLimitChanged;
                acsAxis.AxisHomingBegin += AxisHomingBegin;
                acsAxis.AxisHomingEnd += AxisHomingEnd;

                if (acsAxis.AcsAxisId >= Axis.ACSC_AXIS_0) {
                    api.Halt(acsAxis.AcsAxisId);
                }
            }
        }

        private void InitConveyorAxesCache()
        {
            conveyorAxesCache.Clear();
            for (var conveyorAxes = ConveyorAxes.Conveyor; conveyorAxes <= ConveyorAxes.Lifter; ++conveyorAxes) {
                var acsAxis = new AcsAxis(api, acsUtils, bufferHelper, conveyorAxes, GetAcsAxisIndex(conveyorAxes));

                conveyorAxesCache[conveyorAxes] = acsAxis;
                acsAxis.IdleChanged += ConveyorAxisIdleChanged;
                acsAxis.EnabledChanged += ConveyorAxisEnabledChanged;
                acsAxis.ReadyChanged += ConveyorAxisReadyChanged;
                acsAxis.PositionUpdated += ConveyorAxisPositionUpdated;
                acsAxis.VelocityUpdated += ConveyorAxisVelocityUpdated;
                acsAxis.MovementBegin += ConveyorAxisMovementBegin;
                acsAxis.MovementEnd += ConveyorAxisMovementEnd;
                acsAxis.StopDone += ConveyorAxisStopDone;
                acsAxis.AbortDone += ConveyorAxisAbortDone;
                acsAxis.AtHomeSensorChanged += ConveyorAxisAtHomeSensorChanged;
                acsAxis.AtPositiveHwLimitChanged += ConveyorAxisAtPositiveHwLimitChanged;
                acsAxis.AtNegativeHwLimitChanged += ConveyorAxisAtNegativeHwLimitChanged;
                acsAxis.AtPositiveSwLimitChanged += ConveyorAxisAtPositiveSwLimitChanged;
                acsAxis.AtNegativeSwLimitChanged += ConveyorAxisAtNegativeSwLimitChanged;
                acsAxis.AxisHomingBegin += ConveyorAxisHomingBegin;
                acsAxis.AxisHomingEnd += ConveyorAxisHomingEnd;

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

                InitVariables();

                bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.InitIo);
            }
            catch (Exception e) {
                logger.Error("AcsWrapper: initBuffers Exception: " + e.Message);
                throw;
            }
        }

        private void InitVariables()
        {
            var conveyorInSimulationMode = machineCalSettings.ConveyorType.Value == (int)ConveyorType.Simulated ? 1 : 0;
            acsUtils.WriteVariable(conveyorInSimulationMode, "ConveyorInSimulationMode");
        }

        private bool TerminateOldConnections()
        {
            try {
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule == null) {
                    return false;
                }

                string fileName = Path.GetFileName(processModule.FileName);
                var connectionsList = api.GetConnectionsList();
                for (int index = 0; index < connectionsList.Length; ++index) {
                    if (connectionsList[index].Application.Contains(fileName))
                        api.TerminateConnection(connectionsList[index]);
                }
            }
            catch (Exception) {
                return false;
            }

            return true;
        }

        private void AxisIdleChanged(int axis, bool isIdle)
        {
            logger.Info(string.Format("axisIdleChanged {0} {1}", (GantryAxes) axis, isIdle));
            IdleChanged?.Invoke((GantryAxes) axis, isIdle);
        }

        private void AxisEnabledChanged(int axis, bool isEnabled)
        {
            logger.Info(string.Format("axisEnabledChanged {0} {1}", (GantryAxes) axis, isEnabled));
            EnabledChanged?.Invoke((GantryAxes) axis, isEnabled);
        }

        private void AxisReadyChanged(int axis, bool isReady)
        {
            logger.Info(string.Format("axisReadyChanged {0} {1}", (GantryAxes) axis, isReady));
            ReadyChanged?.Invoke((GantryAxes) axis, isReady);
        }

        private void AxisPositionUpdated(int axis, double pos)
        {
            PositionUpdated?.Invoke((GantryAxes) axis, pos);
        }

        private void AxisVelocityUpdated(int axis, double vel)
        {
            VelocityUpdated?.Invoke((GantryAxes) axis, vel);
        }

        private void AxisMovementBegin(int axis)
        {
            logger.Info(string.Format("Axis_MovementBegin {0} ", (GantryAxes) axis));
            MovementBegin?.Invoke((GantryAxes) axis);
        }

        private void AxisMovementEnd(int axis, bool res)
        {
            logger.Info(string.Format("axisMovementEnd {0} {1}", (GantryAxes) axis, res));
            MovementEnd?.Invoke((GantryAxes) axis, res);
        }

        private void AxisStopDone(int axis, bool res)
        {
            logger.Info(string.Format("axisStopDone {0} {1}", (GantryAxes) axis, res));
            StopDone?.Invoke((GantryAxes) axis, res);
        }

        private void AxisAbortDone(int axis, bool res)
        {
            logger.Info(string.Format("axisAbortDone {0} {1}", (GantryAxes) axis, res));
            AbortDone?.Invoke((GantryAxes) axis, res);
        }

        private void AxisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            logger.Info(
                string.Format("axisAtHomeSensorChanged {0} {1}", (GantryAxes) axis, isAtHomeSensor));
            AtHomeSensorChanged?.Invoke((GantryAxes) axis, isAtHomeSensor);
        }

        private void AxisAtPositiveHwLimitChanged(int axis, bool isAtPositiveHwLimit)
        {
            var gantryAxes = (GantryAxes) axis;
            logger.Info(string.Format("axisAtPositiveHWLimitChanged {0} {1}", gantryAxes, isAtPositiveHwLimit));
            AtPositiveHwLimitChanged?.Invoke(gantryAxes, isAtPositiveHwLimit);
        }

        private void AxisAtNegativeHwLimitChanged(int axis, bool isAtNegativeHwLimit)
        {
            var gantryAxes = (GantryAxes) axis;
            logger.Info(string.Format("axisAtNegativeHWLimitChanged {0} {1}", gantryAxes, isAtNegativeHwLimit));
            AtNegativeHwLimitChanged?.Invoke(gantryAxes, isAtNegativeHwLimit);
        }

        private void AxisAtPositiveSwLimitChanged(int axis, bool isAtPositiveSwLimit)
        {
            var gantryAxes = (GantryAxes) axis;
            logger.Info(string.Format("axisAtPositiveSWLimitChanged {0} {1}", gantryAxes, isAtPositiveSwLimit));
            AtPositiveSwLimitChanged?.Invoke(gantryAxes, isAtPositiveSwLimit);
        }

        private void AxisAtNegativeSwLimitChanged(int axis, bool isAtNegativeSwLimit)
        {
            var gantryAxes = (GantryAxes) axis;
            logger.Info(string.Format("axisAtNegativeSWLimitChanged {0} {1}", gantryAxes, isAtNegativeSwLimit));
            AtNegativeSwLimitChanged?.Invoke(gantryAxes, isAtNegativeSwLimit);
        }

        private void AxisHomingBegin(int axis)
        {
            logger.Info(string.Format("AxisHomingBegin {0} ", (GantryAxes) axis));
            OnAxisHomingBegin?.Invoke((GantryAxes) axis);
        }

        private void AxisHomingEnd(int axis, bool res)
        {
            logger.Info(string.Format("AxisHomingEnd {0} {1}", (GantryAxes) axis, res));
            OnAxisHomingEnd?.Invoke((GantryAxes) axis, res);
        }

        private void ConveyorAxisIdleChanged(int axis, bool isIdle)
        {
            logger.Info(string.Format("axisIdleChanged {0} {1}", (ConveyorAxes) axis, isIdle));
            OnConveyorAxisIdleChanged?.Invoke((ConveyorAxes) axis, isIdle);
        }

        private void ConveyorAxisEnabledChanged(int axis, bool isEnabled)
        {
            logger.Info(string.Format("axisEnabledChanged {0} {1}", (ConveyorAxes) axis, isEnabled));
            OnConveyorAxisEnabledChanged?.Invoke((ConveyorAxes) axis, isEnabled);
        }

        private void ConveyorAxisReadyChanged(int axis, bool isReady)
        {
            OnConveyorAxisReadyChanged?.Invoke((ConveyorAxes) axis, isReady);
        }

        private void ConveyorAxisPositionUpdated(int axis, double pos)
        {
            OnConveyorAxisPositionUpdated?.Invoke((ConveyorAxes) axis, pos);
        }

        private void ConveyorAxisVelocityUpdated(int axis, double vel)
        {
            Action<ConveyorAxes, double> axisVelocityUpdated = OnConveyorAxisVelocityUpdated;
            OnConveyorAxisVelocityUpdated?.Invoke((ConveyorAxes) axis, vel);
        }

        private void ConveyorAxisMovementBegin(int axis)
        {
            OnConveyorAxisMovementBegin?.Invoke((ConveyorAxes) axis);
        }

        private void ConveyorAxisMovementEnd(int axis, bool res)
        {
            OnConveyorAxisMovementEnd?.Invoke((ConveyorAxes) axis, res);
        }

        private void ConveyorAxisStopDone(int axis, bool res)
        {
            OnConveyorAxisStopDone?.Invoke((ConveyorAxes) axis, res);
        }

        private void ConveyorAxisAbortDone(int axis, bool res)
        {
            OnConveyorAxisAbortDone?.Invoke((ConveyorAxes) axis, res);
        }

        private void ConveyorAxisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            OnConveyorAxisAtHomeSensorChanged?.Invoke((ConveyorAxes) axis, isAtHomeSensor);
        }

        private void ConveyorAxisAtPositiveHwLimitChanged(int axis, bool isAtPositiveHwLimit)
        {
            logger.Info(string.Format("axisAtPositiveHWLimitChanged {0} {1}", (ConveyorAxes) axis, isAtPositiveHwLimit));
            OnConveyorAxisAtPositiveHwLimitChanged?.Invoke((ConveyorAxes) axis, isAtPositiveHwLimit);
        }

        private void ConveyorAxisAtNegativeHwLimitChanged(int axis, bool isAtNegativeHwLimit)
        {
            logger.Info(string.Format("axisAtNegativeHWLimitChanged {0} {1}", (ConveyorAxes) axis, isAtNegativeHwLimit));
            OnConveyorAxisAtNegativeHwLimitChanged?.Invoke((ConveyorAxes) axis, isAtNegativeHwLimit);
        }

        private void ConveyorAxisAtPositiveSwLimitChanged(int axis, bool isAtPositiveSwLimit)
        {
            logger.Info(string.Format("axisAtPositiveSWLimitChanged {0} {1}", (ConveyorAxes) axis, isAtPositiveSwLimit));
            OnConveyorAxisAtPositiveSwLimitChanged?.Invoke((ConveyorAxes) axis, isAtPositiveSwLimit);
        }

        private void ConveyorAxisAtNegativeSwLimitChanged(int axis, bool isAtNegativeSwLimit)
        {
            logger.Info(string.Format("axisAtNegativeSWLimitChanged {0} {1}", (ConveyorAxes) axis, isAtNegativeSwLimit));
            OnConveyorAxisAtNegativeSwLimitChanged?.Invoke((ConveyorAxes) axis, isAtNegativeSwLimit);
        }

        private void ConveyorAxisHomingBegin(int axis)
        {
            OnConveyorAxisHomingBegin?.Invoke((ConveyorAxes) axis);
        }

        private void ConveyorAxisHomingEnd(int axis, bool res)
        {
            OnConveyorAxisHomingEnd?.Invoke((ConveyorAxes) axis, res);
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
                    ScanningIndexChange?.Invoke(currentScanningIndex);
                }
            }
        }

        private readonly object _currentMotionCompleteReceivedLocker = new object();

        private int CurrentMotionCompleteReceived
        {
            get { return currentMotionCompleteReceived; }
            set
            {
                lock (_currentMotionCompleteReceivedLocker)
                {
                    if (value == currentMotionCompleteReceived)
                        return;
                    currentMotionCompleteReceived = value;
                    if (currentMotionCompleteReceived > 0)
                    {
                        HardwareNotifySingleMoveMotionCompleteReceived?.Invoke(currentMotionCompleteReceived);
                    }
                }

            }
        }

        private readonly object _currentMovePsxAckReceivedLocker = new object();

        private int CurrentMovePsxAckReceived
        {
            get { return currentMovePsxAckReceived; }
            set
            {
                lock (_currentMovePsxAckReceivedLocker)
                {
                    if (value == currentMovePsxAckReceived)
                        return;
                    currentMovePsxAckReceived = value;
                    if (currentMovePsxAckReceived > 0)
                    {
                        HardwareNotifySingleMovePSXAckReceived?.Invoke(currentMovePsxAckReceived);
                    }
                }
            }
        }

        public bool HasError => HasConveyorError || HasRobotError;
        public bool HasConveyorError => ConveyorStatus == ConveyorStatusCode.ERROR_STATUS ||
                                        ErrorCode != ConveyorErrorCode.ErrorSafe;
        public bool HasRobotError => GantryErrorCode != GantryErrorCode.NoError;

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
            var width = GetConveyorPosition(ConveyorAxes.Width);
            return width;
        }

        public double GetConveyorLifterAxisPosition()
        {
            var lifter = GetConveyorPosition(ConveyorAxes.Lifter);
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
            logger.Debug($"AcsWrapper:StartPanelLoad. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");
            
            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);
            WriteLifterDistances(parameters.Stage_1_LifterOnlyDistance, parameters.Stage_2_LifterAndClamperDistance);

            bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.LoadPanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.LoadPanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            logger.Debug($"AcsWrapper:StartPanelReload. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");

            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);
            WriteLifterDistances(parameters.Stage_1_LifterOnlyDistance, parameters.Stage_2_LifterAndClamperDistance);

            bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.ReloadPanel);
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

        public void StopPanelHandling()
        {
            acsUtils.WriteVariable(1, "StopPanelHandling", acsUtils.GetDBufferIndex());
            
            // wait for panel load/release buffer to end execution
            bufferHelper.WaitProgramEnd((ProgramBuffer) AcsBuffers.LoadPanel, -1);
            bufferHelper.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReloadPanel, -1);

            bufferHelper.WaitProgramEnd((ProgramBuffer) AcsBuffers.PreReleasePanel, -1);
            bufferHelper.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReleasePanel, -1);
        }

        public void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout)
        {
            logger.Debug($"AcsWrapper:StartPanelPreRelease. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");

            InitConveyorBufferParameters(parameters);

            bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.PreReleasePanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.PreReleasePanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout)
        {
            logger.Debug($"AcsWrapper:StartPanelRelease. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");
            
            InitConveyorBufferParameters(parameters);

            bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.ReleasePanel);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReleasePanel, timeout);

            UpdateConveyorStatus();
        }

        public IoStatus GetIoStatus()
        {
            return new IoStatus
            {
                // inputs
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
                ConveyorPressure = Convert.ToBoolean(acsUtils.ReadVar("ConveyorPressureSwitchFeedback_Bit")),
                ResetButton = Convert.ToBoolean(acsUtils.ReadVar("Reset_Button_Bit")),
                StartButton = Convert.ToBoolean(acsUtils.ReadVar("Start_Button_Bit")),
                StopButton = Convert.ToBoolean(acsUtils.ReadVar("Stop_Button_Bit")),
                AlarmCancelPushButton = Convert.ToBoolean(acsUtils.ReadVar("AlarmCancelPushButton_Bit")),
                UpstreamBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamBoardAvailableSignal_Bit")),
                UpstreamFailedBoardAvailableSignal = Convert.ToBoolean(acsUtils.ReadVar("UpstreamFailedBoardAvailableSignal_Bit")),
                DownstreamMachineReadySignal = Convert.ToBoolean(acsUtils.ReadVar("DownstreamMachineReadySignal_Bit")),
                BypassNormal = Convert.ToBoolean(acsUtils.ReadVar("ByPassR2L")),
                BypassDirection = Convert.ToBoolean(acsUtils.ReadVar("ByPassL2R")),
                EstopRight = Convert.ToBoolean(acsUtils.ReadVar("Estop_R_Bit")), 
                EstopLeft = Convert.ToBoolean(acsUtils.ReadVar("Estop_L_Bit")),
                EstopAndDoorOpenFeedback = Convert.ToBoolean(acsUtils.ReadVar("EstopAndDoorOpenFeedback_Bit")),

                // outputs
                LockStopper = Convert.ToBoolean(acsUtils.ReadVar("LockStopper_Bit")),
                RaiseBoardStopStopper = Convert.ToBoolean(acsUtils.ReadVar("RaiseBoardStopStopper_Bit")),
                VacuumChuckValve = Convert.ToBoolean(acsUtils.ReadVar("HighVacummValve")),
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
                StopSensor = Convert.ToBoolean(acsUtils.ReadVar("StopSensor_Bit")),
                SmemaUpStreamMachineReady = Convert.ToBoolean(acsUtils.ReadVar("SmemaUpStreamMachineReady_Bit")),
                DownStreamBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("DownStreamBoardAvailable_Bit")),
                SmemaDownStreamFailedBoardAvailable = Convert.ToBoolean(acsUtils.ReadVar("SmemaDownStreamFailedBoardAvailable_Bit")),

                // invert vacuum output
                BeltShroudVaccumOn = !Convert.ToBoolean(acsUtils.ReadVar("BeltShroudVaccumON_Bit")),
                VacuumChuckEjector = !Convert.ToBoolean(acsUtils.ReadVar("VacuumChuckEjector_Bit")),
            };
        }

        public void SetOutputs(SetOutputParameters outputs)
        {
            acsUtils.WriteVariable(Convert.ToInt32(outputs.LockStopper), "LockStopper_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.RaiseBoardStopStopper), "RaiseBoardStopStopper_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.VacuumChuckValve), "HighVacummValve");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.ClampPanel), "ClampPanel_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.ResetButtonLight), "ResetButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StartButtonLight), "StartButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StopButtonLight), "StopButtonLight_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightRed), "TowerLightRed_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightYellow), "TowerLightYellow_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightGreen), "TowerLightGreen_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightBlue), "TowerLightBlue_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.TowerLightBuzzer), "TowerLightBuzzer_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.SensorPower), "SensorPowerOnOff_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.StopSensor), "StopSensor_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.SmemaUpStreamMachineReady), "SmemaUpStreamMachineReady_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.DownStreamBoardAvailable), "DownStreamBoardAvailable_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(outputs.SmemaDownStreamFailedBoardAvailable),
                "SmemaDownStreamFailedBoardAvailable_Bit");

            // invert vacuum output
            acsUtils.WriteVariable(Convert.ToInt32(!outputs.BeltShroudVacuumOn), "BeltShroudVaccumON_Bit");
            acsUtils.WriteVariable(Convert.ToInt32(!outputs.VacuumChuckEjector), "VacuumChuckEjector_Bit");
        }

        public void ChangeConveyorWidth(ChangeWidthBufferParameters parameters, int timeout)
        {
            logger.Debug($"AcsWrapper:ChangeConveyorWidth. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");

            InitConveyorBufferParameters(parameters);

            bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.ChangeWidth);
            api.WaitProgramEnd((ProgramBuffer) AcsBuffers.ChangeWidth, timeout);

            UpdateConveyorStatus();
        }

        private void UpdateConveyorStatus()
        {
            ConveyorStatus = (ConveyorStatusCode) Convert.ToInt16(acsUtils.ReadVar("CURRENT_STATUS"));
            ErrorCode = (ConveyorErrorCode) Convert.ToInt16(acsUtils.ReadVar("ERROR_CODE"));
            
            logger.Debug($"AcsWrapper:UpdateConveyorStatus. ConveyorStatus: {ConveyorStatus}, ErrorCode: {ErrorCode}");
        }

        private void UpdateRobotStatus()
        {
            GantryStatus = (GantryStatusCode) Convert.ToInt16(acsUtils.ReadVar("GANTRY_STATUS"));
            GantryErrorCode = GantryStatus == GantryStatusCode.Error
                ? (GantryErrorCode) Convert.ToInt16(acsUtils.ReadVar("GANTRY_ERROR"))
                : GantryErrorCode.NoError;

            if (GantryErrorCode != GantryErrorCode.NoError) return;

            UpdateXAxisFault();
            UpdateYAxisFault();
            UpdateZAxisFault();
            
            logger.Debug($"AcsWrapper:UpdateRobotStatus. Status: {GantryStatus}, ErrorCode: {GantryErrorCode}");
        }

        private void UpdateXAxisFault()
        {
            var axis = axesCache[GantryAxes.X];
            axis.UpdateFaultFromController();
            if (axis.AtNegativeHwLimit) {
                GantryErrorCode = GantryErrorCode.XNegativeHardLimitHit;
            }
            else if (axis.AtNegativeSwLimit) {
                GantryErrorCode = GantryErrorCode.XNegativeSoftLimitHit;
            }
            else if (axis.AtPositiveHwLimit) {
                GantryErrorCode = GantryErrorCode.XPositiveHardLimitHit;
            }
            else if (axis.AtPositiveSwLimit) {
                GantryErrorCode = GantryErrorCode.XPositiveSoftLimitHit;
            }
        }

        private void UpdateYAxisFault()
        {
            var axis = axesCache[GantryAxes.Y];
            axis.UpdateFaultFromController();
            if (axis.AtNegativeHwLimit) {
                GantryErrorCode = GantryErrorCode.YNegativeHardLimitHit;
            }
            else if (axis.AtNegativeSwLimit) {
                GantryErrorCode = GantryErrorCode.YNegativeSoftLimitHit;
            }
            else if (axis.AtPositiveHwLimit) {
                GantryErrorCode = GantryErrorCode.YPositiveHardLimitHit;
            }
            else if (axis.AtPositiveSwLimit) {
                GantryErrorCode = GantryErrorCode.YPositiveSoftLimitHit;
            }
        }

        private void UpdateZAxisFault()
        {
            var axis = axesCache[GantryAxes.Z];
            axis.UpdateFaultFromController();
            if (axis.AtNegativeHwLimit) {
                GantryErrorCode = GantryErrorCode.ZNegativeHardLimitHit;
            }
            else if (axis.AtNegativeSwLimit) {
                GantryErrorCode = GantryErrorCode.ZNegativeSoftLimitHit;
            }
            else if (axis.AtPositiveHwLimit) {
                GantryErrorCode = GantryErrorCode.ZPositiveHardLimitHit;
            }
            else if (axis.AtPositiveSwLimit) {
                GantryErrorCode = GantryErrorCode.ZPositiveSoftLimitHit;
            }
        }

        public void PowerOnRecoverFromEmergencyStop(PowerOnRecoverFromEmergencyStopBufferParameters parameter,
            int timeout)
        {
            try {
                InitConveyorBufferParameters(parameter);
                UpdateConveyorStatus();

                bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.PowerOnRecoverFromEmergencyStop);
                api.WaitProgramEnd((ProgramBuffer) AcsBuffers.PowerOnRecoverFromEmergencyStop, timeout);
                
                UpdateConveyorStatus();
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
                EstopRight = Convert.ToBoolean(acsUtils.ReadVar("Estop_R_Bit")),
                EstopLeft = Convert.ToBoolean(acsUtils.ReadVar("Estop_L_Bit")),
                SafetyRelay = Convert.ToBoolean(acsUtils.ReadVar("EstopAndDoorOpenFeedback_Bit")),
            };
        }

        public ClampSensors GetClampSensorsStatus()
        {
            return new ClampSensors
            {
                RearClampUp = Convert.ToBoolean(acsUtils.ReadVar("RearClampUp_Bit")),
                FrontClampUp = Convert.ToBoolean(acsUtils.ReadVar("FrontClampUp_Bit")),
                RearClampDown = Convert.ToBoolean(acsUtils.ReadVar("RearClampDown_Bit")),
                FrontClampDown = Convert.ToBoolean(acsUtils.ReadVar("FrontClampDown_Bit")),
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
            return Convert.ToBoolean(acsUtils.ReadVar("ByPassR2L")) || Convert.ToBoolean(acsUtils.ReadVar("ByPassL2R"));
        }

        public bool IsBypassDirectionRightToLeft()
        {
            return Convert.ToBoolean(acsUtils.ReadVar("ByPassR2L"));
        }

        public void BypassModeOn(BypassModeBufferParameters parameter)
        {
            // activate Bypass mode
            try {
                InitConveyorBufferParameters(parameter);
                bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.BypassMode);
            }
            catch (Exception ex) {
                throw new AcsException(ex.Message);
            }
        }

        public void BypassModeOff()
        {
            // deactivate Bypass mode
            try {
                bufferHelper.StopBuffer((ProgramBuffer) AcsBuffers.BypassMode);
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

        public void SetPartialManualSmemaMode()
        {
            acsUtils.WriteVariable(1, "SqTriggerSmemaUpStreamMachineReady");
        }

        public void ResetPartialManualSmemaMode()
        {
            acsUtils.WriteVariable(0, "SqTriggerSmemaUpStreamMachineReady");
        }

        public void SetMachineReady()
        {
            acsUtils.WriteVariable(1, "SmemaUpStreamMachineReady_Bit");
        }

        public void ResetMachineReady()
        {
            acsUtils.WriteVariable(0, "SmemaUpStreamMachineReady_Bit");
        }

        public void SetSmemaDownStreamFailedBoardAvailable() {
            acsUtils.WriteVariable(1, "SmemaDownStreamFailedBoardAvailable_Bit");
        }

        public void ResetSmemaDownStreamFailedBoardAvailable() {
            acsUtils.WriteVariable(0, "SmemaDownStreamFailedBoardAvailable_Bit");
        }

        public void SetFailedBoard()
        {
            acsUtils.WriteVariable(1, "FailedBoard");
        }

        public void ResetFailedBoard()
        {
            acsUtils.WriteVariable(0, "FailedBoard");
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
                bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.WidthHoming);
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
            if (conveyorAxesCache[ConveyorAxes.Conveyor].EncoderInverted)
            {
                Jog(ConveyorAxes.Conveyor, velocity * -1, acceleration, deceleration);
            }
            else {
                Jog(ConveyorAxes.Conveyor, velocity, acceleration, deceleration);
            }
        }

        public void JogConveyorAxisRightToLeft(double velocity, double acceleration, double deceleration)
        {
            if (conveyorAxesCache[ConveyorAxes.Conveyor].EncoderInverted) {
                Jog(ConveyorAxes.Conveyor, velocity, acceleration, deceleration);
            }
            else {
                Jog(ConveyorAxes.Conveyor, velocity * -1, acceleration, deceleration);
            }
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
                bufferHelper.RunBuffer((ProgramBuffer) AcsBuffers.LifterHoming);
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