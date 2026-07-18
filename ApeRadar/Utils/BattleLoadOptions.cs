using ApeRadar.Models;
using System;

namespace ApeRadar.Utils
{
    internal sealed class BattleLoadOptions
    {
        public required string Filename { get; init; }
        public required string GamePath { get; init; }
        public required Server ConfiguredServer { get; init; }
        public required bool SecondaryServerEnabled { get; init; }
        public required Server SecondaryServer { get; init; }
        public required APIType ApiType { get; init; }
        public required int MaximumRetryAttempts { get; init; }
        public Action<int>? RetryStarted { get; init; }

        public static BattleLoadOptions FromSettings(string filename, Action<int>? retryStarted = null)
        {
            return new BattleLoadOptions
            {
                Filename = filename,
                GamePath = Properties.Settings.Default.GamePath,
                ConfiguredServer = ServerExt.GetServerByName(Properties.Settings.Default.Server),
                SecondaryServerEnabled = Properties.Settings.Default.SecondaryServerEnabled,
                SecondaryServer = ServerExt.GetServerByName(Properties.Settings.Default.SecondaryServer),
                ApiType = APITypeExt.GetAPITypeByName(Properties.Settings.Default.APITypeSelection),
                MaximumRetryAttempts = Properties.Settings.Default.MaximumRetryAttemptsOnError,
                RetryStarted = retryStarted
            };
        }
    }
}
