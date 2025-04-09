using static PersistentCosmetics.PersitentCosmeticsUtility;
using static PersistentCosmetics.OutfitVisualizerManager;

namespace PersistentCosmetics
{
    public class OutfitVisualizerPatches
    {

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
        [HarmonyPostfix]
        static void OnGameManagerAwakePost()
        {
            Plugin.__MenuObject.AddComponent<DaniTestDummySpawner>();
        }


        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnPlayer))]
        [HarmonyPrefix]
        static bool OnGameManagerSpawnPlayerPre(ulong __0)
        {
            if (__0 == 0) return true;
            else if (__0 < 3) return false;
            else return true;
        }

        [HarmonyPatch(typeof(DaniTestDummySpawner), nameof(DaniTestDummySpawner.Start))]
        [HarmonyPostfix]
        static void OnDaniTestDummySpawnerStartPost()
        {
            var allPlayers = GameObject.FindObjectsOfType<GameObject>()
                .Where(go => go.name.Contains("OnlinePlayer"))
                .OrderBy(go => go.transform.GetSiblingIndex())
                .ToList();

            var original = allPlayers[0];
            original.name = "OutfitPreviewPlayer";
            original.transform.position = new Vector3(1000f, 1000f, 1000f);

            outfitPreviewPlayer = GameObject.Instantiate(original);
            outfitPreviewPlayer.name = "OutfitPreviewPlayer(Clone)";

            var animator = outfitPreviewPlayer.GetComponent<OnlinePlayer>()?.animator;
            if (animator != null) GameObject.Destroy(animator);

            ApplyLastEquippedOutfitTo(outfitPreviewPlayer);

            GameManager.Instance.RemovePlayer(0);
        }
    }

    public class OutfitVisualizerManager : MonoBehaviour
    {
        public static GameObject outfitPreviewPlayer;
        private static Camera previewCamera;
        private static RenderTexture previewRenderTexture;
        private static RawImage previewUI;
        private static float orbitAngle = 0f;
        private static Vector3 orbitCenter;

        void Update()
        {
            if (menuTrigger)
            {
                if (previewCamera == null || previewUI == null) ShowPreviewForCurrentOutfit();

                if (outfitPreviewPlayer != null && previewCamera != null)
                {
                    outfitPreviewPlayer.SetActive(true);
                    previewUI.enabled = true;
                    previewCamera.enabled = true;

                    orbitAngle += orbitSpeed * Time.deltaTime;
                    float radius = 11f;
                    float camX = orbitCenter.x + Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * radius;
                    float camZ = orbitCenter.z + Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * radius;
                    float camY = orbitCenter.y + 0.5f;

                    previewCamera.transform.position = new Vector3(camX, camY, camZ);
                    previewCamera.transform.LookAt(orbitCenter);
                }
            }
            else
            {
                outfitPreviewPlayer?.SetActive(false);
                if (previewUI != null) previewUI.enabled = false;
                if (previewCamera != null) previewCamera.enabled = false;
            }
        }

        public static void ApplyLastEquippedOutfitTo(GameObject target)
        {
            if (target == null || lastEquippedOutfit == null || lastEquippedOutfit.Length == 0)
                return;

            var applier = target.GetComponent<MonoBehaviourPublicSkhafaSkshtobaSkMeMaUnique>();
            if (applier == null)
                return;

            var allCategories = new[] { "Hat", "Hair", "Face", "Shoes", "Backpack" };
            var categoriesApplied = new HashSet<string>();
            var items = ExtractCosmeticsFromOutfit(lastEquippedOutfit);

            foreach (var item in items)
            {
                string name = GetCosmeticNameFromCosmeticArray(item);
                int itemId = COSMETIC_DEFID_TO_NAME.FirstOrDefault(x => x.Value == name).Key;
                string color = GetCosmeticTag(item, "color");
                string shiny = GetCosmeticTag(item, "shiny");
                string brand = GetCosmeticTag(item, "brand");
                CosmeticItem cosmetic = MonoBehaviourPublicLi1CoalDi2InitCoUIUnique.itemIdToItem[itemId];
                string category = COSMETIC_DEFID_TO_CATEGORY[itemId];

                var tagDict = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                if (!string.IsNullOrEmpty(color)) tagDict.Add("color", color);
                if (!string.IsNullOrEmpty(shiny)) tagDict.Add("shiny", shiny);
                if (!string.IsNullOrEmpty(brand)) tagDict.Add("brand", brand);

                try
                {
                    switch (category)
                    {
                        case "Hat": applier.SetHat(cosmetic, tagDict); break;
                        case "Hair": applier.SetHair(cosmetic, tagDict); break;
                        case "Face": applier.SetFace(cosmetic, tagDict); break;
                        case "Shoes": applier.SetShoes(cosmetic, tagDict); break;
                        case "Backpack": applier.SetBackpack(cosmetic, tagDict); break;
                    }
                    categoriesApplied.Add(category);
                }
                catch (Exception) { }
            }

            foreach (var cat in allCategories.Except(categoriesApplied))
            {
                try
                {
                    switch (cat)
                    {
                        case "Hat": applier.SetHat(null, null); break;
                        case "Hair": applier.SetHair(null, null); break;
                        case "Face": applier.SetFace(null, null); break;
                        case "Shoes": applier.SetShoes(null, null); break;
                        case "Backpack": applier.SetBackpack(null, null); break;
                    }
                }
                catch (Exception) { }
            }
        }

        public static void ShowPreviewForCurrentOutfit()
        {
            if (outfitPreviewPlayer == null)
                return;

            outfitPreviewPlayer.SetActive(true);
            orbitCenter = outfitPreviewPlayer.transform.position;
            SetupPreviewUI();
        }

        private static void SetupPreviewUI()
        {
            if (previewRenderTexture == null)
            {
                previewRenderTexture = new RenderTexture(512, 512, 16);
                previewRenderTexture.Create();
            }

            if (previewCamera == null)
            {
                GameObject camObj = new("PreviewCamera");
                previewCamera = camObj.AddComponent<Camera>();
                previewCamera.targetTexture = previewRenderTexture;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = Color.clear;
                previewCamera.cullingMask = LayerMask.GetMask("Default");
                previewCamera.fieldOfView = 35f;
            }

            previewCamera.enabled = true;

            if (previewUI == null)
            {
                GameObject canvasObj = GameObject.Find("PersistentCosmeticsUI");
                if (canvasObj == null) return;

                GameObject rawImageObj = new GameObject("OutfitPreviewUI");
                rawImageObj.transform.SetParent(canvasObj.transform);
                previewUI = rawImageObj.AddComponent<RawImage>();
                previewUI.texture = previewRenderTexture;

                var rtTransform = previewUI.GetComponent<RectTransform>();
                rtTransform.anchorMin = new Vector2(1, 0);
                rtTransform.anchorMax = new Vector2(1, 0);
                rtTransform.pivot = new Vector2(1, 0);
                rtTransform.anchoredPosition = new Vector2(-300, 20);
                rtTransform.sizeDelta = new Vector2(300, 300);
            }

            previewUI.enabled = true;
        }
    }
}
