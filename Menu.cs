using static PersistentCosmetics.MenuUtility;
using static PersistentCosmetics.MenuManager;
using static PersistentCosmetics.MenuConstants;

using static PersistentCosmetics.PersitentCosmeticsUtility;

namespace PersistentCosmetics
{
    public class MenuConstants
    {
        public const string MENU_ON_MSG = "■<color=orange>PersistentCosmetics Menu <color=blue>ON</color></color>■";
        public const string MENU_OFF_MSG = "■<color=orange>PersistentCosmetics Menu <color=red>OFF</color></color>■";
    }

    public class MenuButton
    {
        public string Label { get; }
        public Action Action { get; }
        public Func<string> Status { get; }
        public List<MenuButton> SubMenu { get; }

        public bool IsScrollable { get; }
        private readonly Action<int> _setter;
        private readonly Func<int> _getter;

        public int ScrollValue => _getter != null ? _getter() : 0;

        private readonly Func<int> _scrollMinFunc;
        private readonly Func<int> _scrollMaxFunc;

        private readonly Func<MenuButton, bool, string> _customFormatter;

        public MenuButton(string label, Action action = null, Func<string> status = null,
                          List<MenuButton> subMenu = null,
                          bool isScrollable = false,
                          Action<int> setter = null, Func<int> getter = null,
                          Func<int> scrollMin = null, Func<int> scrollMax = null,
                          Func<MenuButton, bool, string> customFormatter = null)
        {
            Label = label;
            Action = action;
            Status = status;
            SubMenu = subMenu;

            IsScrollable = isScrollable;
            _setter = setter;
            _getter = getter;

            _scrollMin = scrollMin;
            _scrollMax = scrollMax;
            _customFormatter = customFormatter;
        }

        public void AdjustScrollValue(int delta)
        {
            if (IsScrollable && _getter != null && _setter != null && scrollMin.HasValue && scrollMax.HasValue)
            {
                int current = _getter();
                int step = 1;

                if (Label.Contains("Speed") || Label.Contains("Delay") || Label.Contains("ms"))
                {
                    int projected = current + delta;

                    if (projected >= 1000)
                        step = 1000;
                    else if (projected >= 50)
                        step = 50;
                    else 
                        step = 1;
                }

                int newValue = Mathf.Clamp(current + delta * step, scrollMin.Value, scrollMax.Value);
                _setter(newValue);
            }
        }



        public string GetFormattedLabel(bool isSelected)
        {
            if (_customFormatter != null)
                return _customFormatter(this, isSelected);

            string prefix = isSelected ? "■<color=yellow>" : "  ";
            string suffix = isSelected ? "</color>■" : "";
            string scrollableValue = IsScrollable ? $"<color=green>{_getter()}</color>" : "";
            return $"{prefix}{Label}{suffix} {Status?.Invoke()} {scrollableValue}";
        }

        public bool HasSubMenu => SubMenu != null && SubMenu.Any();

        private int? scrollMin => _scrollMin?.Invoke();
        private int? scrollMax => _scrollMax?.Invoke();

        private readonly Func<int> _scrollMin;
        private readonly Func<int> _scrollMax;
    }
    public class MenuManager : MonoBehaviour
    {
        public Text menuText;

        private int selectedIndex;
        public static List<MenuButton> currentMenu;
        private Stack<(List<MenuButton> menu, int index)> menuStack;
        public static bool scrollingMode = false;
        private float elapsedTime = 0f;

        void Awake()
        {
            LoadFromFile();
        }
        void Start()
        {
            menuStack = new Stack<(List<MenuButton>, int)>();

            currentMenu =
            [
                new("Persistent Cosmetics", subMenu: BuildItemBrowserMenu()),
            ];
        }

        void BackGroundColor(bool trigger)
        {
            if (trigger)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                GameObject.Find("GameUI/Pause/Overlay").SetActive(false);
                GameObject.Find("GameUI/Pause/Overlay/Menu").SetActive(true);
            }
            else
            {
                GameObject.Find("GameUI/Pause/Overlay").SetActive(true);
                GameObject.Find("GameUI/Pause/Overlay/Menu").SetActive(false);
            }
        }

       
        void Update()
        {
            elapsedTime += Time.deltaTime;

            if (Input.GetKeyDown(menuKey))
            {
                menuTrigger = !menuTrigger;
                menuText.text = menuTrigger ? MENU_ON_MSG : MENU_OFF_MSG;
                BackGroundColor(!menuTrigger);
                PlayMenuSound();
            }

            if (menuTrigger)
            {
                HandleNavigation();
                HandleSelection();
            }
            else menuText.text = "";


            if (loopMode && elapsedTime >= (loopDelay / 1000))
            {
                elapsedTime = 0f;

                var list = loopFavoritesOnly
                    ? GetAllItems().Where(i => favoriteOutfits.Contains(ComputeOutfitHash(i))).ToList()
                    : GetAllItems();

                if (list.Count > 0)
                {
                    var item = list[loopIndex % list.Count];
                    ClientSend.SendSerializedInventory(item, item.Length);
                    loopIndex++;
                }
            }
        }

        void FixedUpdate()
        {
            if (menuTrigger)
            {
                RenderMenu();
                BackGroundColor(!menuTrigger);
            }
        }

        void HandleNavigation()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            var selectedButton = currentMenu[selectedIndex];

            if (scrollingMode)
            {
                if (selectedButton.IsScrollable && Mathf.Abs(scroll) > 0f)
                {
                    selectedButton.AdjustScrollValue(scroll > 0f ? -1 : 1);
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
                currentMenu = BuildItemBrowserMenu();
                PlayMenuSound();
            }
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
            var totalItems = GetAllItems().Count;

            string sortDisplay = $"<color=orange>{currentSortMode}</color>";
            string header = $"      Outfits: <b>{totalItems}</b> | Sort: {sortDisplay} | Page: <b>{itemPageIndex + 1}</b>";

            string separator = "      " + new string('_', 100);

            var menuLines = currentMenu.Select((btn, index) =>
                "      " + btn.GetFormattedLabel(index == selectedIndex));

            menuText.text = $"\n{header}\n{separator}\n\n{string.Join("\n", menuLines)}";
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
            currentMenu = BuildItemBrowserMenu();

            return false;
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

        private static readonly HashSet<string> UnityRichTextColors =
        [
            "red", "green", "blue", "yellow", "black", "white", "grey",
            "cyan", "magenta", "gray", "orange", "purple", "brown"
        ];

        private static readonly Dictionary<string, string> RichTextColorFallbacks = new()
        {
            { "light blue", "cyan" },
            { "golden", "yellow" },
            { "pink", "magenta" },
            { "gray", "grey" },
            { "blond", "#FFFACD" }
        };

        public static List<MenuButton> BuildItemBrowserMenu()
        {
            List<Il2CppStructArray<byte>> allOutfits = GetAllItems();

            IEnumerable<Il2CppStructArray<byte>> sorted = currentSortMode switch
            {
                SortMode.Size => allOutfits.OrderByDescending(o => ExtractItemsFromOutfit(o).Count),
                SortMode.Favorite => allOutfits.OrderByDescending(o => favoriteOutfits.Contains(ComputeOutfitHash(o))),
                _ => allOutfits 
            };

            var sortedItems = sorted.ToList();
            int totalPages = Mathf.CeilToInt((float)sortedItems.Count / itemsPerPage);
            itemPageIndex = Mathf.Clamp(itemPageIndex, 0, Mathf.Max(0, totalPages - 1));

            var pageItems = sortedItems
                .Skip(itemPageIndex * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();

            var controlButtons = new List<MenuButton>
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
                            loopDelay = Mathf.Clamp(val, 16, 10000);
                        },
                        scrollMin: () => 16,
                        scrollMax: () => 10000,
                        customFormatter: (btn, selected) =>
                        {
                            string prefix = selected ? "■<color=yellow>" : "  ";
                            string suffix = selected ? "</color>■" : "";
                            return $"{prefix}{btn.Label}{suffix} <color=green>{(int)loopDelay}ms</color>";
                        }),

                    new("Scope: ", () =>
                    {
                        loopFavoritesOnly = !loopFavoritesOnly;
                        ForceMessage("Loop scope set to: " + (loopFavoritesOnly ? "Favorites" : "All"));
                    },
                    () => loopFavoritesOnly ? "<color=yellow>[Favorites]</color>" : "<color=grey>[All]</color>")
                ]),
                new("▸ Sort by Order", () => { currentSortMode = SortMode.Order; currentMenu = BuildItemBrowserMenu(); }),
                new("▸ Sort by Size", () => { currentSortMode = SortMode.Size; currentMenu = BuildItemBrowserMenu(); }),
                new("▸ Sort by Favorite", () => { currentSortMode = SortMode.Favorite; currentMenu = BuildItemBrowserMenu(); }),
                new("Page",
                    isScrollable: true,
                    getter: () => itemPageIndex + 1,
                    setter: (val) => { itemPageIndex = val - 1; currentMenu = BuildItemBrowserMenu(); },
                    scrollMin: () => 1,
                    scrollMax: () => totalPages,
                    customFormatter: (btn, selected) =>
                    {
                        string prefix = selected ? "■<color=yellow>" : "  ";
                        string suffix = selected ? "</color>■" : "";
                        return $"{prefix}{btn.Label}{suffix} <color=green>{itemPageIndex + 1}/{totalPages}</color>";
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
            var outfitItems = ExtractItemsFromOutfit(item);
            string hash = ComputeOutfitHash(item);
            bool isFavorite = favoriteOutfits.Contains(hash);
            string fav = isFavorite ? " <color=yellow>★</color>" : "";
            string customName = outfitNames.TryGetValue(hash, out var n) ? n : $"OUTFIT ({outfitItems.Count} item{(outfitItems.Count > 1 ? "s" : "")})";
            string label = $"<i><color=grey>➤ {customName}</color></i>{fav}";

            string status() =>
                "\n" + string.Join("\n", outfitItems.Select((i, idx) =>
                {
                    string name = GetItemNameFromCosmeticArray(i);
                    string color = GetItemTag(i, "color");
                    string shiny = GetItemTag(i, "shiny");
                    string brand = GetItemTag(i, "brand");

                    string coloredName = GetColoredText(color, $"<b>{name}</b>");
                    string safeShiny = string.IsNullOrWhiteSpace(shiny) ? "" : $"  {shiny}";
                    string brandInfo = string.IsNullOrWhiteSpace(brand) ? "" : $"  <color=grey>[{brand}]</color>";
                    string glyph = idx == outfitItems.Count - 1 ? "└─" : "├─";

                    return $"        {glyph} {coloredName}{safeShiny}{brandInfo}";
                }));

            return new MenuButton(label,
                status: status,
                subMenu: new List<MenuButton>
                {
            new(label, status: status),

            new("Equip", () =>
            {
                logOutfit = false;
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
                new("Cancel", () => currentMenu = BuildItemBrowserMenu())
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

                currentMenu = CreateOutfitButton(item).SubMenu;

            }),

            new("<color=red>Delete</color>", subMenu:
            [
                new("Cancel", () => currentMenu = BuildItemBrowserMenu()),
                new("<color=red>Confirm Delete</color>", () =>
                {
                    itemsList.RemoveAll(i => i.ToArray().SequenceEqual(item.ToArray()));
                    favoriteOutfits.Remove(hash);
                    outfitNames.Remove(hash);
                    SaveToFile();
                    ForceMessage("Outfit deleted.");
                    currentMenu = BuildItemBrowserMenu();
                })
            ])
                });
        }

        private static string GetColoredText(string color, string fallbackText = null)
        {
            if (string.IsNullOrWhiteSpace(color)) return fallbackText ?? "none";

            string raw = color.Trim().ToLower();
            string safe = raw.Replace(" ", "");

            if (safe == "black")
            {
                // Override pour éviter l'invisibilité
                return $"<color=grey>{fallbackText ?? raw}</color>";
            }

            if (!UnityRichTextColors.Contains(safe))
            {
                if (RichTextColorFallbacks.TryGetValue(raw, out string fallback))
                    return $"<color={fallback}>{fallbackText ?? raw}</color>";

                return $"<color=white>{fallbackText ?? raw}</color>";
            }

            return $"<color={safe}>{fallbackText ?? raw}</color>";
        }

    }
}


