using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ETools
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "ETools";
            string panelName = "Circuits";

            // Создать вкладку, если нет
            try { application.CreateRibbonTab(tabName); } catch { }

            // Создать панель
            RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string baseDir = Path.GetDirectoryName(assemblyPath);

            // --- Кнопка Reassign ---
            PushButtonData reassignData = new PushButtonData(
                "ReassignCircuits",
                "Reassign",
                assemblyPath,
                "ETools.PanelReassignCommand");
            PushButton reassignButton = panel.AddItem(reassignData) as PushButton;

            string iconReassign = Path.Combine(baseDir, "ReassignCircuits.png");
            if (File.Exists(iconReassign))
                reassignButton.LargeImage = new BitmapImage(new Uri(iconReassign, UriKind.Absolute));
            reassignButton.ToolTip = "Reassign selected electrical circuits to another panel.";
            reassignButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url,
                "https://ebuv.github.io/etools-privacy/help-reassign.html"));
            /*
            // --- Кнопка Move Up ---
            PushButtonData moveUpData = new PushButtonData(
                "MoveCircuitsUp",
                "Move Up",
                assemblyPath,
                "ETools.MoveCircuitsUp");
            PushButton moveUpButton = panel.AddItem(moveUpData) as PushButton;

            string iconUp = Path.Combine(baseDir, "MoveCircuitsUp.png");
            if (File.Exists(iconUp))
                moveUpButton.LargeImage = new BitmapImage(new Uri(iconUp, UriKind.Absolute));
            moveUpButton.ToolTip = "Move selected circuit up (N-).";
            moveUpButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url,
                "https://ebuv.github.io/etools-privacy/help-movecircuitsup.html"));

            // --- Кнопка Move Down ---
            PushButtonData moveDownData = new PushButtonData(
                "MoveCircuitsDown",
                "Move Down",
                assemblyPath,
                "ETools.MoveCircuitsDown");
            PushButton moveDownButton = panel.AddItem(moveDownData) as PushButton;

            string iconDown = Path.Combine(baseDir, "MoveCircuitsDown.png");
            if (File.Exists(iconDown))
                moveDownButton.LargeImage = new BitmapImage(new Uri(iconDown, UriKind.Absolute));
            moveDownButton.ToolTip = "Move selected circuit down (N+).";
            moveDownButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url,
                "https://ebuv.github.io/etools-privacy/help-movecircuitsdown.html"));
            */
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
