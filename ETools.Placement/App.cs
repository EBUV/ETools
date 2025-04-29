using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ETools.Placement
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "ETools";
            string panelName = "Placement";

            try
            {
                // Создаем вкладку, если ее еще нет
                application.CreateRibbonTab(tabName);
            }
            catch { /* вкладка уже существует */ }

            // Создаем панель
            RibbonPanel panel = null;
            foreach (RibbonPanel p in application.GetRibbonPanels(tabName))
            {
                if (p.Name == panelName)
                {
                    panel = p;
                    break;
                }
            }
            if (panel == null)
                panel = application.CreateRibbonPanel(tabName, panelName);

            // Путь к текущей DLL
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Кнопка 1 — одиночный объект
            PushButtonData button1 = new PushButtonData(
                "PlaceOne",
                "Place One",
                assemblyPath,
                "ETools.Placement.PlaceOneBetweenTwoPoints");
            button1.ToolTip = "Place one element centered between two picked points.";
            button1.LargeImage = LoadImage("PlaceOneIcon_32.png");

            // Кнопка 2 — массив
            PushButtonData button2 = new PushButtonData(
                "PlaceArray",
                "Place Array",
                assemblyPath,
                "ETools.Placement.PlaceArrayBetweenTwoPoints");
            button2.ToolTip = "Place an array of elements between two picked points.";
            button2.LargeImage = LoadImage("PlaceArrayIcon_32.png");

            panel.AddItem(button1);
            panel.AddItem(button2);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private BitmapImage LoadImage(string imageName)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), imageName);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            return image;
        }
    }
}
