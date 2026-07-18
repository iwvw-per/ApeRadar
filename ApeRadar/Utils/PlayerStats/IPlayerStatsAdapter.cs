using ApeRadar.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApeRadar.Utils.PlayerStats
{
    internal interface IPlayerStatsAdapter
    {
        APIType ApiType { get; }

        Task<List<Player>> GetPlayersAsync(
            int playerCount,
            int relationFilter,
            JObject arenaInfo,
            Server server,
            int delayTimeBetweenHttpRequests);
    }
}
