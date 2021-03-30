// Decompiled with JetBrains decompiler
// Type: AcsWrapperImpl.ACSAxis
// Assembly: AcsWrapper, Version=1.0.1.1, Culture=neutral, PublicKeyToken=null
// MVID: D9F1C9E4-1993-4C36-928A-A81812820D67
// Assembly location: D:\user\Documents\CyberOptics\tasks\acs platform\source\AcsWrapper - 19Mar2021\NewWrapper\NewWrapper\lib\AcsWrapper.dll

using ACS.SPiiPlusNET;
using CO.Common.Logger;
using CO.Systems.Services.Configuration.Settings;
using CO.Systems.Services.Robot.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;
using CO.Systems.Services.Acs.AcsWrapper.util;
using CO.Systems.Services.Acs.AcsWrapper.wrapper;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
  internal class ACSAxis
  {
    private Api Ch = (Api) null;
    private AcsUtils acsUtils = (AcsUtils) null;
    private IRobotControlSetting axisConfig = (IRobotControlSetting) null;
    private const int ACS_MFLAGS_HOME = 8;
    private int WaitEnabledDisabledTimeout = 10000;
    private readonly ILogger _logger = LoggersManager.SystemLogger;
    private object locker = new object();
    private object moveLocker = new object();
    private bool initing = false;
    private bool moving = false;
    private bool stoping = false;
    private bool aborting = false;
    private bool isAcsSimulation = false;
    private bool idle = true;
    private bool enabled = false;
    private bool ready = true;
    private double position = 0.0;
    private double currerntVelocity = 0.0;
    private double currentAccel = 0.0;
    private double currentDecel = 0.0;
    private bool atHomeSensor = false;
    private bool atPositiveHWLimit = false;
    private bool atNegativeHWLimit = false;
    private bool atPositiveSWLimit = false;
    private bool atNegativeSWLimit = false;
    private double minPos = 0.0;
    private double maxPos = 0.0;
    private bool initingBufferRun = false;

    private ACSAxis()
    {
    }

    internal ACSAxis(
      Api ch,
      AcsUtils utils,
      GantryAxes axisID,
      Axis acsAxisId,
      IRobotControlSetting config,
      bool isSimulation)
      : this()
    {
      this.Ch = ch;
      this.acsUtils = utils;
      this.axisConfig = config;
      this.ApplicationAxisId = (int) axisID;
      this.AcsAxisId = acsAxisId;
      this.isAcsSimulation = isSimulation;
      this.Name = this.ApplicationAxisId.ToString();
      this.ReloadConfigParameters();
      this.HomeStopCondition = string.Format("FAULT({0}).#LL", (object) (int) this.AcsAxisId);
      if (!this.Homed)
      {
        this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 64);
        this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 32);
      }
      this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 3);
      if (this.Homed)
      {
        this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 64);
        this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 32);
      }
      if (this.isAcsSimulation)
        this.HomeStopCondition = this.HomeStopCondition.Replace("#LL", "#SLL").Replace("#RL", "#SRL");
      switch (this.ApplicationAxisId)
      {
        case 0:
          if (this.isAcsSimulation)
          {
            this.HomeBuffer = 57;
            break;
          }
          this.HomeBuffer = 3;
          break;
        case 1:
          if (this.isAcsSimulation)
          {
            this.HomeBuffer = 55;
            break;
          }
          this.HomeBuffer = 0;
          break;
        case 2:
          if (this.isAcsSimulation)
          {
            this.HomeBuffer = 56;
            break;
          }
          this.HomeBuffer = 1;
          break;
      }
    }

    internal ACSAxis(
      Api ch,
      AcsUtils utils,
      ConveyorAxes axisID,
      Axis acsAxisId,
      bool isSimulation)
      : this()
    {
      this.Ch = ch;
      this.acsUtils = utils;
      this.ApplicationAxisId = (int) axisID;
      this.AcsAxisId = acsAxisId;
      this.isAcsSimulation = isSimulation;
      this.Name = this.ApplicationAxisId.ToString();
      this.ReloadConfigParameters();
      this.HomeStopCondition = string.Format("FAULT({0}).#LL", (object) (int) this.AcsAxisId);
      if (!this.Homed)
      {
        this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 64);
        this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 32);
      }
      this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 3);
      if (this.Homed)
      {
        this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 64);
        this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 32);
      }
      if (this.isAcsSimulation)
        this.HomeStopCondition = this.HomeStopCondition.Replace("#LL", "#SLL").Replace("#RL", "#SRL");
      switch (this.ApplicationAxisId)
      {
        case 5:
          this.HomeBuffer = 4;
          break;
        case 6:
          this.HomeBuffer = 5;
          break;
        case 7:
          this.HomeBuffer = 6;
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
      get { return this.idle; }
      private set
      {
        if (this.idle == value)
          return;
        this.idle = value;
        Action<int, bool> idleChanged = this.IdleChanged;
        if (idleChanged != null)
          idleChanged(this.ApplicationAxisId, this.idle);
      }
    }

    public bool Enabled
    {
      get { return this.enabled; }
      private set
      {
        if (this.enabled == value)
          return;
        this.enabled = value;
        Action<int, bool> enabledChanged = this.EnabledChanged;
        if (enabledChanged != null)
          enabledChanged(this.ApplicationAxisId, this.enabled);
      }
    }

    public bool Homed
    {
      get { return Convert.ToBoolean(this.acsUtils.ReadInt("MFLAGS", (int) this.AcsAxisId) & 8); }
    }

    public Axis AcsAxisId { get; private set; }

    public string Name { get; set; }

    public bool Ready
    {
      get { return this.ready; }
      private set
      {
        if (this.ready == value)
          return;
        this.ready = value;
        Action<int, bool> readyChanged = this.ReadyChanged;
        if (readyChanged != null)
          readyChanged(this.ApplicationAxisId, this.ready);
      }
    }

    public double Position
    {
      get { return this.position; }
      private set
      {
        if (this.position == value)
          return;
        this.position = value;
        Action<int, double> positionUpdated = this.PositionUpdated;
        if (positionUpdated != null)
          positionUpdated(this.ApplicationAxisId, this.position);
      }
    }

    public double DefaultVelocity { get; set; }

    public double CurrerntVelocity
    {
      get { return this.currerntVelocity; }
      private set
      {
        if (this.currerntVelocity == value)
          return;
        this.currerntVelocity = value;
        Action<int, double> velocityUpdated = this.VelocityUpdated;
        if (velocityUpdated != null)
          velocityUpdated(this.ApplicationAxisId, this.currerntVelocity);
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
        if (value == this.currentAccel)
          return;
        this.currentAccel = value;
        this.Ch.SetAcceleration(this.AcsAxisId, this.currentAccel);
      }
    }

    private double CurrentDecel
    {
      set
      {
        if (value == this.currentDecel)
          return;
        this.currentDecel = value;
        this.Ch.SetDeceleration(this.AcsAxisId, this.currentDecel);
      }
    }

    public double DefaultJerk { get; set; }

    public bool AtHomeSensor
    {
      get { return this.atHomeSensor; }
      private set
      {
        if (this.atHomeSensor == value)
          return;
        this.atHomeSensor = value;
        Action<int, bool> homeSensorChanged = this.AtHomeSensorChanged;
        if (homeSensorChanged != null)
          homeSensorChanged(this.ApplicationAxisId, this.atHomeSensor);
      }
    }

    public bool AtPositiveHWLimit
    {
      get { return this.atPositiveHWLimit; }
      private set
      {
        if (this.atPositiveHWLimit == value)
          return;
        this.atPositiveHWLimit = value;
        Action<int, bool> positiveHwLimitChanged = this.AtPositiveHWLimitChanged;
        if (positiveHwLimitChanged != null)
          positiveHwLimitChanged(this.ApplicationAxisId, this.atPositiveHWLimit);
      }
    }

    public bool AtNegativeHWLimit
    {
      get { return this.atNegativeHWLimit; }
      private set
      {
        if (this.atNegativeHWLimit == value)
          return;
        this.atNegativeHWLimit = value;
        Action<int, bool> negativeHwLimitChanged = this.AtNegativeHWLimitChanged;
        if (negativeHwLimitChanged != null)
          negativeHwLimitChanged(this.ApplicationAxisId, this.atNegativeHWLimit);
      }
    }

    public bool AtPositiveSWLimit
    {
      get { return this.atPositiveSWLimit; }
      private set
      {
        if (this.atPositiveSWLimit == value)
          return;
        this.atPositiveSWLimit = value;
        Action<int, bool> positiveSwLimitChanged = this.AtPositiveSWLimitChanged;
        if (positiveSwLimitChanged != null)
          positiveSwLimitChanged(this.ApplicationAxisId, this.atPositiveSWLimit);
      }
    }

    public bool AtNegativeSWLimit
    {
      get { return this.atNegativeSWLimit; }
      private set
      {
        if (this.atNegativeSWLimit == value)
          return;
        this.atNegativeSWLimit = value;
        Action<int, bool> negativeSwLimitChanged = this.AtNegativeSWLimitChanged;
        if (negativeSwLimitChanged != null)
          negativeSwLimitChanged(this.ApplicationAxisId, this.atNegativeSWLimit);
      }
    }

    public double MinPos
    {
      get { return this.minPos; }
      set
      {
        this.minPos = value;
        try
        {
          if (this.minPos == double.MinValue)
          {
            this.acsUtils.ClearBits("FMASK", (int) this.AcsAxisId, 64);
            if (this.Homed)
              return;
            this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 64);
          }
          else
          {
            this.acsUtils.SetBits("FMASK", (int) this.AcsAxisId, 64);
            if (this.Homed)
              this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 64);
            this.acsUtils.WriteVariable((object) (this.minPos - 0.01), "SLLIMIT", From1: ((int) this.AcsAxisId), To1: ((int) this.AcsAxisId));
          }
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 494, nameof (MinPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        }
      }
    }

    public double MaxPos
    {
      get { return this.maxPos; }
      set
      {
        this.maxPos = value;
        try
        {
          if (this.maxPos == double.MaxValue)
          {
            this.acsUtils.ClearBits("FMASK", (int) this.AcsAxisId, 32);
            if (this.Homed)
              return;
            this.acsUtils.ClearBits("FDEF", (int) this.AcsAxisId, 32);
          }
          else
          {
            this.acsUtils.SetBits("FMASK", (int) this.AcsAxisId, 32);
            if (this.Homed)
              this.acsUtils.SetBits("FDEF", (int) this.AcsAxisId, 32);
            this.acsUtils.WriteVariable((object) (this.maxPos + 0.001), "SRLIMIT", From1: ((int) this.AcsAxisId), To1: ((int) this.AcsAxisId));
          }
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 526, nameof (MaxPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        }
      }
    }

    public double HomeVelIn
    {
      get { return this.acsUtils.ReadGlobalReal("HOME_VEL_IN", (int) this.AcsAxisId); }
      set
      {
        if (this.HomeVelIn == value)
          return;
        try
        {
          this.acsUtils.WriteGlobalReal((object) value, "HOME_VEL_IN", (int) this.AcsAxisId);
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 549, nameof (HomeVelIn), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        }
      }
    }

    public double HomeVelOut
    {
      get { return this.acsUtils.ReadGlobalReal("HOME_VEL_OUT", (int) this.AcsAxisId); }
      set
      {
        if (this.HomeVelOut == value)
          return;
        try
        {
          this.acsUtils.WriteGlobalReal((object) value, "HOME_VEL_OUT", (int) this.AcsAxisId);
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 571, nameof (HomeVelOut), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        }
      }
    }

    public double HomeOffset
    {
      get
      {
        try
        {
          return this.acsUtils.ReadGlobalReal("HOME_OFFSET", (int) this.AcsAxisId);
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 587, nameof (HomeOffset), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return 0.0;
        }
      }
      set
      {
        if (this.HomeOffset == value)
          return;
        try
        {
          this.acsUtils.WriteGlobalReal((object) value, "HOME_OFFSET", (int) this.AcsAxisId);
          this.acsUtils.ClearBits("MFLAGS", (int) this.AcsAxisId, 8);
        }
        catch (Exception ex)
        {
          this._logger.Info(ex.Message, 602, nameof (HomeOffset), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        }
      }
    }

    public string HomeStopCondition { get; set; }

    public int HomeBuffer { get; private set; } = -1;

    public bool ScanningBufferRun { get; set; } = false;

    public void RestoreDefualtSettings()
    {
      this.ReloadConfigParameters();
    }

    public void ClearError()
    {
      if (!this.Ch.IsConnected)
        this._logger.Info("Controller not connected", 636, nameof (ClearError), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      else
        this.Ch.FaultClear(this.AcsAxisId);
    }

    public bool Enable()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 646, nameof (Enable), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      this.ClearError();
      this.Ch.Enable(this.AcsAxisId);
      this.Ch.WaitMotorEnabled(this.AcsAxisId, 1, this.WaitEnabledDisabledTimeout);
      return true;
    }

    public bool Disable()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 660, nameof (Disable), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      this.Ch.Disable(this.AcsAxisId);
      this.Ch.WaitMotorEnabled(this.AcsAxisId, 0, this.WaitEnabledDisabledTimeout);
      return true;
    }

    public bool Init(bool waitProgramEnd)
    {
      if (this.IsLocked(this.moveLocker))
        return false;
      lock (this.moveLocker)
      {
        if (!this.InitPrepare())
          return false;
        this.ReloadConfigParameters();
        this.InitImpl(waitProgramEnd);
        return true;
      }
    }

    public bool Init(AxisInitParameters initParameters, bool waitProgramEnd)
    {
      if (this.IsLocked(this.moveLocker))
        return false;
      lock (this.moveLocker)
      {
        if (!this.InitPrepare())
          return false;
        this.CurrentAccel = initParameters.Accel;
        this.CurrentDecel = initParameters.Decel;
        this.HomeVelIn = initParameters.FastVelocity;
        this.HomeVelOut = Math.Abs(initParameters.SlowVelocity);
        if (this.HomeVelIn < 0.0)
          this.HomeVelOut *= -1.0;
        this.HomeOffset = initParameters.HomeOffset;
        this.InitImpl(waitProgramEnd);
        return true;
      }
    }

    public bool MoveAbsolute(
      double targetPos,
      bool waitProgramEnd,
      double vel = 0.0,
      double acc = 0.0,
      double dec = 0.0)
    {
      if (this.IsLocked(this.moveLocker))
        return false;
      lock (this.moveLocker)
      {
        if (!this.Ch.IsConnected)
        {
          this._logger.Info("Controller not connected", 747, nameof (MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (!this.Idle)
        {
          this._logger.Info("Axis is busy", 752, nameof (MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        this._logger.Info(string.Format("MoveAbsolute {0} to {1} position", (object) this.Name, (object) targetPos), 756, nameof (MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        if (!this.Homed)
        {
          this._logger.Info(this.Name + " need initialize", 760, nameof (MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (!this.Enable())
        {
          this._logger.Info(this.Name + " failed to enable", 765, nameof (MoveAbsolute), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (vel == 0.0 || double.IsNaN(vel))
          vel = this.DefaultVelocity;
        if (vel >= this.MaxVelocity * 0.99)
          vel = this.MaxVelocity * 0.99;
        if (acc == 0.0 || double.IsNaN(acc))
          acc = this.DefaultAccel;
        if (dec == 0.0 || double.IsNaN(dec))
          dec = this.DefaultDecel;
        this.CurrentAccel = acc;
        this.CurrentDecel = dec;
        this.MoveAbsoluteImpl(targetPos, vel, waitProgramEnd);
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
      if (this.IsLocked(this.moveLocker))
        return false;
      lock (this.moveLocker)
      {
        if (!this.Ch.IsConnected)
        {
          this._logger.Info("Controller not connected", 796, nameof (MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (!this.Idle)
        {
          this._logger.Info("Axis is busy", 801, nameof (MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        this._logger.Info(string.Format("MoveRelative {0} to {1} position", (object) this.Name, (object) relativePosition), 805, nameof (MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        if (!this.Enable())
        {
          this._logger.Info(this.Name + " failed to enable", 809, nameof (MoveRelative), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (vel == 0.0 || double.IsNaN(vel))
          vel = this.DefaultVelocity;
        if (vel >= this.MaxVelocity * 0.99)
          vel = this.MaxVelocity * 0.99;
        if (acc == 0.0 || double.IsNaN(acc))
          acc = this.DefaultAccel;
        if (dec == 0.0 || double.IsNaN(dec))
          dec = this.DefaultDecel;
        this.CurrentAccel = acc;
        this.CurrentDecel = dec;
        this.MoveRelativeImpl(waitProgramEnd, relativePosition, vel);
        return true;
      }
    }

    public bool Jog(bool waitProgramEnd, double vel = 0.0, double acc = 0.0, double dec = 0.0)
    {
      if (this.IsLocked(this.moveLocker))
        return false;
      lock (this.moveLocker)
      {
        if (!this.Ch.IsConnected)
        {
          this._logger.Info("Controller not connected", 844, nameof (Jog), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (!this.Idle)
        {
          this._logger.Info("Axis is busy", 849, nameof (Jog), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        this._logger.Info("Jog " + this.Name + " ", 853, nameof (Jog), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        if (!this.Enable())
        {
          this._logger.Info(this.Name + " failed to enable", 857, nameof (Jog), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
          return false;
        }
        if (vel == 0.0 || double.IsNaN(vel))
          vel = this.DefaultVelocity;
        if (Math.Abs(vel) >= this.MaxVelocity * 0.99)
          vel = vel >= 0.0 ? this.MaxVelocity * 0.99 : this.MaxVelocity * 0.99 * -1.0;
        if (acc == 0.0 || double.IsNaN(acc))
          acc = this.DefaultAccel;
        if (dec == 0.0 || double.IsNaN(dec))
          dec = this.DefaultDecel;
        this.CurrentAccel = acc;
        this.CurrentDecel = dec;
        vel = Math.Abs(vel);
        this.JogImpl(waitProgramEnd, vel);
        return true;
      }
    }

    public bool Stop()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 892, nameof (Stop), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      lock (this.locker)
      {
        this.stoping = true;
        return this.haltOrKill(true);
      }
    }

    public bool Abort()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 911, nameof (Abort), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      lock (this.locker)
      {
        this.aborting = true;
        return this.haltOrKill(false);
      }
    }

    public void SetRPos(double pos)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 931, nameof (SetRPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
      else
      {
        this._logger.Info(string.Format("SetRPos {0} to {1} ", (object) this.Name, (object) pos), 935, nameof (SetRPos), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        this.Ch.SetFPosition(this.AcsAxisId, pos);
      }
    }

    public void getDataFromController()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 947, nameof (getDataFromController), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
      else
      {
        this.acsUtils.ReadInt("MFLAGS", (int) this.AcsAxisId);
        SafetyControlMasks fault = this.Ch.GetFault(this.AcsAxisId);
        this.AtPositiveHWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_RL) > SafetyControlMasks.ACSC_NONE;
        this.AtNegativeHWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_LL) > SafetyControlMasks.ACSC_NONE;
        this.AtPositiveSWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SRL) > SafetyControlMasks.ACSC_NONE;
        this.AtNegativeSWLimit = (fault & SafetyControlMasks.ACSC_SAFETY_SLL) > SafetyControlMasks.ACSC_NONE;
        MotorStates motorState = this.Ch.GetMotorState(this.AcsAxisId);
        this.Enabled = (motorState & MotorStates.ACSC_MST_ENABLE) == MotorStates.ACSC_MST_ENABLE;
        bool flag = (motorState & MotorStates.ACSC_MST_MOVE) != MotorStates.ACSC_MST_MOVE && !this.ScanningBufferRun;
        if (this.initing)
        {
          this.InitingBufferRun = this.acsUtils.IsProgramRunning((ProgramBuffer) this.HomeBuffer);
          flag = flag && !this.InitingBufferRun;
        }
        this.Idle = flag;
        if (this.Idle && (this.moving || this.stoping || this.aborting))
          this.motionEnded(true);
        this.Position = this.Ch.GetRPosition(this.AcsAxisId);
        this.CurrerntVelocity = Math.Round(this.Ch.GetRVelocity(this.AcsAxisId));
        this.Ready = this.Enabled && this.Homed;
      }
    }

    private bool InitingBufferRun
    {
      get { return this.initingBufferRun; }
      set
      {
        if (value == this.InitingBufferRun && (value || !this.initing))
          return;
        this.initingBufferRun = value;
        if (!this.initingBufferRun)
          this.motionEnded(this.Homed);
      }
    }

    private bool InitPrepare()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 1003, nameof (InitPrepare), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      if (!this.Idle)
      {
        this._logger.Info("Axis is busy", 1008, nameof (InitPrepare), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      this.ClearError();
      this._logger.Info("Init Axis " + this.Name, 1013, nameof (InitPrepare), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      if (!this.Enable() || !this.checkInitParams())
        return false;
      this.Idle = false;
      return true;
    }

    public void ReloadConfigParameters()
    {
      if (this.axisConfig == null)
      {
        this.LoadDefaultConfigParameters();
      }
      else
      {
        this.DefaultVelocity = this.axisConfig.DefaultVelocity[this.ApplicationAxisId] * 1000.0;
        this.DefaultAccel = this.axisConfig.DefaultAccel[this.ApplicationAxisId] * 1000.0;
        this.DefaultDecel = this.axisConfig.DefaultDecel[this.ApplicationAxisId] * 1000.0;
        this.MinPos = (double) (this.axisConfig.MinPositionLimit[this.ApplicationAxisId] * 1000L);
        this.MaxPos = (double) (this.axisConfig.MaxPositionLimit[this.ApplicationAxisId] * 1000L);
        this.HomeVelIn = this.axisConfig.HomeVelocity1[this.ApplicationAxisId] * 1000.0;
        this.HomeVelOut = this.axisConfig.HomeVelocity2[this.ApplicationAxisId] * 1000.0;
        this.HomeVelOut = Math.Abs(this.HomeVelOut);
        if (this.HomeVelIn < 0.0)
          this.HomeVelOut *= -1.0;
        this.HomeOffset = this.axisConfig.HomeOffset[this.ApplicationAxisId] * 1000.0;
        this.HomeStopCondition = string.Format("FAULT({0}).#LL", (object) (int) this.AcsAxisId);
        this.MaxAccel = this.axisConfig.MaxAccel[this.ApplicationAxisId] * 1000.0;
        this.MaxDecel = this.axisConfig.MaxDecel[this.ApplicationAxisId] * 1000.0;
        this.MaxVelocity = this.axisConfig.MaxVelocity[this.ApplicationAxisId] * 1000.0;
      }
    }

    private void LoadDefaultConfigParameters()
    {
      this.DefaultVelocity = 100.0;
      this.DefaultAccel = 7500.0;
      this.DefaultDecel = 7500.0;
      this.MaxVelocity = this.DefaultVelocity * 5.0;
      this.MaxAccel = this.DefaultAccel * 5.0;
      this.MaxDecel = this.DefaultDecel * 5.0;
      this.MinPos = -1000.0;
      this.MaxPos = 1000.0;
      this.HomeVelIn = 75.0;
      this.HomeVelOut = 10.0;
      this.HomeOffset = 28.0;
    }

    private bool checkInitParams()
    {
      return this.HomeVelIn != 0.0 && this.HomeVelOut != 0.0 && this.HomeBuffer != -1;
    }

    private void motionEnded(bool ok)
    {
      this.Position = this.Ch.GetRPosition(this.AcsAxisId);
      bool flag1 = false;
      bool flag2 = false;
      bool flag3 = false;
      bool flag4 = false;
      lock (this.locker)
      {
        if (this.initing)
        {
          this.initing = false;
          flag2 = true;
        }
        if (this.moving)
        {
          flag1 = true;
          this.moving = false;
        }
        if (this.stoping)
        {
          flag3 = true;
          this.stoping = false;
        }
        if (this.aborting)
        {
          flag4 = true;
          this.aborting = false;
        }
      }
      if (flag2)
      {
        Action<int, bool> axisHomingEnd = this.AxisHomingEnd;
        if (axisHomingEnd != null)
          axisHomingEnd(this.ApplicationAxisId, ok);
      }
      if (flag1)
      {
        Action<int, bool> movementEnd = this.MovementEnd;
        if (movementEnd != null)
          movementEnd(this.ApplicationAxisId, ok);
      }
      if (flag3)
      {
        Action<int, bool> stopDone = this.StopDone;
        if (stopDone != null)
          stopDone(this.ApplicationAxisId, ok);
      }
      if (!flag4)
        return;
      Action<int, bool> abortDone = this.AbortDone;
      if (abortDone == null)
        return;
      abortDone(this.ApplicationAxisId, ok);
    }

    private void motionEnded(int motorErr, int motionErr)
    {
      try
      {
        if (motionErr >= 5000 && motionErr <= 5006)
          motionErr = 0;
        if (motorErr >= 5000 && motorErr <= 5006)
          motorErr = 0;
        string message = string.Empty;
        if ((uint) motionErr > 0U)
          message = this.Name + " " + this.Ch.GetErrorString(motionErr);
        if (motorErr != 0 && motionErr != motorErr)
          message = this.Name + " " + this.Ch.GetErrorString(motorErr);
        if (!string.IsNullOrEmpty(message))
          this._logger.Info(message, 1159, nameof (motionEnded), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
      catch (Exception ex)
      {
        this._logger.Info(ex.Message, 1163, nameof (motionEnded), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
      this.motionEnded(motionErr == 0 && motorErr == 0);
    }

    private bool haltOrKill(bool halt)
    {
      this._logger.Info(string.Format("haltOrKill {0} ", (object) halt), 1169, nameof (haltOrKill), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      try
      {
        this.Ch.StopBuffer((ProgramBuffer) this.HomeBuffer);
        if (halt)
          this.Ch.Halt(this.AcsAxisId);
        else
          this.Ch.Kill(this.AcsAxisId);
      }
      catch (Exception ex)
      {
        this._logger.Info(ex.Message + " " + this.Name, 1184, nameof (haltOrKill), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        return false;
      }
      return true;
    }

    private void InitImpl(bool waitProgramEnd)
    {
      bool ok = true;
      lock (this.locker)
        this.initing = true;
      Action<int> axisHomingBegin = this.AxisHomingBegin;
      if (axisHomingBegin != null)
        axisHomingBegin(this.ApplicationAxisId);
      try
      {
        this.acsUtils.RunBuffer((ProgramBuffer) this.HomeBuffer);
        if (waitProgramEnd)
          this.Ch.WaitProgramEnd((ProgramBuffer) this.HomeBuffer, (int) (100000.0 * Math.Max(Math.Abs((this.MaxPos - this.MinPos) / this.HomeVelIn), 1.0)));
      }
      catch (Exception ex)
      {
        this._logger.Info("failed to init axis " + this.Name + "  :  " + ex.Message, 1214, nameof (InitImpl), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        this.Stop();
        lock (this.locker)
          this.initing = false;
        ok = false;
      }
      if (!waitProgramEnd)
        return;
      if (ok)
      {
        Thread.Sleep(100);
        ok = this.Homed;
      }
      this.motionEnded(ok);
    }

    private async void InitImplAsync(bool waitProgramEnd)
    {
      await Task.Run((Action) (() =>
      {
        lock (this.moveLocker)
          this.InitImpl(waitProgramEnd);
      }));
    }

    private void MoveAbsoluteImpl(double targetPos, double vel, bool waitProgramEnd)
    {
      this.moving = true;
      Action<int> movementBegin = this.MovementBegin;
      if (movementBegin != null)
        movementBegin(this.ApplicationAxisId);
      try
      {
        this.Ch.ExtToPoint(MotionFlags.ACSC_AMF_VELOCITY | MotionFlags.ACSC_AMF_ENDVELOCITY, this.AcsAxisId, targetPos, vel, 0.0);
        if (!waitProgramEnd)
          return;
        this.Ch.WaitMotionEnd(this.AcsAxisId, (int) (100000.0 * Math.Max(Math.Abs((targetPos - this.Position) / vel), 1.0)));
        this.motionEnded(true);
      }
      catch (Exception ex)
      {
        this.motionEnded(false);
        this._logger.Info(string.Format("MoveAbsolute {0} to {1} position exception {2}", (object) this.Name, (object) targetPos, (object) ex.Message), 1272, nameof (MoveAbsoluteImpl), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
    }

    private async void MoveAbsoluteImplAsync(double targetPos, double vel)
    {
      await Task.Run((Action) (() =>
      {
        lock (this.moveLocker)
          this.MoveAbsoluteImpl(targetPos, vel, true);
      }));
    }

    private async void MoveRelativeImplAsync(double relativePosition, double vel)
    {
      await Task.Run((Action) (() =>
      {
        lock (this.moveLocker)
          this.MoveRelativeImpl(true, relativePosition, vel);
      }));
    }

    private void MoveRelativeImpl(bool waitProgramEnd, double relativePosition, double vel)
    {
      lock (this.locker)
        this.moving = true;
      Action<int> movementBegin = this.MovementBegin;
      if (movementBegin != null)
        movementBegin(this.ApplicationAxisId);
      try
      {
        this.Ch.ExtToPoint(MotionFlags.ACSC_AMF_RELATIVE | MotionFlags.ACSC_AMF_VELOCITY | MotionFlags.ACSC_AMF_ENDVELOCITY, this.AcsAxisId, relativePosition, vel, 0.0);
        if (!waitProgramEnd)
          return;
        this.Ch.WaitMotionEnd(this.AcsAxisId, (int) (100000.0 * Math.Max(Math.Abs(relativePosition / vel), 1.0)));
        this.motionEnded(true);
      }
      catch (Exception ex)
      {
        this.motionEnded(false);
        this._logger.Info(string.Format("MoveRelative {0} to {1} position exception {2}", (object) this.Name, (object) relativePosition, (object) ex.Message), 1324, nameof (MoveRelativeImpl), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
      }
    }

    private async void JogImplAsync(RobotAxisDirection direction, double vel)
    {
      await Task.Run((Action) (() =>
      {
        lock (this.locker) {
          this.moving = true;
          Action<int> movementBegin = this.MovementBegin;
          if (movementBegin != null)
            movementBegin(this.ApplicationAxisId);
          try {
            this.Ch.Jog(MotionFlags.ACSC_AMF_VELOCITY, this.AcsAxisId, vel);
            this.Ch.WaitMotionEnd(this.AcsAxisId, int.MaxValue);
            this.motionEnded(true);
          }
          catch (Exception ex) {
            this._logger.Info(
              string.Format("Jog {0} to {1}  exception {2}", (object) this.Name, (object) direction,
                (object) ex.Message), 1345, nameof(JogImplAsync),
              "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
            this.motionEnded(true);
          }
        }
      }));
    }

    private void JogImpl(bool waitProgramEnd, double vel)
    {
      lock (this.locker)
        this.moving = true;
      Action<int> movementBegin = this.MovementBegin;
      if (movementBegin != null)
        movementBegin(this.ApplicationAxisId);
      try
      {
        this.Ch.Jog(MotionFlags.ACSC_AMF_VELOCITY, this.AcsAxisId, vel);
        if (!waitProgramEnd)
          return;
        this.Ch.WaitMotionEnd(this.AcsAxisId, int.MaxValue);
        this.motionEnded(true);
      }
      catch (Exception ex)
      {
        this._logger.Info("Jog " + this.Name + " exception " + ex.Message, 1373, nameof (JogImpl), "C:\\Users\\Garry\\source\\repos\\SQ3000plus\\AcsWrapper\\ACSAxis.cs");
        this.motionEnded(true);
      }
    }

    private bool IsLocked(object locker)
    {
      bool lockTaken = false;
      try
      {
        Monitor.TryEnter(locker, 0, ref lockTaken);
        if (!lockTaken)
          return true;
      }
      finally
      {
        if (lockTaken)
          Monitor.Exit(locker);
      }
      return false;
    }
  }
}
