
using System;
using CO.Common.Logger;
using CO.Systems.Services.Acs.AcsWrapper.config;
using CO.Systems.Services.Acs.AcsWrapper.mockery;
using CO.Systems.Services.Configuration.Settings;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public static class AcsWrapperFactory
    {
        public static IAcsWrapper CreateInstance(ILogger logger, IRobotControlSetting robotSettings,
            MachineCalibrationSetting machineCalSettings)
        {
            AcsWrapper acs;

            if (AcsSimHelper.IsEnable()) {
                AcsSimHelper.Start();
                if (AcsSimHelper.Config.SimulationMode == SimulationMode.MockPlatform) {
                    return new AcsMocker();
                }
                else {
                    acs = new AcsWrapper(logger, robotSettings, machineCalSettings);
                    try {
                        acs.Connect();
                        return acs;
                    }
                    catch (Exception e) {
                        // if failed to connect, return AcsMocker
                        return new AcsMocker();
                    }
                }
            }

            acs = new AcsWrapper(logger, robotSettings, machineCalSettings);
            acs.Connect();
            return acs;
        }
    }
}