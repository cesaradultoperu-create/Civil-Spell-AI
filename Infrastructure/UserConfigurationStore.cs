using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CivilSpellAI.Infrastructure
{
    public sealed class UserConfigurationStore
    {
        public const long MaximumSettingsFileBytes = 64L * 1024L;
        private readonly string configurationDirectory;

        public UserConfigurationStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CivilSpellAI"))
        {
        }

        public UserConfigurationStore(string configurationDirectory)
        {
            if (string.IsNullOrWhiteSpace(configurationDirectory))
            {
                throw new ArgumentException(
                    "Se requiere el directorio de configuración.",
                    "configurationDirectory");
            }

            this.configurationDirectory = configurationDirectory;
        }

        public string ConfigurationDirectory
        {
            get { return configurationDirectory; }
        }

        public string SettingsPath
        {
            get { return Path.Combine(configurationDirectory, "settings.v3.json"); }
        }

        public string PreviousSettingsPath
        {
            get { return Path.Combine(configurationDirectory, "settings.v2.json"); }
        }

        public string LegacySettingsPath
        {
            get { return Path.Combine(configurationDirectory, "settings.v1.json"); }
        }

        public UserSettings Load()
        {
            string[] candidates =
            {
                SettingsPath,
                SettingsPath + ".tmp",
                PreviousSettingsPath,
                LegacySettingsPath
            };

            foreach (string candidate in candidates)
            {
                UserSettings settings;

                if (TryLoad(candidate, out settings))
                    return settings;
            }

            return new UserSettings();
        }

        public void Save(UserSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            settings.Normalize();
            Directory.CreateDirectory(configurationDirectory);
            string temporaryPath = SettingsPath + ".tmp";

            using (FileStream stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(UserSettings));
                serializer.WriteObject(stream, settings);
            }

            ReplaceFile(temporaryPath, SettingsPath);
        }

        internal static void ReplaceFile(string temporaryPath, string targetPath)
        {
            if (File.Exists(targetPath))
                File.Replace(temporaryPath, targetPath, null);
            else
                File.Move(temporaryPath, targetPath);
        }

        private static bool TryLoad(
            string sourcePath,
            out UserSettings settings)
        {
            settings = null;

            if (!File.Exists(sourcePath))
                return false;

            try
            {
                if (new FileInfo(sourcePath).Length > MaximumSettingsFileBytes)
                    return false;

                using (FileStream stream = File.OpenRead(sourcePath))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(UserSettings));
                    settings = serializer.ReadObject(stream) as UserSettings;

                    if (settings == null)
                        return false;

                    settings.Normalize();
                    return true;
                }
            }
            catch (SerializationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
        }
    }
}
