
using CO.Systems.Services.Configuration.Settings;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public class BypassModeBufferParameters
    {
        public int WaitTimeToSearch { get; set; }

        public int WaitTimeToAcq { get; set; }

        public int WaitTimeToCutout { get; set; }

        public int WaitTimeToExit { get; set; }

        public int WaitTimeToRelease { get; set; }

        public int WaitTimeToSmema { get; set; }
    }

    public class ChangeWidthBufferParameters
    {
        public double ConveyorSpecifiedWidth { get; set; }

        public int WaitTimeToSearch { get; set; }
    }

    public class FreePanelBufferParameters
    {
        public int UnclampLiftDelayTime { get; set; }

        public int WaitTimeToUnlift { get; set; }

        public int WaitTimeToUnclamp { get; set; }
    }

    public class InternalMachineLoadBufferParameters
    {
        public int WaitTimeToSlow { get; set; }
        public int WaitTimeToAlign { get; set; }
        public int SlowDelayTime { get; set; }
    }

    public class LoadPanelBufferParameters
    {
        public int WaitTimeToAcq { get; set; }
        public double Stage_1_LifterOnlyDistance { get; set; }
        public double Stage_2_LifterAndClamperDistance { get; set; }
    }

    public class PowerOnRecoverFromEmergencyStopBufferParameters
    {
        public int WaitTimeToSearch { get; set; }

        public int WaitTimeToExit { get; set; }

        public int WaitTimeToReset { get; set; }

        public double WidthToW_0_Position { get; set; }
    }

    public class PreReleasePanelBufferParameters
    {
        public int WaitTimeToExit { get; set; }
    }

    public class ReleasePanelBufferParameters
    {
        public int WaitTimeToExit { get; set; }

        public int WaitTimeToRelease { get; set; }

        public int WaitTimeToSmema { get; set; }

        public int WaitTimeToCutout { get; set; }

        public int WaitTimeToBeltVacuum { get; set; }
    }

    public class ReloadPanelBufferParameters
    {
        public int WaitTimeToSearch { get; set; }
        public int ReloadDelayTime { get; set; }
        public double Stage_1_LifterOnlyDistance { get; set; }
        public double Stage_2_LifterAndClamperDistance { get; set; }
    }

    public class SecurePanelBufferParameters
    {
        public int ClampLiftDelayTime { get; set; }

        public int WaitTimeToPanelClamped { get; set; }

        public int WaitTimeToLifted { get; set; }

        public int WaitTimeToUnstop { get; set; }
    }

    public class HomeConveyorWidthParameters
    {
        public bool AutoWidthEnable { set; get; }
        public double HomeInVelocity { get; set; }
        public double HomeOutVelocity { get; set; }
        public double HomeOffset { get; set; }
    }

    public class DBufferParameters
    {
        public double ConveyorBeltAcquireSpeed { get; set; }
        public double ConveyorBeltLoadingSpeed { get; set; }
        public double ConveyorBeltSlowSpeed { get; set; }
        public double ConveyorBeltReleaseSpeed { get; set; }
        public double ConveyorBeltUnloadingSpeed { get; set; }
        
        public ConveyorOperationMode OperationMode { get; set; }
        public bool ConveyorSimultaneousLoadUnload { get; set; }
        public bool ConveyorReleaseToUpstream { get; set; }
        
        /// <summary>
        /// failed board mode with value definition following <see cref="IConveyorControlSetting.EnableFailedBoardSmemaEx"/>
        /// 0: disable
        /// 1: when inspection failed, trigger failed board output to downstream 
        /// 2: when inspection failed, trigger failed board and customer outputs to downstream
        /// 3: when inspection failed, trigger failed board output to upstream
        /// 4: when inspection failed, trigger failed board and customer outputs to downstream, failed board output to be inverted
        /// </summary>
        public int SmemaFailedBoardMode { get; set; }
        
        public ConveyorDirection ConveyorDirection { get; set; }
        public ushort ConveyorWaitTimeToAlign { get; set; }

        public double DistanceBetweenEntryAndStopSensor { get; set; }
        public double DistanceBetweenSlowPositionAndStopSensor { get; set; }
        public double DistanceBetweenSlowPositionAndEntrySensor { get; set; }

        public double Stage_1_LifterSpeed { get; set; }
        public double Stage_2_LifterSpeed { get; set; }
        public double LifterDownSpeed { get; set; }
    }

    public enum ConveyorDirection
    {
        Backward = -1, // 0xFFFFFFFF
        Forward = 1,
    }

    public class ConveyorAxesMoveParameters
    {
        public ConveyorAxes Axis { set; get; }

        public double TargetPos { set; get; }

        public double Velocity { set; get; }

        public double Accel { set; get; }

        public double Decel { set; get; }
    }
    
    /// <summary>
    /// defined conveyor operation mode on ACS controller
    /// </summary>
    public enum ConveyorOperationMode {
        PingPongMode = 0,
        InlineMode = 1,
        OfflineMode = 2,
        SmemaSerialUpstream = 3,
        SmemaSerialDownstream = 4,
    }
}