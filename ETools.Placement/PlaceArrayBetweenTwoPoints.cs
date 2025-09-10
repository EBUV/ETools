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
    public class PlaceArrayBetweenTwoPoints : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Selection selection = uidoc.Selection;

            try
            {
                // --- Выбор исходного элемента
                ElementId selId;
                var selectedIds = selection.GetElementIds();
                if (selectedIds.Count == 0)
                {
                    if (SettingsManager.GetBool("ShowTip_ArrayPlace"))
                    {
                        TipDialog tip = new TipDialog("Please select an element to copy.", "ShowTip_ArrayPlace");
                        tip.ShowDialog();
                    }
                    selId = uidoc.Selection.PickObject(ObjectType.Element, "Select an element to copy").ElementId;
                }
                else
                {
                    selId = selectedIds.First();
                }

                Element selectedElement = doc.GetElement(selId);
                if (!(selectedElement.Location is LocationPoint locationPoint))
                {
                    message = "Selected element must have a location point.";
                    return Result.Failed;
                }

                // Базовая точка исходника (сохраняем Z!)
                XYZ basePoint3d = locationPoint.Point;

                // --- Ввод параметров массива
                ArrayInputDialog dialog = new ArrayInputDialog();
                if (dialog.ShowDialog() != true)
                    return Result.Cancelled;

                int columns = dialog.Columns;
                int rows = dialog.Rows;

                // Ограничение на кол-во копий
                int maxElements = 1000;
                int total = rows * columns;
                if (total > maxElements)
                {
                    TaskDialog.Show("Too many elements",
                        $"You are trying to place {total} elements.\nThe maximum allowed is {maxElements}.");
                    return Result.Cancelled;
                }

                if (SettingsManager.GetBool("ShowTip_ArrayPlace"))
                {
                    TipDialog tip = new TipDialog("You will now select two points that define the placement bounds.", "ShowTip_ArrayPlace");
                    tip.ShowDialog();
                }

                // --- Основной цикл
                while (true)
                {
                    try
                    {
                        // Прямоугольная область по двум углам
                        XYZ pointA = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick first corner");
                        XYZ pointB = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick opposite corner");

                        // Векторы сторон прямоугольника в плоскости XY текущего вида
                        XYZ aToA2 = new XYZ(0, pointB.Y - pointA.Y, 0);
                        XYZ aToB2 = new XYZ(pointB.X - pointA.X, 0, 0);

                        double lenY = aToA2.GetLength();
                        double lenX = aToB2.GetLength();

                        if (lenX < 1e-9 || lenY < 1e-9)
                        {
                            TaskDialog.Show("Warning", "Rectangle side is too small.");
                            continue;
                        }

                        XYZ dirY = aToA2.Normalize(); // «вверх»
                        XYZ dirX = aToB2.Normalize(); // «вправо»

                        double stepY = lenY / rows;
                        double stepX = lenX / columns;

                        using (Transaction t = new Transaction(doc, "Place Array Between Two Points"))
                        {
                            t.Start();

                            var ids = new List<ElementId> { selId };

                            for (int y = 0; y < rows; y++)
                            {
                                XYZ rowOffset = dirY * ((y + 0.5) * stepY);
                                for (int x = 0; x < columns; x++)
                                {
                                    XYZ colOffset = dirX * ((x + 0.5) * stepX);

                                    // Целевая точка центра ячейки (XY по прямоугольнику, Z — как у исходника)
                                    XYZ targetXY = pointA + rowOffset + colOffset;
                                    XYZ target = new XYZ(targetXY.X, targetXY.Y, basePoint3d.Z);

                                    // Вектор смещения от исходного экземпляра
                                    XYZ move = target - basePoint3d;

                                    // КЛЮЧ: doc->doc перегрузка без контекста вида
                                    ElementTransformUtils.CopyElements(
                                        doc,                              // sourceDoc
                                        ids,                              // что копируем
                                        doc,                              // targetDoc (тот же)
                                        Transform.CreateTranslation(move),
                                        new CopyPasteOptions());
                                }
                            }

                            t.Commit();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break; // ESC — выходим из цикла
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
