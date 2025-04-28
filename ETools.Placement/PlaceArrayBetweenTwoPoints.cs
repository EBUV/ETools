using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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

            ICollection<ElementId> selectedIds = selection.GetElementIds();
            Element selectedElement;
            ElementId selId;

            try
            {
                // Select the base element to be copied
                selId = selectedIds.Count == 0
                    ? uidoc.Selection.PickObject(ObjectType.Element, "Select an element to copy").ElementId
                    : selectedIds.First();

                selectedElement = doc.GetElement(selId);
                Location location = selectedElement.Location;
                XYZ basePoint = (location as LocationPoint).Point;

                // Show input dialog
                ArrayInputDialog dialog = new ArrayInputDialog();
                if (dialog.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                int columns = dialog.Columns;
                int rows = dialog.Rows;

                // Loop until user presses ESC
                while (true)
                {
                    try
                    {
                        XYZ pointA = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick first corner");
                        XYZ pointB = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick opposite corner");

                        XYZ pointA2 = new XYZ(pointA.X, pointB.Y, pointA.Z);
                        XYZ pointB2 = new XYZ(pointB.X, pointA.Y, pointB.Z);

                        double stepY = pointA.DistanceTo(pointA2) / rows;
                        double stepX = pointA.DistanceTo(pointB2) / columns;

                        XYZ dirY = (pointA2 - pointA).Normalize();
                        XYZ dirX = (pointB2 - pointA).Normalize();

                        using (Transaction t = new Transaction(doc, "Place Array Between Two Points"))
                        {
                            t.Start();
                            for (int y = 0; y < rows; y++)
                            {
                                XYZ rowOffset = dirY * ((y + 0.5) * stepY);
                                for (int x = 0; x < columns; x++)
                                {
                                    XYZ colOffset = dirX * ((x + 0.5) * stepX);
                                    XYZ target = pointA + rowOffset + colOffset;
                                    XYZ move = new XYZ(target.X - basePoint.X, target.Y - basePoint.Y, 0);
                                    ElementTransformUtils.CopyElement(doc, selId, move);
                                }
                            }
                            t.Commit();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break; // Exit loop on ESC
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
