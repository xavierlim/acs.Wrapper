// Decompiled with JetBrains decompiler
// Type: AcsWrapperImpl.AxisInitParameters
// Assembly: AcsWrapper, Version=1.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: DC1EDF75-AE0E-403A-BB79-8497514E3B04
// Assembly location: D:\git\tfs\NextGen.UI\SQDev.complete\CO.Phoenix\Source\CO.Systems\CO.Systems\TestApps\Acs\AcsPlatform\lib\AcsWrapper.dll

using CO.Systems.Services.Robot.Interface;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public class AxisInitParameters
    {
        public GantryAxes Axis { set; get; }

        public double FastVelocity { set; get; }

        public double SlowVelocity { set; get; }

        public double Accel { set; get; }

        public double Decel { set; get; }

        public double HomeOffset { get; set; }
    }

    public class AxisMoveParameters
    {
        public GantryAxes Axis { set; get; }

        public double TargetPos { set; get; }

        public double Velocity { set; get; }

        public double Accel { set; get; }

        public double Decel { set; get; }
    }

    /// <summary>
    /// used to pass gantry motion profiles (acc, dec, etc.) before starting scanning buffer
    /// </summary>
    public class AxesScanParameters
    {
        public AxisScanParameters AxisX { get; set; }
        public AxisScanParameters AxisY { get; set; }
        public AxisScanParameters AxisZ { get; set; }
    }

    /// <summary>
    /// used for <see cref="AxesScanParameters"/>
    /// </summary>
    public class AxisScanParameters
    {
        public double Acceleration { get; set; }
        public double Deceleration { get; set; }
    }
}