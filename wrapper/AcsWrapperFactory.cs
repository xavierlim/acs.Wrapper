
using CO.Systems.Services.Acs.AcsWrapper.config;
using CO.Systems.Services.Acs.AcsWrapper.mockery;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public static class AcsWrapperFactory
    {
        public static IAcsWrapper CreateInstance()
        {
            if (AcsSimHelper.IsEnable()) {
                AcsSimHelper.Start();
                if (AcsSimHelper.Config.SimulationMode == SimulationMode.MockPlatform) {
                    return new AcsMocker();
                }
            }

            return new AcsWrapper();
        }
    }
}