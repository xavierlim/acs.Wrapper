
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
        public bool ConveyorPressure;
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
        public bool BypassDirection;
        public bool EstopRight;
        public bool EstopLeft;
        public bool EstopAndDoorOpenFeedback;

        // Outputs
        public bool LockStopper;
        public bool RaiseBoardStopStopper;
        public bool BeltShroudVaccumOn;
        public bool VacuumChuckEjector;
        public bool VacuumChuckValve;
        public bool ClampPanel;
        public bool ResetButtonLight;
        public bool StartButtonLight;
        public bool StopButtonLight;
        public bool TowerLightRed;
        public bool TowerLightYellow;
        public bool TowerLightGreen;
        public bool TowerLightBlue;
        public bool TowerLightBuzzer;
        public bool SensorPower;            // power supply for inspection sensor
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
        public bool EstopRight;
        public bool EstopLeft;
        public bool SafetyRelay;

        public PanelButtons() {
            SafetyRelay = true;
        }
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
        Off = 0,
        On = 1,
        Flashing = 2
    }
}