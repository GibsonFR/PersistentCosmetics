using static PersistentCosmetics.PersitentCosmeticsUtility;

namespace PersistentCosmetics
{
    public class PersitentCosmeticsPatches
    {
        [HarmonyPatch(typeof(ClientSend), nameof(ClientSend.SendSerializedInventory))]
        [HarmonyPrefix]
        static void OnClientSendSendSerializedInventory(Il2CppStructArray<byte> __0)
        {
            logOutfit = false;
            LoadFromFile();       
            AddUnique(__0);
            SaveToFile();

            MenuManager.currentMenu = MenuUtility.BuildItemBrowserMenu();
        }
    }

    public static class PersitentCosmeticsUtility
    {
        public static HashSet<string> favoriteOutfits = [];
        public static Dictionary<string, string> outfitNames = [];
        public static void LoadFromFile()
        {
            itemsList.Clear();
            outfitNames.Clear();
            favoriteOutfits.Clear();

            if (!File.Exists(persistentCosmeticsFilePath))
                return;

            foreach (var line in File.ReadAllLines(persistentCosmeticsFilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(new[] { "::" }, StringSplitOptions.None);
                string hex = parts[0];

                byte[] bytes = HexStringToByteArray(hex);
                var item = new Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                    item[i] = bytes[i];

                itemsList.Add(item);
                string hash = ComputeOutfitHash(item);

                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i] == "FAV")
                        favoriteOutfits.Add(hash);
                    else
                        outfitNames[hash] = parts[i]; 
                }
            }
        }

        public static void SaveToFile()
        {
            using (StreamWriter writer = new StreamWriter(persistentCosmeticsFilePath, false))
            {
                foreach (var item in itemsList)
                {
                    string hex = BitConverter.ToString(item.ToArray()).Replace("-", "");
                    string hash = ComputeOutfitHash(item);

                    List<string> metadata = new();

                    if (outfitNames.TryGetValue(hash, out string name))
                        metadata.Add(name);

                    if (favoriteOutfits.Contains(hash))
                        metadata.Add("FAV");

                    if (metadata.Count > 0)
                        writer.WriteLine($"{hex}::{string.Join("::", metadata)}");
                    else
                        writer.WriteLine(hex);
                }
            }
        }



        public static string ComputeOutfitHash(Il2CppStructArray<byte> data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hash = sha.ComputeHash(data.ToArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void AddUnique(Il2CppStructArray<byte> item)
        {
            bool exists = itemsList.Any(existing =>
                existing.Length == item.Length &&
                existing.Where((t, i) => t != item[i]).Count() == 0
            );

            if (exists)
            {
                ForceMessage("Outfit already exists.");
                return;
            }

            itemsList.Add(item);
            SaveToFile(); 
            ForceMessage("New Outfit Logged.");
        }


        public static string GetItemId(Il2CppStructArray<byte> data)
        {
            string ascii = Encoding.ASCII.GetString(data.ToArray());
            string key = "itemid\u0000";
            int index = ascii.IndexOf(key);
            if (index == -1) return null;

            index += key.Length;
            int end = ascii.IndexOf('\0', index);
            if (end == -1) end = ascii.Length;

            return ascii.Substring(index, end - index);
        }

        public static List<Il2CppStructArray<byte>> GetAllItems() => itemsList;

        private static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] data = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return data;
        }

        public static string GetItemNameFromCosmeticArray(Il2CppStructArray<byte> data)
        {
            byte[] bytes = data.ToArray();
            string ascii = Encoding.ASCII.GetString(bytes);

            string key = "itemdefid\u0000";
            int index = ascii.IndexOf(key);
            if (index == -1) return "Unknown (no itemdefid)";

            int start = index + key.Length;
            int end = ascii.IndexOf('\0', start);
            if (end == -1) end = ascii.Length;

            string itemdefidStr = ascii.Substring(start, end - start);

            if (int.TryParse(itemdefidStr, out int itemdefid))
            {
                return cosmeticsName.TryGetValue(itemdefid, out var name)
                    ? name
                    : $"Unknown (itemdefid: {itemdefid})";
            }

            return "Invalid itemdefid format";
        }

        public static Dictionary<int, string> cosmeticsName = new()
        {
            { 611, "Christmas Socks" }, { 223, "Elf Hat" }, { 610, "Elf Shoes" }, { 420, "Gingerbread Mask" },
            { 612, "Lucky Boots" }, { 222, "Lucky Hat" }, { 11, "Messy" }, { 219, "Party Hat" },
            { 12, "Protagonist" }, { 419, "Pumpkin" }, { 417, "Santa's Beard" }, { 220, "Santa's Hat" },
            { 418, "Scarf" }, { 1008, "ChristmasBox" }, { 1000, "Crab Box 1" }, { 1001, "Crab Box 2" },
            { 1002, "Crab Box 3" }, { 1003, "Crab Box 4" }, { 999, "Crab Box Box" }, { 1005, "Present" },
            { 412, "Balaclava" }, { 404, "Bandit" }, { 411, "CD" }, { 408, "Chad" },
            { 403, "Clown Nose" }, { 414, "Dani Special" }, { 416, "DaniHead" }, { 415, "Dream Mask" },
            { 405, "FaceMask" }, { 407, "Flushed" }, { 413, "Gasmask" }, { 400, "Glasses" },
            { 402, "Headband" }, { 401, "Horseshoe" }, { 409, "Moustache" }, { 410, "Sunglasses" },
            { 406, "Van Dyke" }, { 7, "Baldo" }, { 10, "Bowl Cut" }, { 4, "Buzz Hair" },
            { 3, "Flat Top" }, { 1, "Jeff Hair" }, { 9, "Levi" }, { 6, "Malding" },
            { 5, "Mohawk Hair" }, { 2, "Ponytail" }, { 8, "Yakuza" }, { 216, "Golden Fedora" },
            { 200, "Baseball Cap" }, { 203, "Beanie" }, { 215, "Brick" }, { 209, "Bucket" },
            { 207, "Fisherman" }, { 212, "Cat ears" }, { 206, "Chefs hat" }, { 201, "Cool cap" },
            { 205, "Cowboy Hat" }, { 204, "Detective Hat" }, { 208, "Headset" }, { 213, "Milk" },
            { 214, "RobinHood" }, { 210, "Viking" }, { 202, "Vizor" }, { 211, "Wizzard" },
            { 218, "Fedora" }, { 217, "Gucci Banana" }, { 608, "Block Boots" }, { 607, "Color Block" },
            { 604, "Rocky" }, { 605, "Ground Force1" }, { 602, "Sandals" }, { 601, "Slippers" },
            { 600, "Sneakers" }, { 606, "Socks and Sandals" }, { 603, "Socks" }, { 609, "Wing Shoes" },
            { 801, "DaniSuit" }, { 0, "DefaultTop" }, { 800, "xqcTop" }, { 221, "Rudolph" },
            { 900, "Colorful Backpack" }, { 224, "Crab" }, { 997, "Rags" }, { 901, "Space Backpack" },
            { 902, "Classic Backpack" }, { 903, "Norway Backpack" }
        };

        public static string GetItemTag(Il2CppStructArray<byte> data, string tagName)
        {
            string ascii = Encoding.ASCII.GetString(data.ToArray());
            string tagPrefix = $"{tagName}:";

            int start = ascii.IndexOf(tagPrefix);
            if (start == -1) return "";

            start += tagPrefix.Length;

            int endSemicolon = ascii.IndexOf(";", start);
            int endNull = ascii.IndexOf("\x00", start);
            int end = (endSemicolon != -1 && endNull != -1)
                ? Math.Min(endSemicolon, endNull)
                : (endSemicolon != -1 ? endSemicolon : (endNull != -1 ? endNull : ascii.Length));

            string raw = ascii.Substring(start, end - start);
            return new string(raw.Where(c => !char.IsControl(c)).ToArray()).Trim();
        }

        public static List<Il2CppStructArray<byte>> ExtractItemsFromOutfit(Il2CppStructArray<byte> outfitData)
        {
            var all = new List<Il2CppStructArray<byte>>();
            byte[] data = outfitData.ToArray();
            byte[] signature = Encoding.ASCII.GetBytes("accountid\0");

            List<int> starts = [];

            for (int i = 0; i <= data.Length - signature.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (data[i + j] != signature[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match) starts.Add(i);
            }

            starts.Add(data.Length);

            for (int i = 0; i < starts.Count - 1; i++)
            {
                int start = starts[i];
                int len = starts[i + 1] - start;

                byte[] itemBytes = new byte[len];
                Array.Copy(data, start, itemBytes, 0, len);

                var il2cppItem = new Il2CppStructArray<byte>(len);
                for (int j = 0; j < len; j++) il2cppItem[j] = itemBytes[j];

                all.Add(il2cppItem);
            }

            return all;
        }
    }
}
