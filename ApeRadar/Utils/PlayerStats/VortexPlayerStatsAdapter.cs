using ApeRadar.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApeRadar.Utils.PlayerStats
{
    internal sealed class VortexPlayerStatsAdapter : IPlayerStatsAdapter
    {
        public APIType ApiType => APIType.VORTEX;

        public Task<List<Player>> GetPlayersAsync(int playerCount, int relationFilter, JObject arenaInfo, Server server, int delayTimeBetweenHttpRequests)
        {
            return ApiUtils.VortexApiGetPlayersStatistics(playerCount, relationFilter, arenaInfo, server, delayTimeBetweenHttpRequests);
        }
    }
}
