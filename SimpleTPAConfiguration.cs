using Rocket.API;

namespace SimpleTPA
{
    public class SimpleTPAConfiguration : IRocketPluginConfiguration
    {
        public uint TickrateToExpire = 900;
        public uint TickrateToTeleport = 900;
        public uint ServerTickrate = 60;
        public void LoadDefaults()
        {
            
        }
    }
}
