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
    [BepInPlugin("9B8711D3-536E-4BB7-AD83-08F7EC9F02AJ", "PersistentCosmetics", "1.1.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance;
        public static GameObject __MenuObject = null;
        public override void Load()
        {
            Instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<MainManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();
            ClassInjector.RegisterTypeInIl2Cpp<OutfitVisualizerManager>();

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony harmony = new("gibson.persistent.cosmetics");
            harmony.PatchAll(typeof(MainPatches));
            harmony.PatchAll(typeof(PersitentCosmeticsPatches));
            harmony.PatchAll(typeof(MenuPatches));
            harmony.PatchAll(typeof(OutfitVisualizerPatches));
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
            GameObject pluginObj = new("PersistentCosmeticsUI");
            pluginObj.transform.SetParent(__instance.transform, false);

            RectTransform rt = pluginObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(10, -10);
            rt.sizeDelta = new Vector2(1920, 1080);

            GameObject mainTextObj = new("MainText");
            mainTextObj.transform.SetParent(pluginObj.transform, false);
            Text text = mainTextObj.AddComponent<Text>();
            text.canvas.pixelPerfect = true;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform textRT = mainTextObj.GetComponent<RectTransform>();
            textRT.anchorMin = rt.anchorMin;
            textRT.anchorMax = rt.anchorMax;
            textRT.pivot = rt.pivot;
            textRT.anchoredPosition = rt.anchoredPosition;
            textRT.sizeDelta = rt.sizeDelta;

            GameObject outlineTextObj = new("OutlineText");
            outlineTextObj.transform.SetParent(pluginObj.transform, false);
            Text outlineText = outlineTextObj.AddComponent<Text>();
            outlineText.canvas.pixelPerfect = true;
            outlineText.font = text.font;
            outlineText.fontSize = text.fontSize;
            outlineText.fontStyle = text.fontStyle;
            outlineText.supportRichText = true;
            outlineText.raycastTarget = false;
            outlineText.alignment = text.alignment;
            outlineText.horizontalOverflow = text.horizontalOverflow;
            outlineText.verticalOverflow = text.verticalOverflow;
            RectTransform outlineRT = outlineTextObj.GetComponent<RectTransform>();
            outlineRT.anchorMin = rt.anchorMin;
            outlineRT.anchorMax = rt.anchorMax;
            outlineRT.pivot = rt.pivot;
            outlineRT.anchoredPosition = rt.anchoredPosition;
            outlineRT.sizeDelta = rt.sizeDelta;

            Outline outline = outlineTextObj.AddComponent<Outline>();
            outline.effectDistance = new Vector2(0.75f, -0.75f);
            outline.effectColor = new Color(1f, 1f, 1f, 1f);

            MenuManager menu = pluginObj.AddComponent<MenuManager>();
            menu.menuText = text;
            menu.menuTextOutline = outlineText;

            pluginObj.AddComponent<MainManager>();
            pluginObj.AddComponent<OutfitVisualizerManager>();

            __MenuObject = pluginObj;
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