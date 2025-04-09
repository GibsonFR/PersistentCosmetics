using static PersistentCosmetics.MenuUtility;
using static PersistentCosmetics.MenuManager;
using static PersistentCosmetics.MenuConstants;

using static PersistentCosmetics.PersitentCosmeticsUtility;
using Il2CppSystem.Dynamic.Utils;

namespace PersistentCosmetics
{
    public class MenuConstants
    {
        public const string MENU_ON_MSG = "■<color=orange>PersistentCosmetics Menu <color=blue>ON</color></color>■";
        public const string MENU_OFF_MSG = "■<color=orange>PersistentCosmetics Menu <color=red>OFF</color></color>■";

        public const string SELECTED_PREFIX = "■<color=yellow>";
        public const string SELECTED_SUFFIX = "</color>■";
        public const string UNSELECTED_PREFIX = "  ";
        public const string UNSELECTED_SUFFIX = "";
        public const string MENU_SEPARATOR_PREFIX = "      ";
        public const int MENU_SEPARATOR_LENGTH = 100;

        public const int SCROLL_STEP_OVER_1000 = 1000;
        public const int SCROLL_STEP_OVER_50 = 50;
        public const int SCROLL_STEP_DEFAULT = 1;
        public const int MIN_LOOP_DELAY = 16;
        public const int MAX_LOOP_DELAY = 10000;

        public static readonly Color TEXT_OUTLINE_COLOR = new(1f, 1f, 1f, 0.7f);
        public static readonly Vector2 TEXT_OUTLINE_DISTANCE_PRIMARY = new(0.25f, -0.25f);
        public static readonly Vector2 TEXT_OUTLINE_DISTANCE_SECONDARY = new(0.5f, -0.5f);
    }

    public class MenuButton(string label, Action action = null, Func<string> status = null,
                      List<MenuButton> subMenu = null,
                      bool isScrollable = false,
                      Action<int> setter = null, Func<int> getter = null,
                      Func<int> scrollMin = null, Func<int> scrollMax = null,
                      Func<MenuButton, bool, string> customFormatter = null)
    {
        public string Label { get; } = label;
        public Action Action { get; } = action;
        public Func<string> Status { get; } = status;
        public List<MenuButton> SubMenu { get; } = subMenu;

        public bool IsScrollable { get; } = isScrollable;

        public void AdjustScrollValue(int delta, bool stepMode)
        {
            if (IsScrollable && getter != null && setter != null && scrollMin.HasValue && scrollMax.HasValue)
            {
                int current = getter();
                int step = 1;

                if (stepMode)
                {
                    int nextValue = current + delta;

                    if (nextValue > 1000) step = SCROLL_STEP_OVER_1000;
                    else if (nextValue > 50) step = SCROLL_STEP_OVER_50;
                    else step = SCROLL_STEP_DEFAULT;
                }

                int newValue = Mathf.Clamp(current + delta * step, scrollMin.Value, scrollMax.Value);
                setter(newValue);
            }
        }



        public string GetFormattedLabel(bool isSelected)
        {
            if (customFormatter != null)
                return customFormatter(this, isSelected);

            string prefix = isSelected ? SELECTED_PREFIX : UNSELECTED_PREFIX;
            string suffix = isSelected ? SELECTED_SUFFIX : UNSELECTED_SUFFIX;
            string scrollableValue = IsScrollable ? $"<color=green>{getter()}</color>" : "";
            return $"{prefix}{Label}{suffix} {Status?.Invoke()} {scrollableValue}";
        }

        public bool HasSubMenu => SubMenu != null && SubMenu.Any();

        private int? scrollMin => _scrollMin?.Invoke();
        private int? scrollMax => _scrollMax?.Invoke();

        private readonly Func<int> _scrollMin = scrollMin;
        private readonly Func<int> _scrollMax = scrollMax;
    }

    public class MenuManager : MonoBehaviour
    {
        public Text menuText;
        public Text menuTextOutline;

        public static int selectedIndex;
        public static List<MenuButton> currentMenu;
        private Stack<(List<MenuButton> menu, int index)> menuStack;
        public static bool scrollingMode = false;
        private float elapsedTime = 0f;

        public static List<Il2CppStructArray<byte>> allItemsCache;
        public static List<Il2CppStructArray<byte>> favoritesCache;
        public static bool cacheDirty = true;

        void Awake()
        {
            LoadFromFile();
            SaveToFile();

            RefreshMainMenu();

            if (autoEquipLastOutfit && currentSelectedOutfit.Length > 50) ClientSend.SendSerializedInventory(currentSelectedOutfit, currentSelectedOutfit.Length); 
        }

        void Start()
        {
            menuStack = new Stack<(List<MenuButton>, int)>();
            RefreshMainMenu();
            EnsureTextOutline(menuTextOutline);
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;

            if (cacheDirty) RebuildCaches();

            if (Input.GetKeyDown(menuKey))
            {
                menuTrigger = !menuTrigger;
                menuText.text = menuTrigger ? MENU_ON_MSG : MENU_OFF_MSG;
                ToggleBackgroundOverlay(menuTrigger);
                PlayMenuSound();
            }

            if (menuTrigger)
            {
                HandleNavigation();
                HandleSelection();
            }
            else
            {
                menuText.text = "";
                menuTextOutline.text = "";
            }
        }

        void FixedUpdate()
        {
            if (menuTrigger)
            {
                RenderMenu();
                ToggleBackgroundOverlay(menuTrigger);
            }

            if (loopMode && elapsedTime >= (loopDelay / 1000f))
            {
                elapsedTime = 0f;
                var list = loopFavoritesOnly ? favoritesCache : allItemsCache;
                if (list.Count > 0)
                {
                    var item = list[loopIndex % list.Count];
                    currentSelectedOutfit = item;
                    ClientSend.SendSerializedInventory(item, item.Length);
                    loopIndex++;
                }
            }
        }

        public static void RefreshMainMenu()
        {
            scrollingMode = false;
            selectedIndex = 0;
            RebuildCaches();
            currentMenu = BuildCosmeticBrowserMenu();
        }

        void HandleNavigation()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            var selectedButton = currentMenu[selectedIndex];

            if (scrollingMode)
            {
                if (selectedButton.IsScrollable && Mathf.Abs(scroll) > 0f)
                {
                    selectedButton.AdjustScrollValue(scroll > 0f ? -1 : 1, true);
                    PlayMenuSound();
                }

                if (Input.GetMouseButtonDown(1))
                {
                    scrollingMode = false;
                    PlayMenuSound();
                }

                return;
            }

            if (scroll > 0f)
            {
                selectedIndex = (selectedIndex - 1 + currentMenu.Count) % currentMenu.Count;
                PlayMenuSound();
            }
            else if (scroll < 0f)
            {
                selectedIndex = (selectedIndex + 1) % currentMenu.Count;
                PlayMenuSound();
            }

            if (selectedButton.IsScrollable && Input.GetMouseButtonDown(1))
            {
                scrollingMode = true;
                PlayMenuSound();
            }

            if (Input.GetMouseButtonDown(2) && menuStack.Count > 0)
            {
                (currentMenu, selectedIndex) = menuStack.Pop();
                scrollingMode = false;
                PlayMenuSound();
            }
        }

        private void EnsureTextOutline(Text text)
        {
            if (text == null) return;

            var existingOutline = text.GetComponent<Outline>();
            if (existingOutline == null)
            {
                var outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = TEXT_OUTLINE_COLOR;
                outline.effectDistance = TEXT_OUTLINE_DISTANCE_PRIMARY;
                outline.useGraphicAlpha = true;
            }
            else
            {
                existingOutline.effectColor = TEXT_OUTLINE_COLOR;
                existingOutline.effectDistance = TEXT_OUTLINE_DISTANCE_PRIMARY;
            }
        }

        void ToggleBackgroundOverlay(bool enable)
        {
            if (enable)
            {
                GameObject.Find("GameUI/Pause/Overlay").SetActive(true);
                GameObject.Find("GameUI/Pause/Overlay/Menu").SetActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                GameObject.Find("GameUI/Pause/Overlay").SetActive(false);
                GameObject.Find("GameUI/Pause/Overlay/Menu").SetActive(true);
            }
        }

        public static void RebuildCaches()
        {
            var all = GetAllCosmetics().ToList();
            allItemsCache = all;
            favoritesCache = all.Where(i => favoriteOutfits.Contains(ComputeOutfitHash(i))).ToList();
            cacheDirty = false;
        }

        void HandleSelection()
        {
            if (Input.GetMouseButtonDown(1))
            {
                var selectedButton = currentMenu[selectedIndex];
                if (selectedButton.HasSubMenu)
                {
                    menuStack.Push((currentMenu, selectedIndex));
                    currentMenu = selectedButton.SubMenu;
                    selectedIndex = 0;
                }
                else
                    selectedButton.Action?.Invoke();

                PlayMenuSound();
            }
        }
        void RenderMenu()
        {
            int totalOutfits = allItemsCache?.Count ?? 0;

            string sortDisplay = $"<color=orange>{currentSortMode}</color>";
            string header = $"      Outfits: <b>{totalOutfits}</b> | Sort: {sortDisplay} | Page: <b>{outfitPageIndex + 1}</b>";

            string separator = MENU_SEPARATOR_PREFIX + new string('_', MENU_SEPARATOR_LENGTH);

            var menuLines = currentMenu.Select((btn, index) =>
                "      " + btn.GetFormattedLabel(index == selectedIndex));

            string fullText = $"\n{header}\n{separator}\n\n{string.Join("\n", menuLines)}";

            menuText.text = RemoveNoblurTags(fullText);
            menuTextOutline.text = ExtractNoblurText(fullText);
        }

        private string ExtractNoblurText(string input)
        {
            string marked = System.Text.RegularExpressions.Regex.Replace(
                input,
                @"<noblur>(.*?)</noblur>",
                "<!NOBLUR_START!>$1<!NOBLUR_END!>",
                System.Text.RegularExpressions.RegexOptions.Singleline
            );

            var lines = marked.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<!NOBLUR_START!>"))
                {
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(
                        lines[i],
                        @"<!NOBLUR_START!>(.*?)<!NOBLUR_END!>",
                        "<color=black>$1</color>"
                    );
                }
                else
                {
                    lines[i] = new string(' ', lines[i].Length); 
                }
            }

            return string.Join("\n", lines);
        }

        private string RemoveNoblurTags(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                @"<noblur>(.*?)</noblur>",
                "$1",
                System.Text.RegularExpressions.RegexOptions.Singleline
            );
        }
    }

    public class MenuPatches
    {
        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.SendMessage), [typeof(string)])]
        [HarmonyPrefix]
        public static bool OnSendMessage(string __0)
        {
            if (string.IsNullOrEmpty(waitingForRenameHash)) return true;

            string newName = __0.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                outfitNames[waitingForRenameHash] = newName;
                ForceMessage("Outfit renamed to: " + newName);
            }
            else
            {
                ForceMessage("Rename cancelled (empty name).");
            }

            waitingForRenameHash = null;
            SaveToFile();
            RefreshMainMenu();

            quitChat();
            return false;
        }

        public static void quitChat()
        {
            ChatBox.Instance.field_Private_Boolean_0 = false; // typing boolean
            ChatBox.Instance.inputField.text = "";
            ChatBox.Instance.inputField.interactable = false;
        }
    }

    public class MenuUtility
    {
        public static void ToggleBoolean(ref bool trigger, string triggerName)
        {
            trigger = !trigger;
            ForceMessage(trigger ? $"{triggerName} <color=blue>ON</color>" : $"{triggerName} <color=red>OFF</color>");
        }
        public enum SortMode
        {
            Order,
            Size,
            Favorite
        }

        public static readonly HashSet<string> UNITY_RICH_TEXT_COLORS = new()
        {
            "red", "green", "blue", "yellow", "black", "white", "grey",
            "cyan", "magenta", "gray", "orange", "purple", "brown"
        };

        public static readonly Dictionary<string, string> COLOR_FALLBACKS = new()
        {
            { "light blue", "cyan" },
            { "golden", "yellow" },
            { "pink", "magenta" },
            { "gray", "grey" },
            { "blond", "#EBD672" },
            { "creme", "#EBD672" }
        };

        public static string FormatMenuLabel(string label, bool selected, string extra = "")
        {
            string prefix = selected ? SELECTED_PREFIX : UNSELECTED_PREFIX;
            string suffix = selected ? SELECTED_SUFFIX : UNSELECTED_SUFFIX;
            return $"{prefix}{label}{suffix} {extra}";
        }

        public static List<MenuButton> BuildCosmeticBrowserMenu()
        {
            List<Il2CppStructArray<byte>> allOutfits = allItemsCache ?? [];

            IEnumerable<Il2CppStructArray<byte>> sorted = currentSortMode switch
            {
                SortMode.Size => allOutfits.OrderByDescending(o => ExtractCosmeticsFromOutfit(o).Count),
                SortMode.Favorite => allOutfits
                    .OrderByDescending(o => favoriteOutfits.Contains(ComputeOutfitHash(o)))
                    .ThenByDescending(o => ExtractCosmeticsFromOutfit(o).Count),
                _ => allOutfits
            };

            List<Il2CppStructArray<byte>> sortedItems = sorted.ToList();
            int totalPages = Mathf.CeilToInt((float)sortedItems.Count / outfitsPerPage);
            outfitPageIndex = Mathf.Clamp(outfitPageIndex, 0, Mathf.Max(0, totalPages - 1));

            List<Il2CppStructArray<byte>> pageItems = sortedItems
                .Skip(outfitPageIndex * outfitsPerPage)
                .Take(outfitsPerPage)
                .ToList();

            List<MenuButton> controlButtons = new List<MenuButton>
            {
                new("Log Outfit", () =>
                {
                    ToggleBoolean(ref logOutfit, "Log Outfit");
                    Packet packet = new((int)ServerSendType.requestCosmetics);
                    packet.Method_Public_Void_UInt64_0(clientId);
                    ClientHandle.ServerRequestsCosmetics(packet);

                }, () => logOutfit ? "<color=blue>ON</color>" : "<color=red>OFF</color>"),
                new("Open Inventory", () => {
                    GameObject.Find("GameUI/Pause/Overlay/Inventory").SetActive(true);

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }),
                new("Loop", subMenu:
                [
                    new("Loop Outfits", () => ToggleBoolean(ref loopMode, "Loop Mode"),
                        () => loopMode ? "<color=blue>ON</color>" : "<color=red>OFF</color>"),

                    new("Speed (ms)",
                        isScrollable: true,
                        getter: () => (int)loopDelay,
                        setter: (val) =>
                        {
                            loopDelay = Mathf.Clamp(val, MIN_LOOP_DELAY, MAX_LOOP_DELAY);
                        },
                        scrollMin: () => MIN_LOOP_DELAY,
                        scrollMax: () => MAX_LOOP_DELAY,
                        customFormatter: (btn, selected) => FormatMenuLabel(btn.Label, selected, $"<color=green>{(int)loopDelay}ms</color>")
                    ),

                    new("Scope: ", () =>
                    {
                        loopFavoritesOnly = !loopFavoritesOnly;
                        ForceMessage("Loop scope set to: " + (loopFavoritesOnly ? "Favorites" : "All"));
                    },
                    () => loopFavoritesOnly ? "<color=yellow>[Favorites]</color>" : "<color=grey>[All]</color>")
                ]),
                new("▸ Sort by Order", () => {
                    currentSortMode = SortMode.Order;
                    currentMenu = BuildCosmeticBrowserMenu();
                }),
                new("▸ Sort by Size", () => {
                    currentSortMode = SortMode.Size;
                    currentMenu = BuildCosmeticBrowserMenu();
                }),
                new("▸ Sort by Favorite", () => {
                    currentSortMode = SortMode.Favorite;
                    currentMenu = BuildCosmeticBrowserMenu();
                }),
                new("Page",
                    isScrollable: true,
                    getter: () => outfitPageIndex + 1,
                    setter: (val) => { outfitPageIndex = val - 1; currentMenu = BuildCosmeticBrowserMenu(); },
                    scrollMin: () => 1,
                    scrollMax: () => totalPages,
                    customFormatter: (btn, selected) =>
                    {
                        string prefix = selected ? "■<color=yellow>" : "  ";
                        string suffix = selected ? "</color>■" : "";
                        return $"{prefix}{btn.Label}{suffix} <color=green>{outfitPageIndex + 1}/{totalPages}</color>";
                    }),
                new("<color=grey><i>───── OUTFITS ─────</i></color>")
            };

            var itemButtons = pageItems.SelectMany(item =>
                new List<MenuButton>
                {
                    CreateOutfitButton(item),
                    new("") 
                }
            ).ToList();


            return controlButtons.Concat(itemButtons).ToList();
        }

        public static MenuButton CreateOutfitButton(Il2CppStructArray<byte> item)
        {
            var outfitItems = ExtractCosmeticsFromOutfit(item);
            string hash = ComputeOutfitHash(item);
            bool isFavorite = favoriteOutfits.Contains(hash);
            string fav = isFavorite ? " <color=yellow>★</color>" : "";
            string customName = outfitNames.TryGetValue(hash, out var n) ? n : $"OUTFIT ({outfitItems.Count} item{(outfitItems.Count > 1 ? "s" : "")})";
            string label = $"<i><color=grey>➤ {customName}</color></i>{fav}";

            string status() =>
                "\n" + string.Join("\n", outfitItems.Select((i, idx) =>
                {
                    string name = GetCosmeticNameFromCosmeticArray(i);
                    string color = GetCosmeticTag(i, "color");
                    string shiny = GetCosmeticTag(i, "shiny");
                    string brand = GetCosmeticTag(i, "brand");

                    string coloredName = GetColoredText(color, $"<b>{name}</b>");
                    string safeShiny = string.IsNullOrWhiteSpace(shiny) ? "" : $"  {shiny}";
                    string brandInfo = string.IsNullOrWhiteSpace(brand) ? "" : $"  <color=grey>[{brand}]</color>";
                    string glyph = idx == outfitItems.Count - 1 ? "└─" : "├─";

                    return $"        {glyph} {coloredName}{safeShiny}{brandInfo}";
                }));

            return new MenuButton(label,
            status: status,
            subMenu:
            [
                new(label, status: status),

                new("Equip", () =>
                {
                    logOutfit = false;
                    currentSelectedOutfit = item;
                    ClientSend.SendSerializedInventory(item, item.Length);
                    ForceMessage("Equipped outfit.");
                }),

                new("Rename", subMenu:
                [
                    new("<i>Rename Outfit (type in chat)</i>", () =>
                    {
                        waitingForRenameHash = hash;
                        ForceMessage("Now type the new outfit name in chat.");
                    }),
                    new("Cancel", () => RefreshMainMenu())
                ]),

                new("<color=yellow>★</color> Toggle Favorite", () =>
                {
                    if (favoriteOutfits.Contains(hash))
                    {
                        favoriteOutfits.Remove(hash);
                        ForceMessage("Removed from favorites.");
                    }
                    else
                    {
                        favoriteOutfits.Add(hash);
                        ForceMessage("Marked as favorite.");
                    }
                    SaveToFile();

                    cacheDirty = true;

                    currentMenu = CreateOutfitButton(item).SubMenu;
                }),

                new("<color=red>Delete</color>", subMenu:
                [
                    new("Cancel", () => RefreshMainMenu()),
                    new("<color=red>Confirm Delete</color>", () =>
                    {
                        cosmeticsList.RemoveAll(i => i.ToArray().SequenceEqual(item.ToArray()));
                        favoriteOutfits.Remove(hash);
                        outfitNames.Remove(hash);
                        SaveToFile();

                        ForceMessage("Outfit deleted.");
                        RefreshMainMenu();
                    })
                ])
            ]);
        }

        private static string GetColoredText(string color, string fallbackText = null)
        {
            if (string.IsNullOrWhiteSpace(color)) return fallbackText ?? "none";

            string raw = color.Trim().ToLower();

            if (raw == "black")
            {
                return $"<noblur><color=black>{fallbackText ?? raw}</color></noblur>";
            }

            if (!UNITY_RICH_TEXT_COLORS.Contains(raw))
            {
                if (COLOR_FALLBACKS.TryGetValue(raw, out var fallback))
                    return $"<color={fallback}>{fallbackText ?? raw}</color>";
                return $"<color=white>{fallbackText ?? raw}</color>";
            }

            return $"<color={raw}>{fallbackText ?? raw}</color>";
        }

    }
}