using SteamworksNative;
using System.Diagnostics;
using System.Security.Cryptography;
using static PersistentCosmetics.MainConstants;
using static PersistentCosmetics.MainUtility;
using static System.Net.WebRequestMethods;

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

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdatePost()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            {
                foreach (var inventory in MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.otherPlayerInventories)
                {
                    Plugin.Instance.Log.LogInfo(BitConverter.ToString(inventory.Value.field_Public_ArrayOf_Byte_0));
                }
            }
        }
    }
    public class MainUtility
    {
       
    }
}
