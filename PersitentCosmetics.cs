using System.Text.RegularExpressions;
using static PersistentCosmetics.PersitentCosmeticsUtility;

namespace PersistentCosmetics
{
    public class PersitentCosmeticsPatches
    {
        [HarmonyPatch(typeof(ClientSend), nameof(ClientSend.SendSerializedInventory))]
        [HarmonyPrefix]
        static void OnClientSendSendSerializedInventory(Il2CppStructArray<byte> __0)
        {
            lastEquippedOutfit = __0;
            OutfitVisualizerManager.ApplyLastEquippedOutfitTo(OutfitVisualizerManager.outfitPreviewPlayer);
            if (!logOutfit || __0.Length < 50) return;

            logOutfit = false;
            AddUnique(__0);       
            SaveToFile();         
            LoadFromFile();

            MenuManager.cacheDirty = false;
            MenuManager.RebuildCaches();
            MenuManager.currentMenu = MenuUtility.BuildCosmeticBrowserMenu();
        }
    }

    public static class PersitentCosmeticsUtility
    {
        public static HashSet<string> favoriteOutfits = [];
        public static Dictionary<string, string> outfitNames = [];
        public static void LoadFromFile()
        {
            cosmeticsList.Clear();
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
                var cosmetic = new Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                    cosmetic[i] = bytes[i];

                cosmeticsList.Add(cosmetic);
                string hash = ComputeOutfitHash(cosmetic);

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
            using StreamWriter writer = new(persistentCosmeticsFilePath, false);
            foreach (var cosmetic in cosmeticsList)
            {
                string hex = BitConverter.ToString(cosmetic.ToArray()).Replace("-", "");
                string hash = ComputeOutfitHash(cosmetic);

                List<string> metadata = new();

                if (outfitNames.TryGetValue(hash, out string name)) metadata.Add(name);

                if (favoriteOutfits.Contains(hash)) metadata.Add("FAV");

                if (metadata.Count > 0) writer.WriteLine($"{hex}::{string.Join("::", metadata)}");
                else writer.WriteLine(hex);
            }
        }

        public static string ComputeOutfitHash(Il2CppStructArray<byte> data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hash = sha.ComputeHash(data.ToArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static bool AreArraysEqual(Il2CppStructArray<byte> a, Il2CppStructArray<byte> b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public static void AddUnique(Il2CppStructArray<byte> outfit)
        {
            bool exists = cosmeticsList.Any(existing => AreArraysEqual(existing, outfit));

            if (exists)
            {
                ForceMessage("Outfit already exists.");
                return;
            }

            cosmeticsList.Add(outfit);
            SaveToFile();
            ForceMessage("New Outfit Logged.");
        }


        public static List<Il2CppStructArray<byte>> GetAllCosmetics() => cosmeticsList;

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

        public static string GetCosmeticNameFromCosmeticArray(Il2CppStructArray<byte> data)
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
                return COSMETIC_DEFID_TO_NAME.TryGetValue(itemdefid, out var name)
                    ? name
                    : $"Unknown (itemdefid: {itemdefid})";
            }

            return "Invalid itemdefid format";
        }

        public static Dictionary<int, string> COSMETIC_DEFID_TO_NAME = new()
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

        public static Dictionary<int, string> COSMETIC_DEFID_TO_CATEGORY = new()
        {
            // Hat
            { 223, "Hat" }, { 222, "Hat" }, { 219, "Hat" }, { 220, "Hat" },
            { 216, "Hat" }, { 200, "Hat" }, { 203, "Hat" }, { 215, "Hat" }, { 209, "Hat" },
            { 207, "Hat" }, { 212, "Hat" }, { 206, "Hat" }, { 201, "Hat" }, { 205, "Hat" },
            { 204, "Hat" }, { 208, "Hat" }, { 213, "Hat" }, { 214, "Hat" }, { 210, "Hat" },
            { 202, "Hat" }, { 211, "Hat" }, { 218, "Hat" }, { 217, "Hat" }, { 221, "Hat" },
            { 224, "Hat" },

            // Hair
            { 11, "Hair" }, { 12, "Hair" }, { 7, "Hair" }, { 10, "Hair" }, { 4, "Hair" },
            { 3, "Hair" }, { 1, "Hair" }, { 9, "Hair" }, { 6, "Hair" }, { 5, "Hair" },
            { 2, "Hair" }, { 8, "Hair" },

            // Face
            { 420, "Face" }, { 419, "Face" }, { 417, "Face" }, { 412, "Face" }, { 404, "Face" },
            { 411, "Face" }, { 408, "Face" }, { 403, "Face" }, { 414, "Face" }, { 416, "Face" },
            { 415, "Face" }, { 405, "Face" }, { 407, "Face" }, { 413, "Face" }, { 400, "Face" },
            { 402, "Face" }, { 401, "Face" }, { 409, "Face" }, { 410, "Face" }, { 406, "Face" },
            { 418, "Face" },

            // Shoes
            { 611, "Shoes" }, { 610, "Shoes" }, { 612, "Shoes" }, { 608, "Shoes" }, { 607, "Shoes" },
            { 604, "Shoes" }, { 605, "Shoes" }, { 602, "Shoes" }, { 601, "Shoes" }, { 600, "Shoes" },
            { 606, "Shoes" }, { 603, "Shoes" }, { 609, "Shoes" },

            // Backpack
            { 900, "Backpack" }, { 901, "Backpack" }, { 902, "Backpack" }, { 903, "Backpack" },

            // Top
            { 801, "Top" }, { 0, "Top" }, { 800, "Top" },

            // None
            { 1008, "None" }, { 1000, "None" }, { 1001, "None" }, { 1002, "None" },
            { 1003, "None" }, { 999, "None" }, { 1005, "None" }, { 997, "None" }
        };



        public static string GetCosmeticTag(Il2CppStructArray<byte> data, string tagName)
        {
            string ascii = Encoding.ASCII.GetString(data.ToArray());
            string pattern = $@"{Regex.Escape(tagName)}:(.*?)(;|\x00|$)";
            var match = Regex.Match(ascii, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        public static List<Il2CppStructArray<byte>> ExtractCosmeticsFromOutfit(Il2CppStructArray<byte> outfitData)
        {
            byte[] data = outfitData.ToArray();
            var signature = Encoding.ASCII.GetBytes("accountid\0");
            var cosmetics = new List<Il2CppStructArray<byte>>();

            List<int> starts = Enumerable.Range(0, data.Length - signature.Length + 1)
                .Where(i => signature.SequenceEqual(data.Skip(i).Take(signature.Length)))
                .ToList();

            starts.Add(data.Length);

            for (int i = 0; i < starts.Count - 1; i++)
            {
                int len = starts[i + 1] - starts[i];
                var segment = new Il2CppStructArray<byte>(len);
                for (int j = 0; j < len; j++) segment[j] = data[starts[i] + j];
                cosmetics.Add(segment);
            }

            return cosmetics;
        }
    }
}