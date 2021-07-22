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
    internal class AcsAxis
    {
        private readonly Api api;
        private readonly AcsUtils acsUtils;
        private readonly BufferHelper bufferHelper;
        private readonly IRobotControlSetting axisConfig;
        private readonly ILogger logger = LoggersManager.SystemLogger;

        private const int WaitEnabledDisabledTimeout = 10000;

        private readonly object locker = new object();
        private readonly object moveLocker = new object();

        private bool initializing;
        private bool moving;
        private bool stopping;
        private bool aborting;
        private bool idle = true;
        private bool enabled;
        private bool ready = true;
        private double position;
        private double currentVelocity;
        private double currentAccel;
        private double currentDecel;
        private bool atHomeSensor;
        private bool atPositiveHwLimit;
        private bool atNegativeHwLimit;
        private bool atPositiveSwLimit;
        private bool atNegativeSwLimit;
        private double minPos;
        private double maxPos;
        private bool initializingBufferRun;

        internal AcsAxis(Api api, AcsUtils utils, BufferHelper bufferHelper, GantryAxes axisId, Axis acsAxisId,
            IRobotControlSetting config)
        {
            this.api = api;
            this.bufferHelper = bufferHelper;
            acsUtils = utils;
            axisConfig = config;
            ApplicationAxisId = (int) axisId;
            AcsAxisId = acsAxisId;
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

            switch (ApplicationAxisId) {
                case 0:
                    HomeBuffer = 3;
                    break;
                case 1:
                    HomeBuffer = 0;
                    break;
                case 2:
                    HomeBuffer = 1;
                    break;
            }
        }

        internal AcsAxis(Api api, AcsUtils utils, BufferHelper bufferHelper, ConveyorAxes axisId, Axis acsAxisId)
        {
            this.api = api;
            this.bufferHelper = bufferHelper;
            acsUtils = utils;
            ApplicationAxisId = (int) axisId;
            AcsAxisId = acsAxisId;
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

        public event Action<int, bool> AtPositiveHwLimitChanged;

        public event Action<int, bool> AtNegativeHwLimitChanged;

        public event Action<int, bool> AtPositiveSwLimitChanged;

        public event Action<int, bool> AtNegativeSwLimitChanged;

        public event Action<int> MovementBegin;

        public event Action<int, bool> MovementEnd;

        public event Action<int> AxisHomingBegin;

        public event Action<int, bool> AxisHomingEnd;

        public int ApplicationAxisId { get; }

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

        public bool Homed => Convert.ToBoolean(acsUtils.ReadInt("MFLAGS", (int) AcsAxisId) & 8);

        public Axis AcsAxisId { get; }

        public string Name { get; }

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
                if (Math.Abs(position - value) < 0.01) return;
                position = value;
                PositionUpdated?.Invoke(ApplicationAxisId, position);
            }
        }

        public double DefaultVelocity { get; set; }

        public double CurrentVelocity
        {
            get { return currentVelocity; }
            private set
            {
                if (currentVelocity == value)
                    return;
                currentVelocity = value;
                Action<int, double> velocityUpdated = VelocityUpdated;
                if (velocityUpdated != null)
                    velocityUpdated(ApplicationAxisId, currentVelocity);
            }
        }

        public double MaxVelocity { get; private set; }

        public double DefaultAccel { get; set; }

        public double DefaultDecel { get; set; }

        public double MaxAccel { get; private set; }

        public double MaxDecel { get; private set; }

        private double CurrentAccel
        {
            get { return currentAccel; }
            set
            {
                if (Math.Abs(value - currentAccel) < 0.01) return;
                currentAccel = value;
                api.SetAcceleration(AcsAxisId, currentAccel);
            }
        }

        private double CurrentDecel
        {
            get { return currentDecel; }
            set
            {
                if (Math.Abs(value - currentDecel) < 0.01) return;
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

        public bool AtPositiveHwLimit
        {
            get { return atPositiveHwLimit; }
            private set
            {
                if (atPositiveHwLimit == value)
                    return;
                atPositiveHwLimit = value;
                Action<int, bool> positiveHwLimitChanged = AtPositiveHwLimitChanged;
                if (positiveHwLimitChanged != null)
                    positiveHwLimitChanged(ApplicationAxisId, atPositiveHwLimit);
            }
        }

        public bool AtNegativeHwLimit
        {
            get { return atNegativeHwLimit; }
            private set
            {
                if (atNegativeHwLimit == value)
                    return;
                atNegativeHwLimit = value;
                Action<int, bool> negativeHwLimitChanged = AtNegativeHwLimitChanged;
                if (negativeHwLimitChanged != null)
                    negativeHwLimitChanged(ApplicationAxisId, atNegativeHwLimit);
            }
        }

        public bool AtPositiveSwLimit
        {
            get { return atPositiveSwLimit; }
            private set
            {
                if (atPositiveSwLimit == value)
                    return;
                atPositiveSwLimit = value;
                Action<int, bool> positiveSwLimitChanged = AtPositiveSwLimitChanged;
                if (positiveSwLimitChanged != null)
                    positiveSwLimitChanged(ApplicationAxisId, atPositiveSwLimit);
            }
        }

        public bool AtNegativeSwLimit
        {
            get { return atNegativeSwLimit; }
            private set
            {
                if (atNegativeSwLimit == value)
                    return;
                atNegativeSwLimit = value;
                Action<int, bool> negativeSwLimitChanged = AtNegativeSwLimitChanged;
                if (negativeSwLimitChanged != null)
                    negativeSwLimitChanged(ApplicationAxisId, atNegativeSwLimit);
            }
        }

        public double MinPos
        {
            get { return minPos; }
            set
            {
                minPos = value;
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
        }

        public double MaxPos
        {
            get { return maxPos; }
            set
            {
                maxPos = value;
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
        }

        public double HomeVelIn
        {
            get { return acsUtils.ReadGlobalReal("HOME_VEL_IN", (int) AcsAxisId); }
            set
            {
                if (HomeVelIn == value)
                    return;
                acsUtils.WriteGlobalReal(value, "HOME_VEL_IN", (int) AcsAxisId);
            }
        }

        public double HomeVelOut
        {
            get { return acsUtils.ReadGlobalReal("HOME_VEL_OUT", (int) AcsAxisId); }
            set
            {
                if (HomeVelOut == value)
                    return;
                acsUtils.WriteGlobalReal(value, "HOME_VEL_OUT", (int) AcsAxisId);
            }
        }

        public double HomeOffset
        {
            get
            {
                return acsUtils.ReadGlobalReal("HOME_OFFSET", (int) AcsAxisId);
            }
            set
            {
                if (HomeOffset == value)
                    return;
                acsUtils.WriteGlobalReal(value, "HOME_OFFSET", (int) AcsAxisId);
                acsUtils.ClearBits("MFLAGS", (int) AcsAxisId, 8);
            }
        }

        public string HomeStopCondition { get; set; }

        public int HomeBuffer { get; }

        public bool ScanningBufferRun { get; set; }

        public void RestoreDefaultSettings()
        {
            ReloadConfigParameters();
        }

        public void ClearError()
        {
            if (api.IsConnected) {
                try {
                    if (api.GetFault(AcsAxisId) == SafetyControlMasks.ACSC_NONE) return;
                    api.FaultClear(AcsAxisId);
                }
                catch (Exception e) {
                    logger.Error($"AcsAxis.ClearError: Exception '{AcsAxisId}': " + e.Message);
                }
            }
            else {
                logger.Info("Controller not connected");
            }
        }

        public bool Enable()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            if (enabled) return true;

            ClearError();
            api.Enable(AcsAxisId);
            api.WaitMotorEnabled(AcsAxisId, 1, WaitEnabledDisabledTimeout);
            return true;
        }

        public bool Disable()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
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

        public bool MoveAbsolute(double targetPos, bool waitProgramEnd, double vel = 0.0, double acc = 0.0,
            double dec = 0.0)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!api.IsConnected) {
                    logger.Info("Controller not connected");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy");
                    return false;
                }

                logger.Info(string.Format("MoveAbsolute {0} to {1} position", Name, targetPos));
                if (!Homed) {
                    logger.Info(Name + " need initialize");
                    return false;
                }

                if (!Enable()) {
                    logger.Info(Name + " failed to enable");
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

        public bool MoveRelative(bool waitProgramEnd, double relativePosition, double vel = 0.0, double acc = 0.0,
            double dec = 0.0)
        {
            if (IsLocked(moveLocker))
                return false;
            lock (moveLocker) {
                if (!api.IsConnected) {
                    logger.Info("Controller not connected");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy");
                    return false;
                }

                logger.Info(string.Format("MoveRelative {0} to {1} position", Name, relativePosition));
                if (!Enable()) {
                    logger.Info(Name + " failed to enable");
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
                    logger.Info("Controller not connected");
                    return false;
                }

                if (!Idle) {
                    logger.Info("Axis is busy");
                    return false;
                }

                logger.Info("Jog " + Name + " ");
                if (!Enable()) {
                    logger.Info(Name + " failed to enable");
                    return false;
                }

                if (vel == 0.0 || double.IsNaN(vel))
                    vel = DefaultVelocity;
                if (Math.Abs(vel) >= MaxVelocity * 0.99)
                    vel = vel >= 0.0 ? MaxVelocity * 0.99 : MaxVelocity * 0.99 * -1.0;

                if (acc > 0.0) CurrentAccel = acc;
                if (dec > 0.0) CurrentDecel = dec;

                JogImpl(waitProgramEnd, vel);
                return true;
            }
        }

        public bool Stop()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            lock (locker) {
                stopping = true;
                return HaltOrKill(true);
            }
        }

        public bool Abort()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return false;
            }

            lock (locker) {
                aborting = true;
                return HaltOrKill(false);
            }
        }

        public void SetRPos(double pos)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                logger.Info(string.Format("SetRPos {0} to {1} ", Name, pos));
                api.SetFPosition(AcsAxisId, pos);
            }
        }

        public void GetDataFromController()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                acsUtils.ReadInt("MFLAGS", (int) AcsAxisId);
                SafetyControlMasks fault = api.GetFault(AcsAxisId);
                AtPositiveHwLimit = (fault & SafetyControlMasks.ACSC_SAFETY_RL) > SafetyControlMasks.ACSC_NONE;
                AtNegativeHwLimit = (fault & SafetyControlMasks.ACSC_SAFETY_LL) > SafetyControlMasks.ACSC_NONE;
                AtPositiveSwLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SRL) > SafetyControlMasks.ACSC_NONE;
                AtNegativeSwLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SLL) > SafetyControlMasks.ACSC_NONE;
                MotorStates motorState = api.GetMotorState(AcsAxisId);
                Enabled = (motorState & MotorStates.ACSC_MST_ENABLE) == MotorStates.ACSC_MST_ENABLE;
                bool flag = (motorState & MotorStates.ACSC_MST_MOVE) != MotorStates.ACSC_MST_MOVE &&
                            !ScanningBufferRun;
                if (initializing) {
                    InitializingBufferRun = bufferHelper.IsProgramRunning((ProgramBuffer) HomeBuffer);
                    flag = flag && !InitializingBufferRun;
                }

                Idle = flag;
                if (Idle && (moving || stopping || aborting))
                    MotionEnded(true);
                Position = api.GetRPosition(AcsAxisId);
                CurrentVelocity = Math.Round(api.GetRVelocity(AcsAxisId));
                Ready = Enabled && Homed;
            }
        }

        private bool InitializingBufferRun
        {
            get { return initializingBufferRun; }
            set
            {
                if (value == InitializingBufferRun && (value || !initializing))
                    return;
                initializingBufferRun = value;
                if (!initializingBufferRun)
                    MotionEnded(Homed);
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
            if (!Enable() || !CheckInitParams())
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

        private bool CheckInitParams()
        {
            return HomeVelIn != 0.0 && HomeVelOut != 0.0 && HomeBuffer != -1;
        }

        private void MotionEnded(bool ok)
        {
            Position = api.GetRPosition(AcsAxisId);
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            lock (locker) {
                if (initializing) {
                    initializing = false;
                    flag2 = true;
                }

                if (moving) {
                    flag1 = true;
                    moving = false;
                }

                if (stopping) {
                    flag3 = true;
                    stopping = false;
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

        private void MotionEnded(int motorErr, int motionErr)
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
                    logger.Info(message);
            }
            catch (Exception ex) {
                logger.Info(ex.Message);
            }

            MotionEnded(motionErr == 0 && motorErr == 0);
        }

        private bool HaltOrKill(bool halt)
        {
            logger.Info(string.Format("haltOrKill {0} ", halt));
            try {
                if (bufferHelper.IsProgramRunning((ProgramBuffer) HomeBuffer)) api.StopBuffer((ProgramBuffer) HomeBuffer);

                if (halt)
                    api.Halt(AcsAxisId);
                else
                    api.Kill(AcsAxisId);
            }
            catch (Exception ex) {
                logger.Info(ex.Message + " " + Name);
                return false;
            }

            return true;
        }

        private void InitImpl(bool waitProgramEnd)
        {
            bool ok = true;
            lock (locker)
                initializing = true;
            Action<int> axisHomingBegin = AxisHomingBegin;
            if (axisHomingBegin != null)
                axisHomingBegin(ApplicationAxisId);
            try {
                bufferHelper.RunBuffer((ProgramBuffer) HomeBuffer);
                if (waitProgramEnd)
                    api.WaitProgramEnd((ProgramBuffer) HomeBuffer,
                        (int) (100000.0 * Math.Max(Math.Abs((MaxPos - MinPos) / HomeVelIn), 1.0)));
            }
            catch (Exception ex) {
                logger.Info("failed to init axis " + Name + "  :  " + ex.Message);
                Stop();
                lock (locker)
                    initializing = false;
                ok = false;
            }

            // ACS controller will set homing acceleration and deceleration during homing process, resend the operational
            // acceleration and deceleration value to controller here after homing done
            api.SetAcceleration(AcsAxisId, CurrentAccel);
            api.SetDeceleration(AcsAxisId, CurrentDecel);

            if (!waitProgramEnd)
                return;
            if (ok) {
                Thread.Sleep(100);
                ok = Homed;
            }

            MotionEnded(ok);
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
                MotionEnded(true);
            }
            catch (Exception ex) {
                MotionEnded(false);
                logger.Info(
                    string.Format("MoveAbsolute {0} to {1} position exception {2}", Name, targetPos, ex.Message));
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
                MotionEnded(true);
            }
            catch (Exception ex) {
                MotionEnded(false);
                logger.Info(
                    string.Format("MoveRelative {0} to {1} position exception {2}", Name, relativePosition, ex.Message));
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
                        MotionEnded(true);
                    }
                    catch (Exception ex) {
                        logger.Info(
                            string.Format("Jog {0} to {1}  exception {2}", Name, direction, ex.Message));
                        MotionEnded(true);
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
                MotionEnded(true);
            }
            catch (Exception ex) {
                logger.Info("Jog " + Name + " exception " + ex.Message);
                MotionEnded(true);
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

        public double GetPosition()
        {
            Position = api.GetRPosition(AcsAxisId);
            return Position;
        }
    }
}