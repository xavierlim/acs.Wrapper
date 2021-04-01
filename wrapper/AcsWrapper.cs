// Decompiled with JetBrains decompiler
// Type: AcsWrapperImpl.AcsWrapper
// Assembly: AcsWrapper, Version=1.0.0.9, Culture=neutral, PublicKeyToken=null
// MVID: 1AE830F3-83DA-46CC-8B8A-D7CB7D22A02B
// Assembly location: D:\user\Documents\CyberOptics\tasks\acs platform\source\AcsWrapper - 11Mar2021\NewWrapper\NewWrapper\lib\AcsWrapper.dll

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
        private readonly Api Ch;
        private object lockObject = new object();
        private string ipDefault = "192.168.157.1";
        private bool IscanLoopRun = false;
        private int sleepPoling = 50;
        private AutoResetEvent waiteExitFromPoling = new AutoResetEvent(false);
        private readonly ILogger _logger = LoggersManager.SystemLogger;
        private static AcsWrapper instance;
        private Dictionary<GantryAxes, ACSAxis> axesCache = new Dictionary<GantryAxes, ACSAxis>();
        private Dictionary<ConveyorAxes, ACSAxis> conveyorAxesCache = new Dictionary<ConveyorAxes, ACSAxis>();

        private IRobotControlSetting _robotSettings =
            (IRobotControlSetting) Configuration.Configuration.GetSettingsSection("Robot");

        private readonly bool isSimulation;
        private AcsUtils acsUtils;
        private BufferHelper bufferHelper;
        private bool isScanningBufferRun = false;
        private bool isConveyorBufferRun = false;
        private bool isConnected = false;
        private int currentScanningIndex = -1;
        private int currentMotionCompleteRecvd = 0;
        private int currentMovePSXAckRecvd = 0;

        internal AcsWrapper()
        {
            isSimulation = AcsSimHelper.IsEnable();

            Ch = new Api();
            acsUtils = new AcsUtils(Ch);
            bufferHelper = new BufferHelper(Ch, acsUtils, isSimulation);
        }

        public static AcsWrapper Acs => instance ?? (instance = new AcsWrapper());

        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if (value == isConnected)
                    return;
                isConnected = value;
                Action<bool> connectionStatusChanged = ConnectionStatusChanged;
                if (connectionStatusChanged != null)
                    connectionStatusChanged(isConnected);
            }
        }

        public string FirmwareVersion => Ch.GetFirmwareVersion();

        public uint NETLibraryVersion => Ch.GetNETLibraryVersion();

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

        public event Action HardwareNotifySingleMoveMotionCompleteRecvd;

        public event Action HardwareNotifySingleMovePSXAckRecvd;

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

        public event Action<ConveyorAxes, bool> ConveyorAxisAtPositiveHWLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtNegativeHWLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtPositiveSWLimitChanged;

        public event Action<ConveyorAxes, bool> ConveyorAxisAtNegativeSWLimitChanged;

        public event Action<ConveyorAxes> ConveyorAxisMovementBegin;

        public event Action<ConveyorAxes, bool> ConveyorAxisMovementEnd;

        public event Action<ConveyorAxes> ConveyorAxisHomingBegin;

        public event Action<ConveyorAxes, bool> ConveyorAxisHomingEnd;

        public void Connect(string ip)
        {
            ip = isSimulation ? "localhost" : "10.0.0.100";
            _logger.Info($"AcsWrapper: Connect. IP {ip}");

            lock (lockObject) {
                teminateOldConnections();
                try {
                    Ch.OpenCommEthernet(ip, 700);
                }
                catch (Exception e) {
                    _logger.Error($"AcsWrapper: Connect. Connection attempt exception [{e.Message}]");
                    throw new AcsException(e.Message);
                }

                IsConnected = Ch.IsConnected;
                if (!IsConnected) {
                    _logger.Error("AcsWrapper:Connect. Controller not connected");
                    throw new AcsException("Controller not connected");
                }

                EnableAcsEvents();
                bufferHelper.InitDBuffer();
                initAxesCache();
                this.initConveyorAxesCache();
                initAxisNumbersAtController();
                ReadAxesSettignsFromConfig();
                enableAllBlocking();
                initBuffers();
                ThreadPool.QueueUserWorkItem(s => scanLoop());
            }
        }

        public bool DisConnect()
        {
            _logger.Info(nameof(DisConnect), 334, nameof(DisConnect),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            lock (lockObject) {
                if (Ch == null) {
                    IsConnected = false;
                    return true;
                }

                try {
                    Ch.CloseComm();
                    IsConnected = Ch.IsConnected;
                }
                catch (Exception ex) {
                }

                IscanLoopRun = false;
                waiteExitFromPoling.WaitOne(5000);
                return !IsConnected;
            }
        }

        public bool IsIdle(GantryAxes axis)
        {
            _logger.Info(string.Format("IsIdle(axis = {0})", axis), 374, nameof(IsIdle),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 377, nameof(IsIdle),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Idle;
            throw new ArgumentException("Axis not exist ");
        }

        public bool IsIdle(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("IsIdle(axis = {0})", (object) axis), 515, nameof(IsIdle),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 518, nameof(IsIdle),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Idle;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Enabled(GantryAxes axis)
        {
            this._logger.Info(string.Format("Enabled(axis = {0})", (object) axis), 539, nameof(Enabled),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 542, nameof(Enabled),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].Enabled;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Enabled(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Enabled(axis = {0})", (object) axis), 559, nameof(Enabled),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 562, nameof(Enabled),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Enabled;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Homed(GantryAxes axis)
        {
            this._logger.Info(string.Format("Homed(axis = {0})", (object) axis), 582, nameof(Homed),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 585, nameof(Homed),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].Homed;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Homed(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Homed(axis = {0})", (object) axis), 602, nameof(Homed),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 605, nameof(Homed),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Homed;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Ready(GantryAxes axis)
        {
            this._logger.Info(string.Format("Ready(axis = {0})", (object) axis), 626, nameof(Ready),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 629, nameof(Ready),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].Ready;
            throw new ArgumentException("Axis not exist ");
        }

        public bool Ready(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Ready(axis = {0})", (object) axis), 646, nameof(Ready),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 649, nameof(Ready),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Ready;
            throw new ArgumentException("Axis not exist ");
        }

        public double Position(GantryAxes axis)
        {
            this._logger.Info(string.Format("Position(axis = {0})", (object) axis), 674, nameof(Position),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 677, nameof(Position),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return 0.0;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].Position;
            throw new ArgumentException("Axis not exist ");
        }

        public double Position(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Position(axis = {0})", (object) axis), 694, nameof(Position),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 697, nameof(Position),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return 0.0;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Position;
            throw new ArgumentException("Axis not exist ");
        }

        public double Velocity(GantryAxes axis)
        {
            this._logger.Info(string.Format("Velocity(axis = {0})", (object) axis), 717, nameof(Velocity),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 720, nameof(Velocity),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return 0.0;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].CurrerntVelocity;
            throw new ArgumentException("Axis not exist ");
        }

        public double Velocity(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Velocity(axis = {0})", (object) axis), 737, nameof(Velocity),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 740, nameof(Velocity),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return 0.0;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].CurrerntVelocity;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtHomeSensor(GantryAxes axis)
        {
            this._logger.Info(string.Format("AtHomeSensor(axis = {0})", (object) axis), 765, nameof(AtHomeSensor),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 768, nameof(AtHomeSensor),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].AtHomeSensor;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtHomeSensor(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("AtHomeSensor(axis = {0})", (object) axis), 785, nameof(AtHomeSensor),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 788, nameof(AtHomeSensor),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].AtHomeSensor;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtPositiveHWLimit(GantryAxes axis)
        {
            this._logger.Info(string.Format("AtPositiveHWLimit(axis = {0})", (object) axis), 808,
                nameof(AtPositiveHWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 811, nameof(AtPositiveHWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].AtPositiveHWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtPositiveHWLimit(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("AtPositiveHWLimit(axis = {0})", (object) axis), 828,
                nameof(AtPositiveHWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 831, nameof(AtPositiveHWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].AtPositiveHWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtNegativeHWLimit(GantryAxes axis)
        {
            this._logger.Info(string.Format("AtNegativeHWLimit(axis = {0})", (object) axis), 851,
                nameof(AtNegativeHWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 854, nameof(AtNegativeHWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].AtNegativeHWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtNegativeHWLimit(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("AtNegativeHWLimit(axis = {0})", (object) axis), 871,
                nameof(AtNegativeHWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 874, nameof(AtNegativeHWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].AtNegativeHWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtPositiveSWLimit(GantryAxes axis)
        {
            this._logger.Info(string.Format("AtPositiveSWLimit(axis = {0})", (object) axis), 894,
                nameof(AtPositiveSWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 897, nameof(AtPositiveSWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].AtPositiveSWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtPositiveSWLimit(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("AtPositiveSWLimit(axis = {0})", (object) axis), 914,
                nameof(AtPositiveSWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 917, nameof(AtPositiveSWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].AtPositiveSWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtNegativeSWLimit(GantryAxes axis)
        {
            this._logger.Info(string.Format("AtNegativeSWLimit(axis = {0})", (object) axis), 937,
                nameof(AtNegativeSWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 940, nameof(AtNegativeSWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.axesCache.ContainsKey(axis))
                return this.axesCache[axis].AtNegativeSWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        public bool AtNegativeSWLimit(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("AtNegativeSWLimit(axis = {0})", (object) axis), 957,
                nameof(AtNegativeSWLimit), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 960, nameof(AtNegativeSWLimit),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].AtNegativeSWLimit;
            throw new ArgumentException("Axis not exist ");
        }

        private bool GetInput(int port, int bit)
        {
            _logger.Info(string.Format("GetInput(port = {0},bit = {1})", port, bit), 651,
                nameof(GetInput),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (IsConnected)
                return Convert.ToBoolean(Ch.GetInput(port, bit));
            _logger.Info("Controller not connected", 654, nameof(GetInput),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            return false;
        }

        private bool GetOutput(int port, int bit)
        {
            _logger.Info(string.Format("GetOutput(port = {0},bit = {1})", port, bit), 669,
                nameof(GetOutput),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (IsConnected)
                return Convert.ToBoolean(Ch.GetOutput(port, bit));
            _logger.Info("Controller not connected", 672, nameof(GetOutput),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            return false;
        }

        private void SetOutput(int port, int bit, bool value)
        {
            _logger.Info(
                string.Format("SetOutput(port = {0},bit = {1},value = {2})", port, bit,
                    value), 688, nameof(SetOutput),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected)
                _logger.Info("Controller not connected", 691, nameof(SetOutput),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            else
                Ch.SetOutput(port, bit, Convert.ToInt32(value));
        }

        public bool PrepareScanning(
            List<IPvTuple3D> pvTuple3DList,
            int triggerToCameraStartPort,
            int triggerToCameraStartBit,
            int triggerFromCameraContinuePort,
            int triggerFromCameraContinueBit,
            int triggerFromCameraTimeOut)
        {
            _logger.Info(
                string.Format(
                    "PrepareScanning(triggerToCameraStartPort = {0},triggerToCameraStartBit = {1},triggerFromCameraContinuePort = {2},triggerFromCameraContinueBit = {3},triggerFromCameraTimeOut = {4}))",
                    (object) triggerToCameraStartPort, (object) triggerToCameraStartBit,
                    (object) triggerFromCameraContinuePort, (object) triggerFromCameraContinueBit,
                    (object) triggerFromCameraTimeOut), 718, nameof(PrepareScanning),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 721, nameof(PrepareScanning),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            bufferHelper.PrepareScanningBuffer(pvTuple3DList, triggerToCameraStartPort, triggerToCameraStartBit,
                triggerFromCameraContinuePort, triggerFromCameraContinueBit, triggerFromCameraTimeOut);
            return true;
        }

        public bool StartScanning()
        {
            _logger.Info("StartScanning()", 736, nameof(StartScanning),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 739, nameof(StartScanning),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            CurrentScanningIndex = -1;
            CurrentMotionCompleteRecvd = 0;
            CurrentMovePSXAckRecvd = 0;
            acsUtils.RunBuffer(ProgramBuffer.ACSC_BUFFER_9);
            isScanningBufferRun = acsUtils.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9);
            if (isScanningBufferRun) {
                foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache)
                    keyValuePair.Value.ScanningBufferRun = true;
                Action scanningBegin = ScanningBegin;
                if (scanningBegin != null)
                    scanningBegin();
            }

            return isScanningBufferRun;
        }

        public bool StartConveyorBuffer(AcsBuffers buffer)
        {
            _logger.Info(string.Format("StartConveyorBuffer({0})", buffer), 767,
                nameof(StartConveyorBuffer),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 770, nameof(StartConveyorBuffer),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            acsUtils.RunBuffer((ProgramBuffer) buffer);
            isConveyorBufferRun = acsUtils.IsProgramRunning((ProgramBuffer) buffer);
            return isConveyorBufferRun;
        }

        public bool SetReleaseCommandReceived(bool commandReceived)
        {
            _logger.Info(string.Format("SetReleaseCommandReceived({0})", commandReceived), 781,
                nameof(SetReleaseCommandReceived),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 784, nameof(SetReleaseCommandReceived),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            int NBuf = 19;
            acsUtils.WriteVariable(commandReceived ? 1 : 0, "ReleaseCommandReceived", NBuf);
            return true;
        }

        public bool InitConveyorBufferParameters(BypassModeBufferParameters parameters)
        {
            _logger.Info($"InitBypassModeBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.BypassMode;

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
            _logger.Info($"InitChangeWidthBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.ChangeWidth;

            acsUtils.WriteVariable(parameters.ConveyorSpecifiedWidth, "ConveyorSpecifiedWidth", buffer);
            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "ChangeWidthBuffer_WaitTimeToSearch");


            return true;
        }

        public bool InitConveyorBufferParameters(FreePanelBufferParameters parameters)
        {
            _logger.Info($"InitFreePanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.FreePanel;

            acsUtils.WriteVariable(parameters.UnclampLiftDelayTime, "FreePanelBuffer_UnclampLiftDelayTime");
            acsUtils.WriteVariable(parameters.WaitTimeToUnlift, "FreePanelBuffer_WaitTimeToUnlift");
            acsUtils.WriteVariable(parameters.WaitTimeToUnclamp, "FreePanelBuffer_WaitTimeToUnclamp");

            return true;
        }

        public bool InitConveyorBufferParameters(InternalMachineLoadBufferParameters parameters)
        {
            _logger.Info($"InitInternalMachineLoadBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.InternalMachineLoad;

            acsUtils.WriteVariable(parameters.WaitTimeToSlow, "InternalMachineLoadBuffer_WaitTimeToSlow");
            acsUtils.WriteVariable(parameters.WaitTimeToAlign, "InternalMachineLoadBuffer_WaitTimeToAlign");
            acsUtils.WriteVariable(parameters.SlowDelayTime, "InternalMachineLoadBuffer_SlowDelayTime");



            return true;
        }

        public bool InitConveyorBufferParameters(LoadPanelBufferParameters parameters)
        {
            _logger.Info($"InitLoadPanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.LoadPanel;

            acsUtils.WriteVariable(parameters.WaitTimeToAcq, "LoadPanelBuffer_WaitTimeToAcq");

            return true;
        }

        public bool InitConveyorBufferParameters(PowerOnRecoverFromEmergencyStopBufferParameters parameters)
        {
            _logger.Info($"InitPowerOnRecoverFromEmergencyStopBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
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
            _logger.Info($"InitPreReleasePanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.PreReleasePanel;

            acsUtils.WriteVariable(parameters.WaitTimeToExit, "PreReleasePanelBuffer_WaitTimeToExit");



            return true;
        }

        public bool InitConveyorBufferParameters(ReleasePanelBufferParameters parameters)
        {
            _logger.Info($"InitReleasePanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.ReleasePanel;

            acsUtils.WriteVariable(parameters.WaitTimeToExit, "ReleasePanelBuffer_WaitTimeToExit");
            acsUtils.WriteVariable(parameters.WaitTimeToRelease, "ReleasePanelBuffer_WaitTimeToRelease");
            acsUtils.WriteVariable(parameters.WaitTimeToSmema, "ReleasePanelBuffer_WaitTimeToSmema");
            acsUtils.WriteVariable(parameters.WaitTimeToCutout, "ReleasePanelBuffer_WaitTimeToCutout");
            acsUtils.WriteVariable(parameters.WaitTimeToBeltVacuum, "ReleasePanelBuffer_WaitTimeToBeltVacuum");

            return true;
        }

        public bool InitConveyorBufferParameters(ReloadPanelBufferParameters parameters)
        {
            _logger.Info($"InitReloadPanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.ReloadPanel;

            acsUtils.WriteVariable(parameters.WaitTimeToSearch, "ReloadPanelBuffer_WaitTimeToSearch");
            acsUtils.WriteVariable(parameters.ReloadDelayTime, "ReloadPanelBuffer_ReloadDelayTime");

            return true;
        }

        public bool InitConveyorBufferParameters(SecurePanelBufferParameters parameters)
        {
            _logger.Info($"InitSecurePanelBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }
            int buffer = (int)AcsBuffers.SecurePanel;

            acsUtils.WriteVariable(parameters.ClampLiftDelayTime, "SecurePanelBuffer_ClampLiftDelayTime");
            acsUtils.WriteVariable(parameters.WaitTimeToPanelClamped, "SecurePanelBuffer_WaitTimeToPanelClamped");
            acsUtils.WriteVariable(parameters.WaitTimeToLifted, "SecurePanelBuffer_WaitTimeToLifted");
            acsUtils.WriteVariable(parameters.WaitTimeToUnstop, "SecurePanelBuffer_WaitTimeToUnstop");

            acsUtils.WriteVariable(parameters.Stage_1_LifterOnlyDistance, "Stage_1_LifterOnlyDistance", buffer);
            acsUtils.WriteVariable(parameters.Stage_2_LifterAndClamperDistance, "Stage_2_LifterAndClamperDistance", buffer);

            return true;
        }

        public bool InitConveyorBufferParameters(HomeConveyorWidthParameters parameters)
        {
            _logger.Info($"InitHomeConveyorWidthParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
                return false;
            }

            acsUtils.WriteGlobalReal(parameters.HOME_VEL_IN, "HOME_VEL_IN", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HOME_VEL_OUT, "HOME_VEL_OUT", (int)ConveyorAxes.Width);
            acsUtils.WriteGlobalReal(parameters.HOME_OFFSET, "HOME_OFFSET", (int)ConveyorAxes.Width);

            return true;
        }

        public bool InitConveyorBufferParameters(DBufferParameters parameters)
        {
            _logger.Info($"InitDBufferParameters()");
            if (!IsConnected)
            {
                _logger.Info("Controller not connected");
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

            // acsUtils.WriteVariable(parameters.DistanceBetweenEntryAndStopSensor, "DistanceBetweenEntryAndStopSensor", buffer);
            acsUtils.WriteVariable(parameters.DistanceBetweenSlowPositionAndStopSensor, "DistanceBetweenSlowPositionAndStopSensor", buffer);
            // acsUtils.WriteVariable(parameters.DistanceBetweenStopSensorAndExitSensor, "DistanceBetweenStopSensorAndExitSensor", buffer);
            // acsUtils.WriteVariable(parameters.DistanceBetweenSlowPositionAndExitSensor, "DistanceBetweenSlowPositionAndExitSensor", buffer);

            return true;
        }

        public bool InitConveyorBufferParameters(ConveyorDirection conveyorDirection)
        {
            _logger.Info("Init ConveyorDirection");
            if (!this.IsConnected) {
                _logger.Error("Controller not connected");
                return false;
            }

            int dbufferIndex = this.acsUtils.GetDBufferIndex();
            this.acsUtils.WriteVariable((object) (int) conveyorDirection, "ConveyorDirection", dbufferIndex);
            return true;
        }

        public void Reset()
        {
            _logger.Info("Reset()", 1011, nameof(Reset),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1014, nameof(Reset),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache) {
                    keyValuePair.Value.ClearError();
                    keyValuePair.Value.RestoreDefualtSettings();
                }
            }
        }

        public void ClearError(GantryAxes axis)
        {
            _logger.Info(string.Format("ClearError(axis = {0})", axis), 1030, nameof(ClearError),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1033, nameof(ClearError),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (axesCache.ContainsKey(axis))
                    axesCache[axis].ClearError();
                throw new ArgumentException("Axis not exist ");
            }
        }

        public bool Enable(GantryAxes axis)
        {
            _logger.Info(string.Format("Enable(axis = {0})", axis), 1050, nameof(Enable),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1053, nameof(Enable),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Enable();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool Disable(GantryAxes axis)
        {
            _logger.Info(string.Format("Disable(axis = {0})", axis), 1070, nameof(Disable),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1073, nameof(Disable),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Disable();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool ReloadConfigParameters(bool forZOnly = false)
        {
            _logger.Info(string.Format("ReloadConfigParameters(forZOnly = {0})", forZOnly), 1086,
                nameof(ReloadConfigParameters),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1089, nameof(ReloadConfigParameters),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (forZOnly)
                return ReloadConfigParameters(GantryAxes.Z);
            foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache)
                ReloadConfigParameters(keyValuePair.Key);
            return true;
        }

        public bool ReloadConfigParameters(GantryAxes axis)
        {
            _logger.Info(string.Format("ReloadConfigParameters(axis = {0})", axis), 1105,
                nameof(ReloadConfigParameters),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1108, nameof(ReloadConfigParameters),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (!axesCache.ContainsKey(axis))
                throw new ArgumentException("Axis not exist ");
            axesCache[axis].ReloadConfigParameters();
            return true;
        }

        public bool Init(bool forZOnly = false)
        {
            _logger.Info(string.Format("Init(forZOnly = {0})", forZOnly), 1130, nameof(Init),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1133, nameof(Init),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (forZOnly)
                return Init(GantryAxes.Z);
            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache) {
                KeyValuePair<GantryAxes, ACSAxis> item = keyValuePair;
                taskList.Add(Task.Run((Action) (() => Init(item.Key))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool Init(List<AxisInitParameters> initParameters, bool forZOnly = false)
        {
            _logger.Info(
                string.Format("Init(List<AxisInitParameters> initParameters,forZOnly = {0})", forZOnly), 1169,
                nameof(Init),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1172, nameof(Init),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
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
            foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache) {
                KeyValuePair<GantryAxes, ACSAxis> item = keyValuePair;
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
            _logger.Info(string.Format("Init(axis = {0})", axis), 1232, nameof(Init),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1235, nameof(Init),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Init(true);
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool Init(AxisInitParameters initParameters)
        {
            _logger.Info("Init(AxisInitParameters initParameters)", 1257, nameof(Init),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1260, nameof(Init),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
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
            _logger.Info("MoveAbsolute(List<AxisMoveParameters> axesToMove)", 1287, nameof(MoveAbsolute),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1290, nameof(MoveAbsolute),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0) {
                if (axesToMove == null)
                    _logger.Info("axesToMove = null", 1297, nameof(MoveAbsolute),
                        "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                else
                    _logger.Info("axesToMove.Count = 0", 1299, nameof(MoveAbsolute),
                        "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
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

        public bool MoveAbsolute(
            GantryAxes axis,
            double targetPos,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            _logger.Info(
                string.Format("MoveAbsolute(axis = {0},targetPos= {1}, vel= {2}, acc= {3}, dec= {4})", (object) axis,
                    (object) targetPos, (object) vel, (object) acc, (object) dec), 1328, nameof(MoveAbsolute),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1331, nameof(MoveAbsolute),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveRelative(List<AxisMoveParameters> axesToMove)
        {
            _logger.Info("MoveRelative(List<AxisMoveParameters> axesToMove)", 1354, nameof(MoveRelative),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1357, nameof(MoveRelative),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
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

        public bool MoveRelative(
            GantryAxes axis,
            double relativePosition,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            _logger.Info(
                string.Format("MoveRelative(axis = {0},relativePosition= {1}, vel= {2}, acc= {3}, dec= {4})",
                    (object) axis, (object) relativePosition, (object) vel, (object) acc, (object) dec), 1389,
                nameof(MoveRelative),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1392, nameof(MoveRelative),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Jog(GantryAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            _logger.Info(
                string.Format("Jog(axis = {0}, vel= {1}, acc= {2}, dec= {3})", (object) axis, (object) vel,
                    (object) acc, (object) dec), 1412, nameof(Jog),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1415, nameof(Jog),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Jog(true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool StopAll()
        {
            _logger.Info("StopAll()", 1431, nameof(StopAll),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1434, nameof(StopAll),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
            foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache)
                keyValuePair.Value.Stop();
            return true;
        }

        public bool Stop(GantryAxes axis)
        {
            _logger.Info(string.Format("Stop(axis={0})", axis), 1455, nameof(Stop),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1458, nameof(Stop),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Stop();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool Abort(GantryAxes axis)
        {
            _logger.Info(string.Format("Abort(axis={0})", axis), 1476, nameof(Abort),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1479, nameof(Abort),
                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);

            if (axesCache.ContainsKey(axis))
                return axesCache[axis].Abort();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(GantryAxes axis, double pos)
        {
            _logger.Info(string.Format("SetRPos(axis={0},pos={1})", axis, pos), 1499,
                nameof(SetRPos),
                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
            if (!IsConnected) {
                _logger.Info("Controller not connected", 1502, nameof(SetRPos),
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
            this._logger.Info("Reset()", 1877, nameof(ResetConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1880, nameof(ResetConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                foreach (KeyValuePair<ConveyorAxes, ACSAxis> keyValuePair in this.conveyorAxesCache) {
                    keyValuePair.Value.ClearError();
                    keyValuePair.Value.RestoreDefualtSettings();
                }
            }
        }

        public void ClearError(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("ClearError(axis = {0})", (object) axis), 1896, nameof(ClearError),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1899, nameof(ClearError),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (this.conveyorAxesCache.ContainsKey(axis))
                    this.conveyorAxesCache[axis].ClearError();
                throw new ArgumentException("Axis not exist ");
            }
        }

        public bool Enable(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Enable(axis = {0})", (object) axis), 1916, nameof(Enable),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1919, nameof(Enable),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Enable();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool Disable(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Disable(axis = {0})", (object) axis), 1936, nameof(Disable),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1939, nameof(Disable),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Disable();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool ReloadConfigParameters(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("ReloadConfigParameters(axis = {0})", (object) axis), 1952,
                nameof(ReloadConfigParameters),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1955, nameof(ReloadConfigParameters),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (!this.conveyorAxesCache.ContainsKey(axis))
                throw new ArgumentException("Axis not exist ");
            this.conveyorAxesCache[axis].ReloadConfigParameters();
            return true;
        }

        public bool InitConveyorAxes()
        {
            this._logger.Info("Init()", 1977, nameof(InitConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 1980, nameof(InitConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<ConveyorAxes, ACSAxis> keyValuePair in this.conveyorAxesCache) {
                KeyValuePair<ConveyorAxes, ACSAxis> item = keyValuePair;
                taskList.Add(Task.Run((Action) (() => this.Init(item.Key))));
            }

            Task.WaitAll(taskList.ToArray());
            return true;
        }

        public bool Init(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Init(axis = {0})", (object) axis), 2008, nameof(Init),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2011, nameof(Init),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Init(true);
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool MoveAbsolute(List<ConveyorAxesMoveParameters> axesToMove)
        {
            this._logger.Info("MoveAbsolute(List<ConveyorAxesMoveParameters> axesToMove)", 2035, nameof(MoveAbsolute),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2038, nameof(MoveAbsolute),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0) {
                if (axesToMove == null)
                    this._logger.Info("axesToMove = null", 2045, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                else
                    this._logger.Info("axesToMove.Count = 0", 2047, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            List<Task> taskList = new List<Task>();
            foreach (ConveyorAxesMoveParameters axesMoveParameters in axesToMove) {
                ConveyorAxesMoveParameters axisToMove = axesMoveParameters;
                taskList.Add(Task.Run((Action) (() => this.MoveAbsolute(axisToMove.Axis, axisToMove.TargetPos,
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
            this._logger.Info(
                string.Format("MoveAbsolute(axis = {0},targetPos= {1}, vel= {2}, acc= {3}, dec= {4})", (object) axis,
                    (object) targetPos, (object) vel, (object) acc, (object) dec), 2076, nameof(MoveAbsolute),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2079, nameof(MoveAbsolute),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].MoveAbsolute(targetPos, true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool MoveRelative(List<ConveyorAxesMoveParameters> axesToMove)
        {
            this._logger.Info("MoveRelative(List<AxisMoveParameters> axesToMove)", 2102, nameof(MoveRelative),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2105, nameof(MoveRelative),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (axesToMove == null || axesToMove.Count == 0)
                return false;
            List<Task> taskList = new List<Task>();
            foreach (ConveyorAxesMoveParameters axesMoveParameters in axesToMove) {
                ConveyorAxesMoveParameters axisToMove = axesMoveParameters;
                taskList.Add(Task.Run((Action) (() => this.MoveRelative(axisToMove.Axis, axisToMove.TargetPos,
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
            this._logger.Info(
                string.Format("MoveRelative(axis = {0},relativePosition= {1}, vel= {2}, acc= {3}, dec= {4})",
                    (object) axis, (object) relativePosition, (object) vel, (object) acc, (object) dec), 2137,
                nameof(MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2140, nameof(MoveRelative),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].MoveRelative(true, relativePosition, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool Jog(ConveyorAxes axis, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            this._logger.Info(
                string.Format("Jog(axis = {0}, vel= {1}, acc= {2}, dec= {3})", (object) axis, (object) vel,
                    (object) acc, (object) dec), 2160, nameof(Jog),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2163, nameof(Jog),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Jog(true, vel, acc, dec);
            throw new ArgumentException("Axis not exist ");
        }

        public bool StopAllConveyorAxes()
        {
            this._logger.Info("StopAll()", 2179, nameof(StopAllConveyorAxes),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2182, nameof(StopAllConveyorAxes),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            for (AcsBuffers acsBuffers = AcsBuffers.ConveyorHoming;
                acsBuffers <= AcsBuffers.InternalErrorExit;
                ++acsBuffers)
                this.acsUtils.StopBuffer((ProgramBuffer) acsBuffers);
            this.acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_55);
            this.acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_56);
            this.acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_57);
            foreach (KeyValuePair<ConveyorAxes, ACSAxis> keyValuePair in this.conveyorAxesCache)
                keyValuePair.Value.Stop();
            return true;
        }

        public bool Stop(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Stop(axis={0})", (object) axis), 2210, nameof(Stop),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2213, nameof(Stop),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Stop();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public bool Abort(ConveyorAxes axis)
        {
            this._logger.Info(string.Format("Abort(axis={0})", (object) axis), 2231, nameof(Abort),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2234, nameof(Abort),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
                return false;
            }

            this.acsUtils.StopBuffer(ProgramBuffer.ACSC_BUFFER_9);
            if (this.conveyorAxesCache.ContainsKey(axis))
                return this.conveyorAxesCache[axis].Abort();
            else
                throw new ArgumentException("Axis not exist ");
        }

        public void SetRPos(ConveyorAxes axis, double pos)
        {
            this._logger.Info(string.Format("SetRPos(axis={0},pos={1})", (object) axis, (object) pos), 2254,
                nameof(SetRPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            if (!this.IsConnected) {
                this._logger.Info("Controller not connected", 2257, nameof(SetRPos),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            }
            else {
                if (this.conveyorAxesCache.ContainsKey(axis))
                    this.conveyorAxesCache[axis].SetRPos(pos);
                throw new ArgumentException("Axis not exist ");
            }
        }

        private void scanLoop()
        {
            IscanLoopRun = true;
            while (IscanLoopRun) {
                lock (lockObject) {
                    try {
                        IsConnected = Ch.IsConnected;
                    }
                    catch (Exception ex1) {
                        var acsException1 = ex1 as ACSException;
                        if (acsException1 != null)
                            _logger.Info(acsException1.Message, 1535, nameof(scanLoop),
                                "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                        try {
                            IsConnected = Ch.IsConnected;
                        }
                        catch (Exception ex2) {
                            var acsException = ex2 as ACSException;
                            if (acsException != null)
                                _logger.Info(acsException.Message, 1544, nameof(scanLoop),
                                    "C:\\Users\\Garry.han\\CyberOptics Gantry\\2nd edit\\ExternalHardware\\AcsWrapper\\AcsWrapper.cs");
                            IsConnected = false;
                        }
                    }

                    if (!IsConnected)
                        continue;
                }

                if (isScanningBufferRun) {
                    CurrentScanningIndex =
                        Convert.ToInt32(acsUtils.ReadVar("CURRENT_STEP_INDEX", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentMotionCompleteRecvd =
                        Convert.ToInt32(
                            acsUtils.ReadVar("MOVE_MOTION_COMPLETE_RECVD", ProgramBuffer.ACSC_BUFFER_9));
                    CurrentMovePSXAckRecvd =
                        Convert.ToInt32(acsUtils.ReadVar("MOVE_PSX_ACK_RECVD", ProgramBuffer.ACSC_BUFFER_9));
                    if (!acsUtils.IsProgramRunning(ProgramBuffer.ACSC_BUFFER_9)) {
                        isScanningBufferRun = false;
                        Action scanningEnd = ScanningEnd;
                        if (scanningEnd != null)
                            scanningEnd();
                        foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in axesCache)
                            keyValuePair.Value.ScanningBufferRun = false;
                    }
                }

                foreach (KeyValuePair<GantryAxes, ACSAxis> keyValuePair in this.axesCache)
                    keyValuePair.Value.getDataFromController();
                foreach (KeyValuePair<ConveyorAxes, ACSAxis> keyValuePair in this.conveyorAxesCache)
                    keyValuePair.Value.getDataFromController();
                Thread.Sleep(this.sleepPoling);
            }

            waiteExitFromPoling.Set();
        }

        private void EnableAcsEvents()
        {
            if (Ch == null)
                return;
            Ch.EnableEvent(Interrupts.ACSC_INTR_EMERGENCY);
            Ch.EnableEvent(Interrupts.ACSC_INTR_ETHERCAT_ERROR);
            Ch.EnableEvent(Interrupts.ACSC_INTR_MESSAGE);
            Ch.EnableEvent(Interrupts.ACSC_INTR_MOTION_FAILURE);
            Ch.EnableEvent(Interrupts.ACSC_INTR_MOTOR_FAILURE);
            Ch.EnableEvent(Interrupts.ACSC_INTR_SYSTEM_ERROR);
            Ch.EnableEvent(Interrupts.ACSC_INTR_COMMAND);
            Ch.EMERGENCY += new Api.DefaultAxisEventHandler(Ch_EMERGENCY);
            Ch.SYSTEMERROR += new Api.DefaultAxisEventHandler(Ch_SYSTEMERROR);
            Ch.MOTORFAILURE += new Api.AxisEventHandler(Ch_MOTORFAILURE);
            Ch.MOTIONFAILURE += new Api.AxisEventHandler(Ch_MOTIONFAILURE);
            Ch.ETHERCATERROR += new Api.DefaultAxisEventHandler(Ch_ETHERCATERROR);
            Ch.MESSAGE += new Api.DefaultAxisEventHandler(Ch_MESSAGE);
            Ch.ACSPLPROGRAMEX += new Api.DefaultAxisEventHandler(Ch_ACSPLPROGRAMEX);
        }

        private void Ch_MESSAGE(ulong Param)
        {
        }

        private void Ch_ETHERCATERROR(ulong Param)
        {
        }

        private void Ch_MOTIONFAILURE(AxisMasks Param)
        {
        }

        private void Ch_MOTORFAILURE(AxisMasks Param)
        {
        }

        private void Ch_SYSTEMERROR(ulong Param)
        {
            int num = this.Ch.GetLastError() + 1;
            try {
                this.Ch.GetErrorString(this.Ch.GetLastError());
            }
            catch (ACSException ex) {
            }
        }

        private void Ch_EMERGENCY(ulong Param)
        {
        }

        private void Ch_ACSPLPROGRAMEX(ulong Param)
        {
        }

        private void initAxesCache()
        {
            this.axesCache.Clear();
            for (GantryAxes gantryAxes = GantryAxes.Z; gantryAxes < GantryAxes.All; ++gantryAxes) {
                ACSAxis acsAxis = new ACSAxis(this.Ch, this.acsUtils, gantryAxes, this.GetAcsAxisIndex(gantryAxes),
                    this._robotSettings, this.isSimulation);
                this.axesCache[gantryAxes] = acsAxis;
                acsAxis.IdleChanged += new Action<int, bool>(this.axisIdleChanged);
                acsAxis.EnabledChanged += new Action<int, bool>(this.axisEnabledChanged);
                acsAxis.ReadyChanged += new Action<int, bool>(this.axisReadyChanged);
                acsAxis.PositionUpdated += new Action<int, double>(this.axisPositionUpdated);
                acsAxis.VelocityUpdated += new Action<int, double>(this.axisVelocityUpdated);
                acsAxis.MovementBegin += new Action<int>(this.axisMovementBegin);
                acsAxis.MovementEnd += new Action<int, bool>(this.axisMovementEnd);
                acsAxis.StopDone += new Action<int, bool>(this.axisStopDone);
                acsAxis.AbortDone += new Action<int, bool>(this.axisAbortDone);
                acsAxis.AtHomeSensorChanged += new Action<int, bool>(this.axisAtHomeSensorChanged);
                acsAxis.AtPositiveHWLimitChanged += new Action<int, bool>(this.axisAtPositiveHWLimitChanged);
                acsAxis.AtNegativeHWLimitChanged += new Action<int, bool>(this.axisAtNegativeHWLimitChanged);
                acsAxis.AtPositiveSWLimitChanged += new Action<int, bool>(this.axisAtPositiveSWLimitChanged);
                acsAxis.AtNegativeSWLimitChanged += new Action<int, bool>(this.axisAtNegativeSWLimitChanged);
                acsAxis.AxisHomingBegin += new Action<int>(this.Axis_AxisHomingBegin);
                acsAxis.AxisHomingEnd += new Action<int, bool>(this.Axis_AxisHomingEnd);
                if (acsAxis.AcsAxisId >= Axis.ACSC_AXIS_0)
                    this.Ch.Halt(acsAxis.AcsAxisId);
            }
        }

        private void initConveyorAxesCache()
        {
            conveyorAxesCache.Clear();
            for (ConveyorAxes conveyorAxes = ConveyorAxes.Conveyor;
                conveyorAxes <= ConveyorAxes.Lifter;
                ++conveyorAxes) {
                ACSAxis acsAxis = new ACSAxis(this.Ch, this.acsUtils, conveyorAxes, this.GetAcsAxisIndex(conveyorAxes),
                    this.isSimulation);
                this.conveyorAxesCache[conveyorAxes] = acsAxis;
                acsAxis.IdleChanged += new Action<int, bool>(this.conveyorAxisIdleChanged);
                acsAxis.EnabledChanged += new Action<int, bool>(this.conveyorAxisEnabledChanged);
                acsAxis.ReadyChanged += new Action<int, bool>(this.conveyorAxisReadyChanged);
                acsAxis.PositionUpdated += new Action<int, double>(this.conveyorAxisPositionUpdated);
                acsAxis.VelocityUpdated += new Action<int, double>(this.conveyorAxisVelocityUpdated);
                acsAxis.MovementBegin += new Action<int>(this.conveyorAxisMovementBegin);
                acsAxis.MovementEnd += new Action<int, bool>(this.conveyorAxisMovementEnd);
                acsAxis.StopDone += new Action<int, bool>(this.conveyorAxisStopDone);
                acsAxis.AbortDone += new Action<int, bool>(this.conveyorAxisAbortDone);
                acsAxis.AtHomeSensorChanged += new Action<int, bool>(this.conveyorAxisAtHomeSensorChanged);
                acsAxis.AtPositiveHWLimitChanged += new Action<int, bool>(this.conveyorAxisAtPositiveHWLimitChanged);
                acsAxis.AtNegativeHWLimitChanged += new Action<int, bool>(this.conveyorAxisAtNegativeHWLimitChanged);
                acsAxis.AtPositiveSWLimitChanged += new Action<int, bool>(this.conveyorAxisAtPositiveSWLimitChanged);
                acsAxis.AtNegativeSWLimitChanged += new Action<int, bool>(this.conveyorAxisAtNegativeSWLimitChanged);
                acsAxis.AxisHomingBegin += new Action<int>(this.conveyorAxis_AxisHomingBegin);
                acsAxis.AxisHomingEnd += new Action<int, bool>(this.conveyorAxis_AxisHomingEnd);
                if (acsAxis.AcsAxisId >= Axis.ACSC_AXIS_0)
                    this.Ch.Halt(acsAxis.AcsAxisId);
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

        private void initAxisNumbersAtController()
        {
            this.acsUtils.WriteVariable((object) this.axesCache[GantryAxes.X].AcsAxisId, "X_AXIS", From1: 0, To1: 0);
            this.acsUtils.WriteVariable((object) this.axesCache[GantryAxes.Y].AcsAxisId, "Y_AXIS", From1: 0, To1: 0);
            this.acsUtils.WriteVariable((object) this.axesCache[GantryAxes.Z].AcsAxisId, "Z_AXIS", From1: 0, To1: 0);
            this.acsUtils.WriteVariable((object) this.conveyorAxesCache[ConveyorAxes.Conveyor].AcsAxisId,
                "CONVEYOR_AXIS", From1: 0, To1: 0);
            this.acsUtils.WriteVariable((object) this.conveyorAxesCache[ConveyorAxes.Width].AcsAxisId,
                "CONVEYOR_WIDTH_AXIS", From1: 0, To1: 0);
            this.acsUtils.WriteVariable((object) this.conveyorAxesCache[ConveyorAxes.Lifter].AcsAxisId, "LIFTER_AXIS",
                From1: 0, To1: 0);
        }

        public void ReadAxesSettignsFromConfig()
        {
        }

        private void enableAllBlocking()
        {
        }

        private void initBuffers()
        {
            bufferHelper.StopAllBuffers();
            createCommutationBuffer();
            bufferHelper.InitGantryHomingBuffers();
            bufferHelper.InitIoBuffer();

            bufferHelper.InitConveyorBuffers();
            bufferHelper.InitConveyorHomingBuffers();
            bufferHelper.InitConveyorResetBuffers();

            bufferHelper.FlashAllBuffers();
        }

        private void createCommutationBuffer()
        {
            if (!isSimulation)
                ;
        }

        private bool teminateOldConnections()
        {
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            try {
                ACSC_CONNECTION_DESC[] connectionsList = Ch.GetConnectionsList();
                for (int index = 0; index < connectionsList.Length; ++index) {
                    if (connectionsList[index].Application.Contains(fileName))
                        Ch.TerminateConnection(connectionsList[index]);
                }
            }
            catch (Exception ex) {
                return false;
            }

            return true;
        }

        private void axisIdleChanged(int axis, bool isIdle)
        {
            this._logger.Info(string.Format("axisIdleChanged {0} {1}", (object) (GantryAxes) axis, (object) isIdle),
                3053, nameof(axisIdleChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> idleChanged = this.IdleChanged;
            if (idleChanged == null)
                return;
            idleChanged((GantryAxes) axis, isIdle);
        }

        private void axisEnabledChanged(int axis, bool isEnabled)
        {
            this._logger.Info(
                string.Format("axisEnabledChanged {0} {1}", (object) (GantryAxes) axis, (object) isEnabled), 3058,
                nameof(axisEnabledChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> enabledChanged = this.EnabledChanged;
            if (enabledChanged == null)
                return;
            enabledChanged((GantryAxes) axis, isEnabled);
        }

        private void axisReadyChanged(int axis, bool isReady)
        {
            this._logger.Info(string.Format("axisReadyChanged {0} {1}", (object) (GantryAxes) axis, (object) isReady),
                3063, nameof(axisReadyChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> readyChanged = this.ReadyChanged;
            if (readyChanged == null)
                return;
            readyChanged((GantryAxes) axis, isReady);
        }

        private void axisPositionUpdated(int axis, double pos)
        {
            this._logger.Info(string.Format("axisPositionUpdated {0} {1}", (object) (GantryAxes) axis, (object) pos),
                3068, nameof(axisPositionUpdated),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, double> positionUpdated = this.PositionUpdated;
            if (positionUpdated == null)
                return;
            positionUpdated((GantryAxes) axis, pos);
        }

        private void axisVelocityUpdated(int axis, double vel)
        {
            this._logger.Info(string.Format("axisVelocityUpdated {0} {1}", (object) (GantryAxes) axis, (object) vel),
                3074, nameof(axisVelocityUpdated),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, double> velocityUpdated = this.VelocityUpdated;
            if (velocityUpdated == null)
                return;
            velocityUpdated((GantryAxes) axis, vel);
        }

        private void axisMovementBegin(int axis)
        {
            this._logger.Info(string.Format("Axis_MovementBegin {0} ", (object) (GantryAxes) axis), 3079,
                nameof(axisMovementBegin),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes> movementBegin = this.MovementBegin;
            if (movementBegin == null)
                return;
            movementBegin((GantryAxes) axis);
        }

        private void axisMovementEnd(int axis, bool res)
        {
            this._logger.Info(string.Format("axisMovementEnd {0} {1}", (object) (GantryAxes) axis, (object) res), 3084,
                nameof(axisMovementEnd),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> movementEnd = this.MovementEnd;
            if (movementEnd == null)
                return;
            movementEnd((GantryAxes) axis, res);
        }

        private void axisStopDone(int axis, bool res)
        {
            this._logger.Info(string.Format("axisStopDone {0} {1}", (object) (GantryAxes) axis, (object) res), 3089,
                nameof(axisStopDone),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> stopDone = this.StopDone;
            if (stopDone == null)
                return;
            stopDone((GantryAxes) axis, res);
        }

        private void axisAbortDone(int axis, bool res)
        {
            this._logger.Info(string.Format("axisAbortDone {0} {1}", (object) (GantryAxes) axis, (object) res), 3094,
                nameof(axisAbortDone),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> abortDone = this.AbortDone;
            if (abortDone == null)
                return;
            abortDone((GantryAxes) axis, res);
        }

        private void axisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            this._logger.Info(
                string.Format("axisAtHomeSensorChanged {0} {1}", (object) (GantryAxes) axis, (object) isAtHomeSensor),
                3099, nameof(axisAtHomeSensorChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> homeSensorChanged = this.AtHomeSensorChanged;
            if (homeSensorChanged == null)
                return;
            homeSensorChanged((GantryAxes) axis, isAtHomeSensor);
        }

        private void axisAtPositiveHWLimitChanged(int axis, bool isAtPositiveHWLimit)
        {
            this._logger.Info(
                string.Format("axisAtPositiveHWLimitChanged {0} {1}", (object) (GantryAxes) axis,
                    (object) isAtPositiveHWLimit), 3104, nameof(axisAtPositiveHWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> positiveHwLimitChanged = this.AtPositiveHWLimitChanged;
            if (positiveHwLimitChanged == null)
                return;
            positiveHwLimitChanged((GantryAxes) axis, isAtPositiveHWLimit);
        }

        private void axisAtNegativeHWLimitChanged(int axis, bool isAtNegativeHWLimit)
        {
            this._logger.Info(
                string.Format("axisAtNegativeHWLimitChanged {0} {1}", (object) (GantryAxes) axis,
                    (object) isAtNegativeHWLimit), 3109, nameof(axisAtNegativeHWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> negativeHwLimitChanged = this.AtNegativeHWLimitChanged;
            if (negativeHwLimitChanged == null)
                return;
            negativeHwLimitChanged((GantryAxes) axis, isAtNegativeHWLimit);
        }

        private void axisAtPositiveSWLimitChanged(int axis, bool isAtPositiveSWLimit)
        {
            this._logger.Info(
                string.Format("axisAtPositiveSWLimitChanged {0} {1}", (object) (GantryAxes) axis,
                    (object) isAtPositiveSWLimit), 3114, nameof(axisAtPositiveSWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> positiveSwLimitChanged = this.AtPositiveSWLimitChanged;
            if (positiveSwLimitChanged == null)
                return;
            positiveSwLimitChanged((GantryAxes) axis, isAtPositiveSWLimit);
        }

        private void axisAtNegativeSWLimitChanged(int axis, bool isAtNegativeSWLimit)
        {
            this._logger.Info(
                string.Format("axisAtNegativeSWLimitChanged {0} {1}", (object) (GantryAxes) axis,
                    (object) isAtNegativeSWLimit), 3119, nameof(axisAtNegativeSWLimitChanged),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> negativeSwLimitChanged = this.AtNegativeSWLimitChanged;
            if (negativeSwLimitChanged == null)
                return;
            negativeSwLimitChanged((GantryAxes) axis, isAtNegativeSWLimit);
        }

        private void Axis_AxisHomingBegin(int axis)
        {
            this._logger.Info(string.Format("Axis_AxisHomingBegin {0} ", (object) (GantryAxes) axis), 3125,
                nameof(Axis_AxisHomingBegin),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes> axisHomingBegin = this.AxisHomingBegin;
            if (axisHomingBegin == null)
                return;
            axisHomingBegin((GantryAxes) axis);
        }

        private void Axis_AxisHomingEnd(int axis, bool res)
        {
            this._logger.Info(string.Format("Axis_AxisHomingEnd {0} {1}", (object) (GantryAxes) axis, (object) res),
                3130, nameof(Axis_AxisHomingEnd),
                "C:\\Users\\Garry\\Desktop\\ExternalHardware - 19032021\\AcsWrapper\\AcsWrapper.cs");
            Action<GantryAxes, bool> axisHomingEnd = this.AxisHomingEnd;
            if (axisHomingEnd == null)
                return;
            axisHomingEnd((GantryAxes) axis, res);
        }

        private void conveyorAxisIdleChanged(int axis, bool isIdle)
        {
            this._logger.Info(string.Format("axisIdleChanged {0} {1}", (object) (ConveyorAxes) axis, (object) isIdle));
            Action<ConveyorAxes, bool> conveyorAxisIdleChanged = this.ConveyorAxisIdleChanged;
            if (conveyorAxisIdleChanged == null)
                return;
            conveyorAxisIdleChanged((ConveyorAxes) axis, isIdle);
        }

        private void conveyorAxisEnabledChanged(int axis, bool isEnabled)
        {
            this._logger.Info(
                string.Format("axisEnabledChanged {0} {1}", (object) (ConveyorAxes) axis, (object) isEnabled), 3146,
                nameof(conveyorAxisEnabledChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> axisEnabledChanged = this.ConveyorAxisEnabledChanged;
            if (axisEnabledChanged == null)
                return;
            axisEnabledChanged((ConveyorAxes) axis, isEnabled);
        }

        private void conveyorAxisReadyChanged(int axis, bool isReady)
        {
            this._logger.Info(string.Format("axisReadyChanged {0} {1}", (object) (ConveyorAxes) axis, (object) isReady),
                3151, nameof(conveyorAxisReadyChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> axisReadyChanged = this.ConveyorAxisReadyChanged;
            if (axisReadyChanged == null)
                return;
            axisReadyChanged((ConveyorAxes) axis, isReady);
        }

        private void conveyorAxisPositionUpdated(int axis, double pos)
        {
            this._logger.Info(string.Format("axisPositionUpdated {0} {1}", (object) (ConveyorAxes) axis, (object) pos),
                3156, nameof(conveyorAxisPositionUpdated),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, double> axisPositionUpdated = this.ConveyorAxisPositionUpdated;
            if (axisPositionUpdated != null)
                axisPositionUpdated((ConveyorAxes) axis, pos);
        }

        private void conveyorAxisVelocityUpdated(int axis, double vel)
        {
            this._logger.Info(string.Format("axisVelocityUpdated {0} {1}", (object) (ConveyorAxes) axis, (object) vel),
                3166, nameof(conveyorAxisVelocityUpdated),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, double> axisVelocityUpdated = this.ConveyorAxisVelocityUpdated;
            if (axisVelocityUpdated == null)
                return;
            axisVelocityUpdated((ConveyorAxes) axis, vel);
        }

        private void conveyorAxisMovementBegin(int axis)
        {
            this._logger.Info(string.Format("Axis_MovementBegin {0} ", (object) (ConveyorAxes) axis), 3171,
                nameof(conveyorAxisMovementBegin),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes> axisMovementBegin = this.ConveyorAxisMovementBegin;
            if (axisMovementBegin == null)
                return;
            axisMovementBegin((ConveyorAxes) axis);
        }

        private void conveyorAxisMovementEnd(int axis, bool res)
        {
            this._logger.Info(string.Format("axisMovementEnd {0} {1}", (object) (ConveyorAxes) axis, (object) res));
            Action<ConveyorAxes, bool> conveyorAxisMovementEnd = this.ConveyorAxisMovementEnd;
            if (conveyorAxisMovementEnd == null)
                return;
            conveyorAxisMovementEnd((ConveyorAxes) axis, res);
        }

        private void conveyorAxisStopDone(int axis, bool res)
        {
            this._logger.Info(string.Format("axisStopDone {0} {1}", (object) (ConveyorAxes) axis, (object) res));
            Action<ConveyorAxes, bool> conveyorAxisStopDone = this.ConveyorAxisStopDone;
            if (conveyorAxisStopDone == null)
                return;
            conveyorAxisStopDone((ConveyorAxes) axis, res);
        }

        private void conveyorAxisAbortDone(int axis, bool res)
        {
            this._logger.Info(string.Format("axisAbortDone {0} {1}", (object) (ConveyorAxes) axis, (object) res));
            Action<ConveyorAxes, bool> conveyorAxisAbortDone = this.ConveyorAxisAbortDone;
            if (conveyorAxisAbortDone == null)
                return;
            conveyorAxisAbortDone((ConveyorAxes) axis, res);
        }

        private void conveyorAxisAtHomeSensorChanged(int axis, bool isAtHomeSensor)
        {
            this._logger.Info(
                string.Format("axisAtHomeSensorChanged {0} {1}", (object) (ConveyorAxes) axis, (object) isAtHomeSensor),
                3191, nameof(conveyorAxisAtHomeSensorChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> homeSensorChanged = this.ConveyorAxisAtHomeSensorChanged;
            if (homeSensorChanged == null)
                return;
            homeSensorChanged((ConveyorAxes) axis, isAtHomeSensor);
        }

        private void conveyorAxisAtPositiveHWLimitChanged(int axis, bool isAtPositiveHWLimit)
        {
            this._logger.Info(
                string.Format("axisAtPositiveHWLimitChanged {0} {1}", (object) (ConveyorAxes) axis,
                    (object) isAtPositiveHWLimit), 3196, nameof(conveyorAxisAtPositiveHWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> positiveHwLimitChanged = this.ConveyorAxisAtPositiveHWLimitChanged;
            if (positiveHwLimitChanged == null)
                return;
            positiveHwLimitChanged((ConveyorAxes) axis, isAtPositiveHWLimit);
        }

        private void conveyorAxisAtNegativeHWLimitChanged(int axis, bool isAtNegativeHWLimit)
        {
            this._logger.Info(
                string.Format("axisAtNegativeHWLimitChanged {0} {1}", (object) (ConveyorAxes) axis,
                    (object) isAtNegativeHWLimit), 3201, nameof(conveyorAxisAtNegativeHWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> negativeHwLimitChanged = this.ConveyorAxisAtNegativeHWLimitChanged;
            if (negativeHwLimitChanged == null)
                return;
            negativeHwLimitChanged((ConveyorAxes) axis, isAtNegativeHWLimit);
        }

        private void conveyorAxisAtPositiveSWLimitChanged(int axis, bool isAtPositiveSWLimit)
        {
            this._logger.Info(
                string.Format("axisAtPositiveSWLimitChanged {0} {1}", (object) (ConveyorAxes) axis,
                    (object) isAtPositiveSWLimit), 3206, nameof(conveyorAxisAtPositiveSWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> positiveSwLimitChanged = this.ConveyorAxisAtPositiveSWLimitChanged;
            if (positiveSwLimitChanged == null)
                return;
            positiveSwLimitChanged((ConveyorAxes) axis, isAtPositiveSWLimit);
        }

        private void conveyorAxisAtNegativeSWLimitChanged(int axis, bool isAtNegativeSWLimit)
        {
            this._logger.Info(
                string.Format("axisAtNegativeSWLimitChanged {0} {1}", (object) (ConveyorAxes) axis,
                    (object) isAtNegativeSWLimit), 3211, nameof(conveyorAxisAtNegativeSWLimitChanged),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> negativeSwLimitChanged = this.ConveyorAxisAtNegativeSWLimitChanged;
            if (negativeSwLimitChanged == null)
                return;
            negativeSwLimitChanged((ConveyorAxes) axis, isAtNegativeSWLimit);
        }

        private void conveyorAxis_AxisHomingBegin(int axis)
        {
            this._logger.Info(string.Format("Axis_AxisHomingBegin {0} ", (object) (ConveyorAxes) axis), 3217,
                nameof(conveyorAxis_AxisHomingBegin),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes> conveyorAxisHomingBegin = this.ConveyorAxisHomingBegin;
            if (conveyorAxisHomingBegin == null)
                return;
            conveyorAxisHomingBegin((ConveyorAxes) axis);
        }

        private void conveyorAxis_AxisHomingEnd(int axis, bool res)
        {
            this._logger.Info(string.Format("Axis_AxisHomingEnd {0} {1}", (object) (ConveyorAxes) axis, (object) res),
                3222, nameof(conveyorAxis_AxisHomingEnd),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\AcsWrapper.cs");
            Action<ConveyorAxes, bool> conveyorAxisHomingEnd = this.ConveyorAxisHomingEnd;
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

        private int CurrentMotionCompleteRecvd
        {
            get { return currentMotionCompleteRecvd; }
            set
            {
                if (value == currentMotionCompleteRecvd)
                    return;
                currentMotionCompleteRecvd = value;
                if (currentMotionCompleteRecvd == 1) {
                    Action motionCompleteRecvd = HardwareNotifySingleMoveMotionCompleteRecvd;
                    if (motionCompleteRecvd != null)
                        motionCompleteRecvd();
                    acsUtils.WriteVariable(0, "MOVE_MOTION_COMPLETE_RECVD", 9);
                    currentMotionCompleteRecvd = 0;
                }
            }
        }

        private int CurrentMovePSXAckRecvd
        {
            get { return currentMovePSXAckRecvd; }
            set
            {
                if (value == currentMovePSXAckRecvd)
                    return;
                currentMovePSXAckRecvd = value;
                if (currentMovePSXAckRecvd == 1) {
                    Action singleMovePsxAckRecvd = HardwareNotifySingleMovePSXAckRecvd;
                    if (singleMovePsxAckRecvd != null)
                        singleMovePsxAckRecvd();
                    acsUtils.WriteVariable(0, "MOVE_PSX_ACK_RECVD", 9);
                    currentMovePSXAckRecvd = 0;
                }
            }
        }

        public bool HasError => HasConveyorError || HasRobotError;
        public bool HasConveyorError => ErrorCode != ConveyorErrorCode.Error_Safe;
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
            return new CurrentandErrorStatusfromACS()
            {
                CurrentStatus = Convert.ToInt16(this.acsUtils.ReadVar("CURRENT_STATUS"))
            }.CurrentStatus;
        }

        public int GetErrorCode()
        {
            return new CurrentandErrorStatusfromACS()
            {
                ErrorCode = Convert.ToInt16(this.acsUtils.ReadVar("ERROR_CODE"))
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

        public void StartPanelLoad(LoadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.LoadPanel);
            Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.LoadPanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelReload(ReloadPanelBufferParameters parameters, double panelLength, int timeout)
        {
            InitConveyorBufferParameters(parameters);
            WritePanelLength(panelLength);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.ReloadPanel);
            Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReloadPanel, timeout);

            UpdateConveyorStatus();
        }

        private void WritePanelLength(double panelLength)
        {
            acsUtils.WriteVariable(panelLength, "PanelLength");
        }

        public void StopPanelLoad()
        {
            acsUtils.StopBuffer((ProgramBuffer) AcsBuffers.LoadPanel);
        }

        public void StartPanelPreRelease(PreReleasePanelBufferParameters parameters, int timeout)
        {
            InitConveyorBufferParameters(parameters);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.PreReleasePanel);
            Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.PreReleasePanel, timeout);

            UpdateConveyorStatus();
        }

        public void StartPanelRelease(ReleasePanelBufferParameters parameters, int timeout)
        {
            InitConveyorBufferParameters(parameters);

            acsUtils.RunBuffer((ProgramBuffer) AcsBuffers.ReleasePanel);
            Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.ReleasePanel, timeout);

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
            Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.ChangeWidth, timeout);

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
                Ch.WaitProgramEnd((ProgramBuffer) AcsBuffers.PowerOnRecoverFromEmergencyStop, timeout);
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
            return new ClampSensors()
            {
                FrontClampUp = Convert.ToBoolean(acsUtils.ReadVar("StopperUnlocked_Bit")),
                RearClampUp = Convert.ToBoolean(acsUtils.ReadVar("FrontClampUp_Bit")),
                FrontClampDown = Convert.ToBoolean(acsUtils.ReadVar("RearClampDown_Bit")),
                RearClampDown = Convert.ToBoolean(acsUtils.ReadVar("FrontClampDown_Bit"))
            };
        }

        public PresentSensors GetPresentSensorsStatus()
        {
            return new PresentSensors()
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
            _logger.Info($"SetTowerLightRed flash state {state}");
            SetIndicatorState(state, "TowerLightRedFlashing_Bit", "TowerLightRed_Bit");
        }

        public void SetTowerLightYellow(AcsIndicatorState state)
        {
            _logger.Info($"SetTowerLightYellow flash state {state}");
            SetIndicatorState(state, "TowerLightYellowFlashing_Bit", "TowerLightYellow_Bit");
        }

        public void SetTowerLightGreen(AcsIndicatorState state)
        {
            _logger.Info($"SetTowerLightGreen flash state {state}");
            SetIndicatorState(state, "TowerLightGreenFlashing_Bit", "TowerLightGreen_Bit");
        }

        public void SetTowerLightBlue(AcsIndicatorState state)
        {
            _logger.Info($"SetTowerLightBlue flash state {state}");
            SetIndicatorState(state, "TowerLightBlueFlashing_Bit", "TowerLightBlue_Bit");
        }

        public void SetTowerLightBuzzer(AcsIndicatorState state)
        {
            _logger.Info($"SetTowerLightBuzzer flash state {state}");
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
            _logger.Info($"SetStartButtonIndicator state {state}");
            switch (state) {
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, "StartPushButtonLight_Bit");
                    break;
                case AcsIndicatorState.Flashing:
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, "StartPushButtonLight_Bit");
                    break;
            }
        }

        public void SetStopButtonIndicator(AcsIndicatorState state)
        {
            _logger.Info($"SetStopButtonIndicator state {state}");
            switch (state) {
                default:
                case AcsIndicatorState.Off:
                    acsUtils.WriteVariable(0, "StopPushButtonLight_Bit");
                    break;
                case AcsIndicatorState.Flashing:
                case AcsIndicatorState.On:
                    acsUtils.WriteVariable(1, "StopPushButtonLight_Bit");
                    break;
            }
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