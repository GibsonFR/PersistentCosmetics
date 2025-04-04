namespace PersistentCosmetics
{
    public class MainConstants
    {

    }

    public class MainManager : MonoBehaviour
    {
        /// <summary>
        /// Reads the configuration file to ensure up-to-date settings each Round.
        /// </summary>
        void Awake()
        {
            ReadConfigFile();
        }
    }

    public class MainPatches
    {
        /// <summary>
        /// Retrieves and sets the Steam ID of the mod owner when SteamManager initializes.
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwakePost(SteamManager __instance)
        {
            if (clientId != 0) return;
            clientId = (ulong)__instance.field_Private_CSteamID_0;

            persistentCosmeticsFilePath = $"{persistentCosmeticsFilePath}_{clientId}.txt";
        }
    }
    public class MainUtility
    {
       
    }
}
