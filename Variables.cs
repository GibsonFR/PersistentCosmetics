using static PersistentCosmetics.MenuUtility;

namespace PersistentCosmetics
{
    public static class Variables
    {
        // folder
        public static string assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"PersistentCosmetics\"; 

        // file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string configFilePath = mainFolderPath + "config.txt";
        public static string persistentCosmeticsFilePath = mainFolderPath + @"cosmeticsDatabase";

        public static PlayerInventory __PlayerInventory;

        // List
        public static readonly List<Il2CppStructArray<byte>> cosmeticsList = [];
  
        // string
        public static string menuKey, waitingForRenameHash = null;

        // int 
        public static int outfitPageIndex = 0, outfitsPerPage = 4, loopIndex = 0;

        //float 
        public static float loopDelay = 1000f, orbitSpeed = 72f;

        // ulong
        public static ulong clientId, hostId;

        // bool
        public static bool configOnStart, menuTrigger, logOutfit, loopMode, loopFavoritesOnly, autoEquipLastOutfit = true;

        // SortMode
        public static SortMode currentSortMode = SortMode.Order;

        public static Il2CppStructArray<byte> lastEquippedOutfit, currentSelectedOutfit;
    }
}
