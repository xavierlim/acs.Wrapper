
namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public class IoStatus
    {
        // Inputs
        public bool EntryOpto;
        public bool ExitOpto;
        public bool LifterLowered;
        public bool BoardStopPanelAlignSensor;
        public bool StopperArmUp;
        public bool StopperArmDown;
        public bool StopperLocked;
        public bool StopperUnlocked;
        public bool RearClampUp;
        public bool FrontClampUp;
        public bool RearClampDown;
        public bool FrontClampDown;
        public bool ResetButton;
        public bool StartButton;
        public bool StopButton;
        public bool AlarmCancelPushButton;
        public bool WidthHomeSwitch;
        public bool WidthLimitSwitch;
        public bool UpstreamBoardAvailableSignal;
        public bool UpstreamFailedBoardAvailableSignal;
        public bool DownstreamMachineReadySignal;
        public bool BypassNormal;
        public bool EstopAndDoorOpenFeedback;

        // Outputs
        public bool LockStopper;
        public bool RaiseBoardStopStopper;
        public bool BeltShroudVaccumOn;
        public bool ClampPanel;
        public bool ResetButtonLight;
        public bool StartButtonLight;
        public bool StopButtonLight;
        public bool TowerLightRed;
        public bool TowerLightYellow;
        public bool TowerLightGreen;
        public bool TowerLightBlue;
        public bool TowerLightBuzzer;
        public bool StopSensor;
        public bool SmemaUpStreamMachineReady;
        public bool DownStreamBoardAvailable;
        public bool SmemaDownStreamFailedBoardAvailable;
    }

    public class PanelButtons
    {
        public bool StartButton;
        public bool StopButton;
        public bool ResetButton;
        public bool EStopButton;
    }

    public class PresentSensors
    {
        public bool EntryOpto;
        public bool ExitOpto;
        public bool BoardStopPanelAlignSensor;
    }

    public class ClampSensors
    {
        public bool RearClampUp;
        public bool FrontClampUp;
        public bool RearClampDown;
        public bool FrontClampDown;
    }

    public class SmemaIo
    {
        // inputs
        public bool UpstreamBoardAvailableSignal;
        public bool UpstreamFailedBoardAvailableSignal;
        public bool DownstreamMachineReadySignal;

        // outputs
        public bool SmemaUpStreamMachineReady;
        public bool DownStreamBoardAvailable;
        public bool SmemaDownStreamFailedBoardAvailable;
    }

    public enum AcsIndicatorState
    {
        On,
        Off,
        Flashing
    }
}