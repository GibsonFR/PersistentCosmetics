global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
global using System.Reflection;
global using UnityEngine;
global using UnhollowerRuntimeLib;
global using System.Globalization;
global using System.Text;
global using UnityEngine.UI;
global using UnhollowerBaseLib;
global using UnityEngine.Events;

global using static PersistentCosmetics.Variables;
global using static PersistentCosmetics.Utility;

namespace PersistentCosmetics
{
    [BepInPlugin("9B8711D3-536E-4BB7-AD83-08F7EC9F02AJ", "PersistentCosmetics", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance;
        public override void Load()
        {
            Instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<MainManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony harmony = new("gibson.persistent.cosmetics");
            harmony.PatchAll(typeof(MainPatches));
            harmony.PatchAll(typeof(PersitentCosmeticsPatches));
            harmony.PatchAll(typeof(MenuPatches));
            harmony.PatchAll(typeof(Patches));

            CreateFolder(mainFolderPath);

            CreateFile(logFilePath);
            ResetFile(logFilePath);

            CreateFile(configFilePath);
            SetConfigFile(configFilePath);

            Log.LogInfo("Mod created by Gibson, discord : gib_son, github : GibsonFR");
        }

        [HarmonyPatch(typeof(GameUI), nameof(GameUI.Awake))]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject pluginObj = new();

            Text text = pluginObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.supportRichText = true;
            text.raycastTarget = false;

            MenuManager menu = pluginObj.AddComponent<MenuManager>();
            menu.menuText = text;

            pluginObj.AddComponent<MainManager>();

            pluginObj.transform.SetParent(__instance.transform);
            pluginObj.transform.localPosition = new(pluginObj.transform.localPosition.x, -pluginObj.transform.localPosition.y, pluginObj.transform.localPosition.z);
            RectTransform rt = pluginObj.GetComponent<RectTransform>();
            rt.pivot = new(0, 1);
            rt.sizeDelta = new(1920, 1080);

        }

        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(MethodBase __originalMethod)
        {
            return false;
        }
    }
}