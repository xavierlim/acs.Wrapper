
using System;
using CO.Systems.Services.Acs.AcsWrapper.config;
using CO.Systems.Services.Acs.AcsWrapper.mockery;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public static class AcsWrapperFactory
    {
        public static IAcsWrapper CreateInstance()
        {
            AcsWrapper acs;

            if (AcsSimHelper.IsEnable()) {
                AcsSimHelper.Start();
                if (AcsSimHelper.Config.SimulationMode == SimulationMode.MockPlatform) {
                    return new AcsMocker();
                }
                else {
                    acs = new AcsWrapper();
                    try {
                        acs.Connect(null);
                        return acs;
                    }
                    catch (Exception e) {
                        // if failed to connect, return AcsMocker
                        return new AcsMocker();
                    }
                }
            }

            acs = new AcsWrapper();
            acs.Connect(null);
            return acs;
        }
    }
}