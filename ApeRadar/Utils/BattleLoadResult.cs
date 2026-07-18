using ApeRadar.Models;

namespace ApeRadar.Utils
{
    internal sealed class BattleLoadResult
    {
        public BattleLoadResult(Battlefield battlefield, string outputText, int playerCount, Server server, Server secondaryServer)
        {
            Battlefield = battlefield;
            OutputText = outputText;
            PlayerCount = playerCount;
            Server = server;
            SecondaryServer = secondaryServer;
        }

        public Battlefield Battlefield { get; }
        public string OutputText { get; }
        public int PlayerCount { get; }
        public Server Server { get; }
        public Server SecondaryServer { get; }
    }
}
