using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
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

            Element selectedElement;
            ElementId selId;

            try
            {
                View view = doc.ActiveView;

                // Set work plane if not set
                if (view.SketchPlane == null)
                {
                    using (Transaction t = new Transaction(doc, "Set Work Plane"))
                    {
                        t.Start();
                        double elevation = 0;
                        if (view.GenLevel != null)
                            elevation = view.GenLevel.Elevation;

                        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                        SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
                        view.SketchPlane = sketchPlane;
                        t.Commit();
                    }
                }

                // Element selection
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

                selectedElement = doc.GetElement(selId);
                if (!(selectedElement.Location is LocationPoint locationPoint))
                {
                    message = "Selected element must have a location point.";
                    return Result.Failed;
                }
                XYZ basePoint = locationPoint.Point;

                // Input dialog
                ArrayInputDialog dialog = new ArrayInputDialog();
                if (dialog.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                if (SettingsManager.GetBool("ShowTip_ArrayPlace"))
                {
                    TipDialog tip = new TipDialog("You will now select two points that define the placement bounds.", "ShowTip_ArrayPlace");
                    tip.ShowDialog();
                }

                int columns = dialog.Columns;
                int rows = dialog.Rows;

                // Check for excessive element count
                int maxElements = 1000;
                if (rows * columns > maxElements)
                {
                    TaskDialog.Show("Too many elements",
                        $"You are trying to place {rows * columns} elements.\n" +
                        $"The maximum allowed is {maxElements}.");
                    return Result.Cancelled;
                }

                // Main loop
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

                                    XYZ move = target - basePoint;
                                    ElementTransformUtils.CopyElement(doc, selId, move);
                                }
                            }
                            t.Commit();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break;
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