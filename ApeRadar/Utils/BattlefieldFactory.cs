using ApeRadar.Models;
using System;
using System.Collections.Generic;

namespace ApeRadar.Utils
{
    internal sealed class BattlefieldFactory
    {
        public Battlefield Create(string battleType, DateTimeOffset battleStartTime, List<Player> players)
        {
            return new Battlefield(battleType, battleStartTime, players);
        }
    }
}
