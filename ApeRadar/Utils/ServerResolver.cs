using ApeRadar.Models;
using System.IO;

namespace ApeRadar.Utils
{
    internal sealed class ServerResolver
    {
        public Server Resolve(Server configuredServer, string gamePath)
        {
            if (configuredServer != Server.AUTO)
            {
                return configuredServer;
            }

            return ServerExt.AutoDetectServer(Path.Combine(gamePath, "profile", "clientrunner.log"));
        }
    }
}
