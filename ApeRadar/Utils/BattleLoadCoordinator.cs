using ApeRadar.Models;
using ApeRadar.Utils.PlayerStats;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApeRadar.Utils
{
    internal sealed class BattleLoadCoordinator
    {
        private const int DelayTimeBetweenRetryAttempts = 1000;
        private const int DelayTimeBetweenHttpRequestsBaseValue = 20;
        private const int DelayTimeBetweenHttpRequestsAdditionalDelayPerRetryAttempt = 10;

        private readonly ArenaInfoReader arenaInfoReader;
        private readonly ServerResolver serverResolver;
        private readonly WatchListRepository watchListRepository;
        private readonly BattlefieldFactory battlefieldFactory;
        private readonly IReadOnlyDictionary<APIType, IPlayerStatsAdapter> playerStatsAdapters;

        public BattleLoadCoordinator(
            ArenaInfoReader arenaInfoReader,
            ServerResolver serverResolver,
            WatchListRepository watchListRepository,
            BattlefieldFactory battlefieldFactory,
            IEnumerable<IPlayerStatsAdapter> playerStatsAdapters)
        {
            this.arenaInfoReader = arenaInfoReader;
            this.serverResolver = serverResolver;
            this.watchListRepository = watchListRepository;
            this.battlefieldFactory = battlefieldFactory;
            this.playerStatsAdapters = playerStatsAdapters.ToDictionary(adapter => adapter.ApiType);
        }

        public async Task<BattleLoadResult> LoadAsync(BattleLoadOptions options, CancellationToken cancellationToken = default)
        {
            for (int attempt = 0; attempt <= options.MaximumRetryAttempts; attempt++)
            {
                try
                {
                    return await LoadOnceAsync(options, attempt, cancellationToken);
                }
                catch (Exception ex) when (ShouldRetry(ex) && attempt < options.MaximumRetryAttempts)
                {
                    await Task.Delay(DelayTimeBetweenRetryAttempts, cancellationToken);
                    options.RetryStarted?.Invoke(attempt + 1);
                }
            }

            return await LoadOnceAsync(options, options.MaximumRetryAttempts, cancellationToken);
        }

        private async Task<BattleLoadResult> LoadOnceAsync(BattleLoadOptions options, int retryAttempt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int delayTimeBetweenHttpRequests = DelayTimeBetweenHttpRequestsBaseValue + retryAttempt * DelayTimeBetweenHttpRequestsAdditionalDelayPerRetryAttempt;
            Server server = serverResolver.Resolve(options.ConfiguredServer, options.GamePath);
            Server secondaryServer = options.SecondaryServer;
            JObject watchList = watchListRepository.Read();
            JObject arenaInfo = arenaInfoReader.ReadTempArenaInfoFile(options.Filename);

            string battleType = arenaInfo["matchGroup"]!.Value<string>()!;
            DateTimeOffset battleStartTime = DateTimeOffset.ParseExact(arenaInfo["dateTime"]!.Value<string>()!, "dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
            int playerCount = arenaInfo["vehicles"]!.Count();

            IPlayerStatsAdapter adapter = SelectAdapter(options.ApiType, server, options.SecondaryServerEnabled, secondaryServer);
            List<Task<List<Player>>> tasks = new();

            if (options.SecondaryServerEnabled)
            {
                tasks.Add(adapter.GetPlayersAsync(playerCount, 1, arenaInfo, server, delayTimeBetweenHttpRequests));
                tasks.Add(adapter.GetPlayersAsync(playerCount, 2, arenaInfo, secondaryServer, delayTimeBetweenHttpRequests));
            }
            else
            {
                tasks.Add(adapter.GetPlayersAsync(playerCount, 0, arenaInfo, server, delayTimeBetweenHttpRequests));
            }

            await Task.WhenAll(tasks);
            cancellationToken.ThrowIfCancellationRequested();

            List<Player> players = tasks[0].Result;
            if (options.SecondaryServerEnabled)
            {
                players = players.Concat(tasks[1].Result).ToList();
            }

            ApplyWatchList(players, watchList);

            Battlefield battlefield = battlefieldFactory.Create(battleType, battleStartTime, players);
            string outputText = TextUtils.GenerateGeneralStatisticsOutputText(battlefield);

            return new BattleLoadResult(battlefield, outputText, playerCount, server, secondaryServer);
        }

        private IPlayerStatsAdapter SelectAdapter(APIType requestedApiType, Server server, bool secondaryServerEnabled, Server secondaryServer)
        {
            bool wgPublicCanServeAllPlayers = server != Server.RU
                && server != Server.CN
                && (!secondaryServerEnabled || secondaryServer != Server.RU && secondaryServer != Server.CN);

            if (wgPublicCanServeAllPlayers && requestedApiType != APIType.VORTEX)
            {
                return playerStatsAdapters[requestedApiType];
            }

            return playerStatsAdapters[APIType.VORTEX];
        }

        private static void ApplyWatchList(IEnumerable<Player> players, JObject watchList)
        {
            foreach (Player player in players)
            {
                string serverName = ServerExt.GetNameByServer(player.Server);
                if (player.ID != "-1" && watchList[serverName]!.SelectToken(player.ID) != null)
                {
                    player.WatchStatus = WatchStatusExt.GetStatusByName(watchList[serverName]![player.ID]!["status"]!.Value<string>()!);
                }
            }
        }

        private static bool ShouldRetry(Exception ex)
        {
            return ex.Message != "FileFormatIncorrect" && ex.Message != "ServerAutoDetectionFailed";
        }
    }
}
