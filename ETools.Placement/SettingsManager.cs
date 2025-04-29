using System;
using System.IO;
using System.Xml.Linq;

namespace ETools.Placement
{
    public static class SettingsManager
    {
        private static readonly string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ETools");

        private static readonly string settingsFilePath = Path.Combine(folderPath, "settings.xml");

        private static XDocument settingsDoc;

        // Load or create settings file
        static SettingsManager()
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (!File.Exists(settingsFilePath))
            {
                // Create default settings file
                settingsDoc = new XDocument(
                    new XElement("Settings",
                        new XElement("ShowTip_SelectElement_Single", "true"),
                        new XElement("ShowTip_SinglePlace", "true"),
                        new XElement("ShowTip_SelectElement_Array", "true"),
                        new XElement("ShowTip_ArrayPlace", "true")
                    )
                );
                settingsDoc.Save(settingsFilePath);
            }
            else
            {
                settingsDoc = XDocument.Load(settingsFilePath);
            }
        }

        public static bool GetBool(string key)
        {
            try
            {
                var value = settingsDoc.Root.Element(key)?.Value;
                return value?.ToLower() != "false";
            }
            catch
            {
                return true; // fallback: show if something wrong
            }
        }

        public static void SetBool(string key, bool value)
        {
            try
            {
                if (settingsDoc.Root.Element(key) == null)
                {
                    settingsDoc.Root.Add(new XElement(key, value.ToString().ToLower()));
                }
                else
                {
                    settingsDoc.Root.Element(key).Value = value.ToString().ToLower();
                }
                settingsDoc.Save(settingsFilePath);
            }
            catch
            {
                // Silent catch – avoid crashing add-in
            }
        }
    }
}
