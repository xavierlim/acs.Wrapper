using System;
using System.Threading;
using System.Threading.Tasks;
using ACS.SPiiPlusNET;
using CO.Common.Logger;
using CO.Systems.Services.Acs.AcsWrapper.util;
using CO.Systems.Services.Configuration.Settings;
using CO.Systems.Services.Robot.Interface;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    internal class ACSAxis
    {
        private readonly Api api;
        private readonly AcsUtils acsUtils;
        private readonly IRobotControlSetting axisConfig;
        private readonly ILogger logger = LoggersManager.SystemLogger;

        private const int ACS_MFLAGS_HOME = 8;
        private int WaitEnabledDisabledTimeout = 10000;

        private object locker = new object();
        private object moveLocker = new object();

        private bool initing;
        private bool moving;
        private bool stoping;
        private bool aborting;
        private bool isAcsSimulation;
        private bool idle = true;
        private bool enabled;
        private bool ready = true;
        private double position;
        private double currerntVelocity;
        private double currentAccel;
        private double currentDecel;
        private bool atHomeSensor;
        private bool atPositiveHWLimit;
        private bool atNegativeHWLimit;
        private bool atPositiveSWLimit;
        private bool atNegativeSWLimit;
        private double minPos;
        private double maxPos;
        private bool initingBufferRun;

        private ACSAxis()
        {
        }

        internal ACSAxis(
            Api api,
            AcsUtils utils,
            GantryAxes axisID,
            Axis acsAxisId,
            IRobotControlSetting config,
            bool isSimulation)
            : this()
        {
            this.api = api;
            acsUtils = utils;
            axisConfig = config;
            ApplicationAxisId = (int) axisID;
            AcsAxisId = acsAxisId;
            isAcsSimulation = isSimulation;
            Name = ApplicationAxisId.ToString();
            ReloadConfigParameters();
            HomeStopCondition = string.Format("FAULT({0}).#LL", (int) AcsAxisId);
            if (!Homed) {
                acsUtils.ClearBits("FDEF", (int) AcsAxisId, 64);
                acsUtils.ClearBits("FDEF", (int) AcsAxisId, 32);
            }

            acsUtils.SetBits("FDEF", (int) AcsAxisId, 3);
            if (Homed) {
                acsUtils.SetBits("FDEF", (int) AcsAxisId, 64);
                acsUtils.SetBits("FDEF", (int) AcsAxisId, 32);
            }

            if (isAcsSimulation)
                HomeStopCondition = HomeStopCondition.Replace("#LL", "#SLL").Replace("#RL", "#SRL");
            switch (ApplicationAxisId) {
                case 0:
                    if (isAcsSimulation) {
                        HomeBuffer = 57;
                        break;
                    }

                    HomeBuffer = 3;
                    break;
                case 1:
                    if (isAcsSimulation) {
                        HomeBuffer = 55;
                        break;
                    }

                    HomeBuffer = 0;
                    break;
                case 2:
                    if (isAcsSimulation) {
                        HomeBuffer = 56;
                        break;
                    }

                    HomeBuffer = 1;
                    break;
            }
        }

        internal ACSAxis(
            Api api,
            AcsUtils utils,
            ConveyorAxes axisID,
            Axis acsAxisId,
            bool isSimulation)
            : this()
        {
            this.api = api;
            acsUtils = utils;
            ApplicationAxisId = (int) axisID;
            AcsAxisId = acsAxisId;
            isAcsSimulation = isSimulation;
            Name = ApplicationAxisId.ToString();
            ReloadConfigParameters();
            HomeStopCondition = string.Format("FAULT({0}).#LL", (int) AcsAxisId);
            if (!Homed) {
                acsUtils.ClearBits("FDEF", (int) AcsAxisId, 64);
                acsUtils.ClearBits("FDEF", (int) AcsAxisId, 32);
            }

            acsUtils.SetBits("FDEF", (int) AcsAxisId, 3);
            if (Homed) {
                acsUtils.SetBits("FDEF", (int) AcsAxisId, 64);
                acsUtils.SetBits("FDEF", (int) AcsAxisId, 32);
            }

            if (isAcsSimulation)
                HomeStopCondition = HomeStopCondition.Replace("#LL", "#SLL").Replace("#RL", "#SRL");
            switch (ApplicationAxisId) {
                case 5:
                    HomeBuffer = 4;
                    break;
                case 6:
                    HomeBuffer = 5;
                    break;
                case 7:
                    HomeBuffer = 6;
                    break;
            }
        }

        public event Action<int, bool> IdleChanged;

        public event Action<int, bool> EnabledChanged;

        public event Action<int, bool> ReadyChanged;

        public event Action<int, double> PositionUpdated;

        public event Action<int, double> VelocityUpdated;

        public event Action<int, bool> StopDone;

        public event Action<int, bool> AbortDone;

        public event Action<int, bool> AtHomeSensorChanged;

        public event Action<int, bool> AtPositiveHWLimitChanged;

        public event Action<int, bool> AtNegativeHWLimitChanged;

        public event Action<int, bool> AtPositiveSWLimitChanged;

        public event Action<int, bool> AtNegativeSWLimitChanged;

        public event Action<int> MovementBegin;

        public event Action<int, bool> MovementEnd;

        public event Action<int> AxisHomingBegin;

        public event Action<int, bool> AxisHomingEnd;

        public int ApplicationAxisId { get; private set; }

        public bool Idle
        {
            get { return idle; }
            private set
            {
                if (idle == value)
                    return;
                idle = value;
                Action<int, bool> idleChanged = IdleChanged;
                if (idleChanged != null)
                    idleChanged(ApplicationAxisId, idle);
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            private set
            {
                if (enabled == value)
                    return;
                enabled = value;
                Action<int, bool> enabledChanged = EnabledChanged;
                if (enabledChanged != null)
                    enabledChanged(ApplicationAxisId, enabled);
            }
        }

        public bool Homed
        {
            get { return Convert.ToBoolean(acsUtils.ReadInt("MFLAGS", (int) AcsAxisId) & 8); }
        }

        public Axis AcsAxisId { get; private set; }

        public string Name { get; set; }

        public bool Ready
        {
            get { return ready; }
            private set
            {
                if (ready == value)
                    return;
                ready = value;
                Action<int, bool> readyChanged = ReadyChanged;
                if (readyChanged != null)
                    readyChanged(ApplicationAxisId, ready);
            }
        }

        public double Position
        {
            get { return position; }
            private set
            {
                if (position == value)
                    return;
                position = value;
                Action<int, double> positionUpdated = PositionUpdated;
                if (positionUpdated != null)
                    positionUpdated(ApplicationAxisId, position);
            }
        }

        public double DefaultVelocity { get; set; }

        public double CurrerntVelocity
        {
            get { return currerntVelocity; }
            private set
            {
                if (currerntVelocity == value)
                    return;
                currerntVelocity = value;
                Action<int, double> velocityUpdated = VelocityUpdated;
                if (velocityUpdated != null)
                    velocityUpdated(ApplicationAxisId, currerntVelocity);
            }
        }

        public double MaxVelocity { get; private set; }

        public double DefaultAccel { get; set; }

        public double DefaultDecel { get; set; }

        public double MaxAccel { get; private set; }

        public double MaxDecel { get; private set; }

        private double CurrentAccel
        {
            set
            {
                if (value == currentAccel)
                    return;
                currentAccel = value;
                api.SetAcceleration(AcsAxisId, currentAccel);
            }
        }

        private double CurrentDecel
        {
            set
            {
                if (value == currentDecel)
                    return;
                currentDecel = value;
                api.SetDeceleration(AcsAxisId, currentDecel);
            }
        }

        public double DefaultJerk { get; set; }

        public bool AtHomeSensor
        {
            get { return atHomeSensor; }
            private set
            {
                if (atHomeSensor == value)
                    return;
                atHomeSensor = value;
                Action<int, bool> homeSensorChanged = AtHomeSensorChanged;
                if (homeSensorChanged != null)
                    homeSensorChanged(ApplicationAxisId, atHomeSensor);
            }
        }

        public bool AtPositiveHWLimit
        {
            get { return atPositiveHWLimit; }
            private set
            {
                if (atPositiveHWLimit == value)
                    return;
                atPositiveHWLimit = value;
                Action<int, bool> positiveHwLimitChanged = AtPositiveHWLimitChanged;
                if (positiveHwLimitChanged != null)
                    positiveHwLimitChanged(ApplicationAxisId, atPositiveHWLimit);
            }
        }

        public bool AtNegativeHWLimit
        {
            get { return atNegativeHWLimit; }
            private set
            {
                if (atNegativeHWLimit == value)
                    return;
                atNegativeHWLimit = value;
                Action<int, bool> negativeHwLimitChanged = AtNegativeHWLimitChanged;
                if (negativeHwLimitChanged != null)
                    negativeHwLimitChanged(ApplicationAxisId, atNegativeHWLimit);
            }
        }

        public bool AtPositiveSWLimit
        {
            get { return atPositiveSWLimit; }
            private set
            {
                if (atPositiveSWLimit == value)
                    return;
                atPositiveSWLimit = value;
                Action<int, bool> positiveSwLimitChanged = AtPositiveSWLimitChanged;
                if (positiveSwLimitChanged != null)
                    positiveSwLimitChanged(ApplicationAxisId, atPositiveSWLimit);
            }
        }

        public bool AtNegativeSWLimit
        {
            get { return atNegativeSWLimit; }
            private set
            {
                if (atNegativeSWLimit == value)
                    return;
                atNegativeSWLimit = value;
                Action<int, bool> negativeSwLimitChanged = AtNegativeSWLimitChanged;
                if (negativeSwLimitChanged != null)
                    negativeSwLimitChanged(ApplicationAxisId, atNegativeSWLimit);
            }
        }

        public double MinPos
        {
            get { return minPos; }
            set
            {
                minPos = value;
                try {
                    if (minPos == double.MinValue) {
                        acsUtils.ClearBits("FMASK", (int) AcsAxisId, 64);
                        if (Homed)
                            return;
                        acsUtils.ClearBits("FDEF", (int) AcsAxisId, 64);
                    }
                    else {
                        acsUtils.SetBits("FMASK", (int) AcsAxisId, 64);
                        if (Homed)
                            acsUtils.SetBits("FDEF", (int) AcsAxisId, 64);
                        acsUtils.WriteVariable(minPos - 0.01, "SLLIMIT",
                            from1: ((int) AcsAxisId), to1: ((int) AcsAxisId));
                    }
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 494, nameof(MinPos),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                }
            }
        }

        public double MaxPos
        {
            get { return maxPos; }
            set
            {
                maxPos = value;
                try {
                    if (maxPos == double.MaxValue) {
                        acsUtils.ClearBits("FMASK", (int) AcsAxisId, 32);
                        if (Homed)
                            return;
                        acsUtils.ClearBits("FDEF", (int) AcsAxisId, 32);
                    }
                    else {
                        acsUtils.SetBits("FMASK", (int) AcsAxisId, 32);
                        if (Homed)
                            acsUtils.SetBits("FDEF", (int) AcsAxisId, 32);
                        acsUtils.WriteVariable(maxPos + 0.001, "SRLIMIT",
                            from1: ((int) AcsAxisId), to1: ((int) AcsAxisId));
                    }
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 526, nameof(MaxPos),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                }
            }
        }

        public double HomeVelIn
        {
            get { return acsUtils.ReadGlobalReal("HOME_VEL_IN", (int) AcsAxisId); }
            set
            {
                if (HomeVelIn == value)
                    return;
                try {
                    acsUtils.WriteGlobalReal(value, "HOME_VEL_IN", (int) AcsAxisId);
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 549, nameof(HomeVelIn),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                }
            }
        }

        public double HomeVelOut
        {
            get { return acsUtils.ReadGlobalReal("HOME_VEL_OUT", (int) AcsAxisId); }
            set
            {
                if (HomeVelOut == value)
                    return;
                try {
                    acsUtils.WriteGlobalReal(value, "HOME_VEL_OUT", (int) AcsAxisId);
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 571, nameof(HomeVelOut),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                }
            }
        }

        public double HomeOffset
        {
            get
            {
                try {
                    return acsUtils.ReadGlobalReal("HOME_OFFSET", (int) AcsAxisId);
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 587, nameof(HomeOffset),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return 0.0;
                }
            }
            set
            {
                if (HomeOffset == value)
                    return;
                try {
                    acsUtils.WriteGlobalReal(value, "HOME_OFFSET", (int) AcsAxisId);
                    acsUtils.ClearBits("MFLAGS", (int) AcsAxisId, 8);
                }
                catch (Exception ex) {
                    logger.Info(ex.Message, 602, nameof(HomeOffset),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                }
            }
        }

        public string HomeStopCondition { get; set; }

        public int HomeBuffer { get; private set; } = -1;

        public bool ScanningBufferRun { get; set; } = false;

        public void RestoreDefualtSettings()
        {
            ReloadConfigParameters();
        }

        public void ClearError()
        {
            if (!api.IsConnected)
                logger.Info("Controller not connected", 636, nameof(ClearError),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            else
                api.FaultClear(AcsAxisId);
        }

        public bool Enable()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected", 646, nameof(Enable),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                return false;
            }

            ClearError();
            api.Enable(AcsAxisId);
            api.WaitMotorEnabled(AcsAxisId, 1, WaitEnabledDisabledTimeout);
            return true;
        }

        public bool Disable()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected", 660, nameof(Disable),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                return false;
            }

            api.Disable(AcsAxisId);
            api.WaitMotorEnabled(AcsAxisId, 0, WaitEnabledDisabledTimeout);
            return true;
        }

        public bool Init(bool waitProgramEnd)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!InitPrepare())
                    return false;
                ReloadConfigParameters();
                InitImpl(waitProgramEnd);
                return true;
            }
        }

        public bool Init(AxisInitParameters initParameters, bool waitProgramEnd)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!InitPrepare())
                    return false;
                CurrentAccel = initParameters.Accel;
                CurrentDecel = initParameters.Decel;
                HomeVelIn = initParameters.FastVelocity;
                HomeVelOut = Math.Abs(initParameters.SlowVelocity);
                if (HomeVelIn < 0.0)
                    HomeVelOut *= -1.0;
                HomeOffset = initParameters.HomeOffset;
                InitImpl(waitProgramEnd);
                return true;
            }
        }

        public void UpdateScanningProfiles(AxisScanParameters parameters)
        {
            CurrentAccel = parameters.Acceleration;
            CurrentDecel = parameters.Deceleration;
        }

        public bool MoveAbsolute(
            double targetPos,
            bool waitProgramEnd,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!api.IsConnected) {
                    logger.Info("Controller not connected", 747, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy", 752, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                logger.Info(
                    string.Format("MoveAbsolute {0} to {1} position", Name, targetPos), 756,
                    nameof(MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                if (!Homed) {
                    logger.Info(Name + " need initialize", 760, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (!Enable()) {
                    logger.Info(Name + " failed to enable", 765, nameof(MoveAbsolute),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (vel == 0.0 || double.IsNaN(vel))
                    vel = DefaultVelocity;
                if (vel >= MaxVelocity * 0.99)
                    vel = MaxVelocity * 0.99;
                if (acc == 0.0 || double.IsNaN(acc))
                    acc = DefaultAccel;
                if (dec == 0.0 || double.IsNaN(dec))
                    dec = DefaultDecel;
                CurrentAccel = acc;
                CurrentDecel = dec;
                MoveAbsoluteImpl(targetPos, vel, waitProgramEnd);
                return true;
            }
        }

        public bool MoveRelative(
            bool waitProgramEnd,
            double relativePosition,
            double vel = 0.0,
            double acc = 0.0,
            double dec = 0.0)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!api.IsConnected) {
                    logger.Info("Controller not connected", 796, nameof(MoveRelative),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy", 801, nameof(MoveRelative),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                logger.Info(
                    string.Format("MoveRelative {0} to {1} position", Name, relativePosition),
                    805, nameof(MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                if (!Enable()) {
                    logger.Info(Name + " failed to enable", 809, nameof(MoveRelative),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (vel == 0.0 || double.IsNaN(vel))
                    vel = DefaultVelocity;
                if (vel >= MaxVelocity * 0.99)
                    vel = MaxVelocity * 0.99;
                if (acc == 0.0 || double.IsNaN(acc))
                    acc = DefaultAccel;
                if (dec == 0.0 || double.IsNaN(dec))
                    dec = DefaultDecel;
                CurrentAccel = acc;
                CurrentDecel = dec;
                MoveRelativeImpl(waitProgramEnd, relativePosition, vel);
                return true;
            }
        }

        public bool Jog(bool waitProgramEnd, double vel = 0.0, double acc = 0.0, double dec = 0.0)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!api.IsConnected) {
                    logger.Info("Controller not connected", 844, nameof(Jog),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy", 849, nameof(Jog),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                logger.Info("Jog " + Name + " ", 853, nameof(Jog),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                if (!Enable()) {
                    logger.Info(Name + " failed to enable", 857, nameof(Jog),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                    return false;
                }

                if (vel == 0.0 || double.IsNaN(vel))
                    vel = DefaultVelocity;
                if (Math.Abs(vel) >= MaxVelocity * 0.99)
                    vel = vel >= 0.0 ? MaxVelocity * 0.99 : MaxVelocity * 0.99 * -1.0;
                if (acc == 0.0 || double.IsNaN(acc))
                    acc = DefaultAccel;
                if (dec == 0.0 || double.IsNaN(dec))
                    dec = DefaultDecel;
                CurrentAccel = acc;
                CurrentDecel = dec;
                vel = Math.Abs(vel);
                JogImpl(waitProgramEnd, vel);
                return true;
            }
        }

        public bool Stop()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected", 892, nameof(Stop),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                return false;
            }

            lock (locker) {
                stoping = true;
                return haltOrKill(true);
            }
        }

        public bool Abort()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected", 911, nameof(Abort),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                return false;
            }

            lock (locker) {
                aborting = true;
                return haltOrKill(false);
            }
        }

        public void SetRPos(double pos)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected", 931, nameof(SetRPos),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            }
            else {
                logger.Info(string.Format("SetRPos {0} to {1} ", Name, pos), 935,
                    nameof(SetRPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                api.SetFPosition(AcsAxisId, pos);
            }
        }

        public void getDataFromController()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                acsUtils.ReadInt("MFLAGS", (int) AcsAxisId);
                SafetyControlMasks fault = api.GetFault(AcsAxisId);
                AtPositiveHWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_RL) > SafetyControlMasks.ACSC_NONE;
                AtNegativeHWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_LL) > SafetyControlMasks.ACSC_NONE;
                AtPositiveSWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SRL) > SafetyControlMasks.ACSC_NONE;
                AtNegativeSWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SLL) > SafetyControlMasks.ACSC_NONE;
                MotorStates motorState = api.GetMotorState(AcsAxisId);
                Enabled = (motorState & MotorStates.ACSC_MST_ENABLE) == MotorStates.ACSC_MST_ENABLE;
                bool flag = (motorState & MotorStates.ACSC_MST_MOVE) != MotorStates.ACSC_MST_MOVE &&
                            !ScanningBufferRun;
                if (initing) {
                    InitingBufferRun = acsUtils.IsProgramRunning((ProgramBuffer) HomeBuffer);
                    flag = flag && !InitingBufferRun;
                }

                Idle = flag;
                if (Idle && (moving || stoping || aborting))
                    motionEnded(true);
                Position = api.GetRPosition(AcsAxisId);
                CurrerntVelocity = Math.Round(api.GetRVelocity(AcsAxisId));
                Ready = Enabled && Homed;
            }
        }

        private bool InitingBufferRun
        {
            get { return initingBufferRun; }
            set
            {
                if (value == InitingBufferRun && (value || !initing))
                    return;
                initingBufferRun = value;
                if (!initingBufferRun)
                    motionEnded(Homed);
            }
        }

        private bool InitPrepare()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (!Idle) {
                logger.Info("Axis is busy");
                return false;
            }

            ClearError();
            logger.Info("Init Axis " + Name);
            if (!Enable() || !checkInitParams())
                return false;
            Idle = false;
            return true;
        }

        public void ReloadConfigParameters()
        {
            if (axisConfig == null) {
                LoadDefaultConfigParameters();
            }
            else {
                DefaultVelocity = axisConfig.DefaultVelocity[ApplicationAxisId] * 1000.0;
                DefaultAccel = axisConfig.DefaultAccel[ApplicationAxisId] * 1000.0;
                DefaultDecel = axisConfig.DefaultDecel[ApplicationAxisId] * 1000.0;
                MinPos = axisConfig.MinPositionLimit[ApplicationAxisId] * 1000L;
                MaxPos = axisConfig.MaxPositionLimit[ApplicationAxisId] * 1000L;
                HomeVelIn = axisConfig.HomeVelocity1[ApplicationAxisId] * 1000.0;
                HomeVelOut = axisConfig.HomeVelocity2[ApplicationAxisId] * 1000.0;
                HomeVelOut = Math.Abs(HomeVelOut);
                if (HomeVelIn < 0.0)
                    HomeVelOut *= -1.0;
                HomeOffset = axisConfig.HomeOffset[ApplicationAxisId] * 1000.0;
                HomeStopCondition = string.Format("FAULT({0}).#LL", (int) AcsAxisId);
                MaxAccel = axisConfig.MaxAccel[ApplicationAxisId] * 1000.0;
                MaxDecel = axisConfig.MaxDecel[ApplicationAxisId] * 1000.0;
                MaxVelocity = axisConfig.MaxVelocity[ApplicationAxisId] * 1000.0;
            }
        }

        private void LoadDefaultConfigParameters()
        {
            DefaultVelocity = 100.0;
            DefaultAccel = 7500.0;
            DefaultDecel = 7500.0;
            MaxVelocity = DefaultVelocity * 5.0;
            MaxAccel = DefaultAccel * 5.0;
            MaxDecel = DefaultDecel * 5.0;
            MinPos = -1000.0;
            MaxPos = 1000.0;
            HomeVelIn = 75.0;
            HomeVelOut = 10.0;
            HomeOffset = 28.0;
        }

        private bool checkInitParams()
        {
            return HomeVelIn != 0.0 && HomeVelOut != 0.0 && HomeBuffer != -1;
        }

        private void motionEnded(bool ok)
        {
            Position = api.GetRPosition(AcsAxisId);
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            lock (locker) {
                if (initing) {
                    initing = false;
                    flag2 = true;
                }

                if (moving) {
                    flag1 = true;
                    moving = false;
                }

                if (stoping) {
                    flag3 = true;
                    stoping = false;
                }

                if (aborting) {
                    flag4 = true;
                    aborting = false;
                }
            }

            if (flag2) {
                Action<int, bool> axisHomingEnd = AxisHomingEnd;
                if (axisHomingEnd != null)
                    axisHomingEnd(ApplicationAxisId, ok);
            }

            if (flag1) {
                Action<int, bool> movementEnd = MovementEnd;
                if (movementEnd != null)
                    movementEnd(ApplicationAxisId, ok);
            }

            if (flag3) {
                Action<int, bool> stopDone = StopDone;
                if (stopDone != null)
                    stopDone(ApplicationAxisId, ok);
            }

            if (!flag4)
                return;
            Action<int, bool> abortDone = AbortDone;
            if (abortDone == null)
                return;
            abortDone(ApplicationAxisId, ok);
        }

        private void motionEnded(int motorErr, int motionErr)
        {
            try {
                if (motionErr >= 5000 && motionErr <= 5006)
                    motionErr = 0;
                if (motorErr >= 5000 && motorErr <= 5006)
                    motorErr = 0;
                string message = string.Empty;
                if ((uint) motionErr > 0U)
                    message = Name + " " + api.GetErrorString(motionErr);
                if (motorErr != 0 && motionErr != motorErr)
                    message = Name + " " + api.GetErrorString(motorErr);
                if (!string.IsNullOrEmpty(message))
                    logger.Info(message, 1159, nameof(motionEnded),
                        "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            }
            catch (Exception ex) {
                logger.Info(ex.Message, 1163, nameof(motionEnded),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            }

            motionEnded(motionErr == 0 && motorErr == 0);
        }

        private bool haltOrKill(bool halt)
        {
            logger.Info(string.Format("haltOrKill {0} ", halt), 1169, nameof(haltOrKill),
                "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            try {
                api.StopBuffer((ProgramBuffer) HomeBuffer);
                if (halt)
                    api.Halt(AcsAxisId);
                else
                    api.Kill(AcsAxisId);
            }
            catch (Exception ex) {
                logger.Info(ex.Message + " " + Name, 1184, nameof(haltOrKill),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                return false;
            }

            return true;
        }

        private void InitImpl(bool waitProgramEnd)
        {
            bool ok = true;
            lock (locker)
                initing = true;
            Action<int> axisHomingBegin = AxisHomingBegin;
            if (axisHomingBegin != null)
                axisHomingBegin(ApplicationAxisId);
            try {
                acsUtils.RunBuffer((ProgramBuffer) HomeBuffer);
                if (waitProgramEnd)
                    api.WaitProgramEnd((ProgramBuffer) HomeBuffer,
                        (int) (100000.0 * Math.Max(Math.Abs((MaxPos - MinPos) / HomeVelIn), 1.0)));
            }
            catch (Exception ex) {
                logger.Info("failed to init axis " + Name + "  :  " + ex.Message, 1214, nameof(InitImpl),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                Stop();
                lock (locker)
                    initing = false;
                ok = false;
            }

            if (!waitProgramEnd)
                return;
            if (ok) {
                Thread.Sleep(100);
                ok = Homed;
            }

            motionEnded(ok);
        }

        private async void InitImplAsync(bool waitProgramEnd)
        {
            await Task.Run(() =>
            {
                lock (moveLocker)
                    InitImpl(waitProgramEnd);
            });
        }

        private void MoveAbsoluteImpl(double targetPos, double vel, bool waitProgramEnd)
        {
            moving = true;
            Action<int> movementBegin = MovementBegin;
            if (movementBegin != null)
                movementBegin(ApplicationAxisId);
            try {
                api.ExtToPoint(MotionFlags.ACSC_AMF_VELOCITY | MotionFlags.ACSC_AMF_ENDVELOCITY, AcsAxisId,
                    targetPos, vel, 0.0);
                if (!waitProgramEnd)
                    return;
                api.WaitMotionEnd(AcsAxisId,
                    (int) (100000.0 * Math.Max(Math.Abs((targetPos - Position) / vel), 1.0)));
                motionEnded(true);
            }
            catch (Exception ex) {
                motionEnded(false);
                logger.Info(
                    string.Format("MoveAbsolute {0} to {1} position exception {2}", Name,
                        targetPos, ex.Message), 1272, nameof(MoveAbsoluteImpl),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            }
        }

        private async void MoveAbsoluteImplAsync(double targetPos, double vel)
        {
            await Task.Run(() =>
            {
                lock (moveLocker)
                    MoveAbsoluteImpl(targetPos, vel, true);
            });
        }

        private async void MoveRelativeImplAsync(double relativePosition, double vel)
        {
            await Task.Run(() =>
            {
                lock (moveLocker)
                    MoveRelativeImpl(true, relativePosition, vel);
            });
        }

        private void MoveRelativeImpl(bool waitProgramEnd, double relativePosition, double vel)
        {
            lock (locker)
                moving = true;
            Action<int> movementBegin = MovementBegin;
            if (movementBegin != null)
                movementBegin(ApplicationAxisId);
            try {
                api.ExtToPoint(
                    MotionFlags.ACSC_AMF_RELATIVE | MotionFlags.ACSC_AMF_VELOCITY | MotionFlags.ACSC_AMF_ENDVELOCITY,
                    AcsAxisId, relativePosition, vel, 0.0);
                if (!waitProgramEnd)
                    return;
                api.WaitMotionEnd(AcsAxisId,
                    (int) (100000.0 * Math.Max(Math.Abs(relativePosition / vel), 1.0)));
                motionEnded(true);
            }
            catch (Exception ex) {
                motionEnded(false);
                logger.Info(
                    string.Format("MoveRelative {0} to {1} position exception {2}", Name,
                        relativePosition, ex.Message), 1324, nameof(MoveRelativeImpl),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            }
        }

        private async void JogImplAsync(RobotAxisDirection direction, double vel)
        {
            await Task.Run(() =>
            {
                lock (locker) {
                    moving = true;
                    Action<int> movementBegin = MovementBegin;
                    if (movementBegin != null)
                        movementBegin(ApplicationAxisId);
                    try {
                        api.Jog(MotionFlags.ACSC_AMF_VELOCITY, AcsAxisId, vel);
                        api.WaitMotionEnd(AcsAxisId, int.MaxValue);
                        motionEnded(true);
                    }
                    catch (Exception ex) {
                        logger.Info(
                            string.Format("Jog {0} to {1}  exception {2}", Name, direction,
                                ex.Message), 1345, nameof(JogImplAsync),
                            "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                        motionEnded(true);
                    }
                }
            });
        }

        private void JogImpl(bool waitProgramEnd, double vel)
        {
            lock (locker)
                moving = true;
            Action<int> movementBegin = MovementBegin;
            if (movementBegin != null)
                movementBegin(ApplicationAxisId);
            try {
                api.Jog(MotionFlags.ACSC_AMF_VELOCITY, AcsAxisId, vel);
                if (!waitProgramEnd)
                    return;
                api.WaitMotionEnd(AcsAxisId, int.MaxValue);
                motionEnded(true);
            }
            catch (Exception ex) {
                logger.Info("Jog " + Name + " exception " + ex.Message, 1373, nameof(JogImpl),
                    "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
                motionEnded(true);
            }
        }

        private bool IsLocked(object locker)
        {
            bool lockTaken = false;
            try {
                Monitor.TryEnter(locker, 0, ref lockTaken);
                if (!lockTaken)
                    return true;
            }
            finally {
                if (lockTaken)
                    Monitor.Exit(locker);
            }

            return false;
        }
    }
}