using ApeRadar.Models;
using System.Windows;

namespace ApeRadar.Utils
{
    static internal class ShipInfoUtils
    {
        private static readonly ShipCatalog catalog = new();

        public static void ReadShipInfoFile(string filename)
        {
            catalog.Load(filename);
        }

        public static string GetShipInfoVersion()
        {
            return catalog.Version;
        }

        public static string GetShipInfoDate()
        {
            return catalog.Date;
        }

        public static string GetShipNameByID(string ID, Language language)
        {
            string strUnknownShip = Application.Current?.TryFindResource("StringUnknownShip") as string ?? "Unknown Ship";
            return catalog.GetShipNameByID(ID, language, strUnknownShip);
        }

        public static string GetShipTypeByID(string ID)
        {
            return catalog.GetShipTypeByID(ID);
        }

        public static int GetShipTierByID(string ID)
        {
            return catalog.GetShipTierByID(ID);
        }
    }
}
