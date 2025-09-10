using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETools.Placement
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class PlaceOneBetweenTwoPoints : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Selection selection = uidoc.Selection;

            try
            {
                // 1) Выбор исходного элемента
                ElementId selId;
                if (selection.GetElementIds().Count == 0)
                {
                    if (SettingsManager.GetBool("ShowTip_SelectElement_Single"))
                    {
                        TipDialog tip = new TipDialog("Please select an element to copy.", "ShowTip_SelectElement_Single");
                        tip.ShowDialog();
                    }
                    selId = selection.PickObject(ObjectType.Element, "Select an element to copy").ElementId;
                }
                else
                {
                    selId = selection.GetElementIds().First();
                }

                Element srcElem = doc.GetElement(selId);
                if (!(srcElem.Location is LocationPoint srcLocPt))
                {
                    message = "Selected element must have a location point.";
                    return Result.Failed;
                }

                // Базовая точка исходника (оставляем исходный Z!)
                XYZ basePoint3d = srcLocPt.Point;

                if (SettingsManager.GetBool("ShowTip_SinglePlace"))
                {
                    TipDialog tip = new TipDialog("You will now select two points that define the placement bounds.", "ShowTip_SinglePlace");
                    tip.ShowDialog();
                }

                while (true)
                {
                    try
                    {
                        // 2) Две точки -> середина
                        XYZ pointA = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick first point");
                        XYZ pointB = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick second point");
                        XYZ midPoint = pointA + (pointB - pointA) * 0.5;

                        // 3) Смещение: переносим только по XY, Z сохраняем как у исходника —
                        // это уменьшает шанс ре-хостинга и ошибок допустимого диапазона.
                        XYZ target = new XYZ(midPoint.X, midPoint.Y, basePoint3d.Z);
                        XYZ offset = target - basePoint3d;

                        using (Transaction t = new Transaction(doc, "Copy element by doc->doc"))
                        {
                            t.Start();

                            // !!! ВАЖНО: перегрузка doc->doc, НЕ view->view и НЕ CopyElement(doc, …)
                            var ids = new List<ElementId> { selId };
                            var opts = new CopyPasteOptions();
                            // при необходимости: opts.SetDuplicateTypeNamesHandler(new YourHandler());

                            ICollection<ElementId> newIds =
                                ElementTransformUtils.CopyElements(
                                    doc,          // sourceDoc
                                    ids,          // что копируем
                                    doc,          // targetDoc (тот же)
                                    Transform.CreateTranslation(offset),
                                    opts
                                );

                            t.Commit();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break; // пользователь нажал ESC
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                        return Result.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
