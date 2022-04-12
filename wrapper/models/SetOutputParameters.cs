
namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public class SetOutputParameters
    {
        public bool LockStopper;
        public bool RaiseBoardStopStopper;
        public bool BeltShroudVacuumOn;
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
        public bool SensorPower;
        public bool StopSensor;
        public bool SmemaUpStreamMachineReady;
        public bool DownStreamBoardAvailable;
        public bool SmemaDownStreamFailedBoardAvailable;
    }
}