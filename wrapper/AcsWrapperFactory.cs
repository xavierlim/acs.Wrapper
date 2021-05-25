
using System;
using CO.Common.Logger;
using CO.Systems.Services.Acs.AcsWrapper.config;
using CO.Systems.Services.Acs.AcsWrapper.mockery;
using CO.Systems.Services.Configuration.Settings;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public static class AcsWrapperFactory
    {
        public static IAcsWrapper CreateInstance(ILogger logger, IRobotControlSetting robotSettings)
        {
            AcsWrapper acs;

            if (AcsSimHelper.IsEnable()) {
                AcsSimHelper.Start();
                if (AcsSimHelper.Config.SimulationMode == SimulationMode.MockPlatform) {
                    return new AcsMocker();
                }
                else {
                    acs = new AcsWrapper(logger, robotSettings);
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

            acs = new AcsWrapper(logger, robotSettings);
            acs.Connect();
            return acs;
        }
    }
}