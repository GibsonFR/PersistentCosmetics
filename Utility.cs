namespace PersistentCosmetics
{
    public static class Utility
    {
        /// <summary>
        /// Creates a directory if it does not already exist.
        /// </summary>
        public static void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error creating folder: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates a file if it does not already exist.
        /// </summary>
        public static void CreateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using StreamWriter sw = File.CreateText(path);
                    sw.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error creating file: " + ex.Message);
            }
        }

        /// <summary>
        /// Resets a file by clearing its contents if it exists.
        /// </summary>
        public static void ResetFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using StreamWriter sw = new(path, false);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, "Error resetting file: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes a log entry to the specified log file.
        /// </summary>
        public static void Log(string path, string line)
        {
            try
            {
                using StreamWriter writer = new(path, true);
                writer.WriteLine(line.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log Error] Failed to write log: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a message to appear in the client chat only.
        /// </summary>
        public static void ForceMessage(string message)
        {
            try
            {
                ChatBox.Instance?.ForceMessage($"<color=yellow>[PersistentCosmetics] {message}</color>");
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error forcing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates or updates the configuration file with default values if missing.
        /// </summary>
        public static void SetConfigFile(string configFilePath)
        {
            try
            {
                Dictionary<string, string> configDefaults = new()
                {
                    {"version", "v1.1.0"},
                    {"menuKey", "insert"},
                    {"outfitsPerPage", "4"},
                    {"orbitSpeed", "72"},
                    {"autoEquipLastOutfit", "true"}
                };

                Dictionary<string, string> currentConfig = [];

                if (File.Exists(configFilePath))
                {
                    string[] lines = File.ReadAllLines(configFilePath);

                    foreach (string line in lines)
                    {
                        string[] keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            currentConfig[keyValue[0]] = keyValue[1];
                        }
                    }
                }

                foreach (KeyValuePair<string, string> pair in configDefaults)
                {
                    if (!currentConfig.ContainsKey(pair.Key))
                    {
                        currentConfig[pair.Key] = pair.Value;
                    }
                }

                using StreamWriter sw = File.CreateText(configFilePath);
                foreach (KeyValuePair<string, string> pair in currentConfig)
                {
                    sw.WriteLine(pair.Key + "=" + pair.Value);
                }
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error setting config file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the configuration file and applies settings to the application.
        /// </summary>
        public static void ReadConfigFile()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Log(logFilePath, "Config file not found.");
                    return;
                }

                string[] lines = File.ReadAllLines(configFilePath);
                Dictionary<string, string> config = [];
                CultureInfo cultureInfo = new CultureInfo("fr-FR");
                bool parseSuccess;
                bool resultBool;
                float resultFloat;
                int resultInt;

                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        config[key] = value;
                    }
                }

                if (!configOnStart)
                {
                    configOnStart = true;
                }

                menuKey = config["menuKey"];

                parseSuccess = int.TryParse(config["outfitsPerPage"], NumberStyles.Any, cultureInfo, out resultInt);
                outfitsPerPage = parseSuccess ? resultInt : 4;

                parseSuccess = float.TryParse(config["orbitSpeed"], NumberStyles.Any, cultureInfo, out resultFloat);
                orbitSpeed = parseSuccess ? resultFloat : 72f;

                parseSuccess = bool.TryParse(config["autoEquipLastOutfit"], out resultBool);
                autoEquipLastOutfit = parseSuccess ? resultBool : true;
            }
            catch (Exception ex)
            {
                Log(logFilePath, $"Error reading config file: {ex.Message}");
            }
        }

        public static void PlayMenuSound()
        {
            if (__PlayerInventory == null) return;

            __PlayerInventory.woshSfx.pitch = 5;
            __PlayerInventory.woshSfx.Play();
        }
    }
}
