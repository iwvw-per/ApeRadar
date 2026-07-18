using ApeRadar.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApeRadar.Utils.PlayerStats
{
    internal sealed class WgPublicPlayerStatsAdapter : IPlayerStatsAdapter
    {
        public APIType ApiType => APIType.WG_PUBLIC;

        public Task<List<Player>> GetPlayersAsync(int playerCount, int relationFilter, JObject arenaInfo, Server server, int delayTimeBetweenHttpRequests)
        {
            return ApiUtils.WgPublicApiGetPlayersStatistics(playerCount, relationFilter, arenaInfo, server, delayTimeBetweenHttpRequests, false);
        }
    }
}
