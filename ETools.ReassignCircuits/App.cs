using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;

namespace ETools
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "ETools";
            string panelName = "Circuits";

            // Try to create ribbon tab
            try { application.CreateRibbonTab(tabName); } catch { }

            // Create ribbon panel
            RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

            // Path to this assembly
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Create button data
            PushButtonData buttonData = new PushButtonData(
                "ReassignCircuits",
                "Reassign",
                assemblyPath,
                "ETools.PanelReassignCommand");



            PushButton button = panel.AddItem(buttonData) as PushButton;

            // Load icon
            string iconPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "ReassignCircuits.png");
            if (File.Exists(iconPath))
            {
                button.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
            }

            // Tooltip
            button.ToolTip = "Reassign selected electrical circuits to another panel.";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
