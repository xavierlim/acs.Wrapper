
namespace CO.Systems.Services.Acs.AcsWrapper.config
{
    public class AcsSimConfig
    {

        public SimulationMode SimulationMode { get; set; }

        public AcsSimConfig()
        {
            SimulationMode = SimulationMode.AcsSimulator;
        }
    }

    public enum SimulationMode
    {
        AcsSimulator,
        MockPlatform,
    }
}