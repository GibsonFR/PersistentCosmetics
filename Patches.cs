
namespace PersistentCosmetics
{
    public class Patches
    {
        [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.Awake))]
        [HarmonyPrefix]
        static void OnPlayerInventoryAwakePre(PlayerInventory __instance)
        {
            __PlayerInventory = __instance;
        }
    }
}
