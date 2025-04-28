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
    public class PlaceOneBetweenTwoPoints : IExternalCommand
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
                if (selectedIds.Count == 0)
                {
                    TaskDialog.Show("Select Element", "Please select an element to copy.");
                    selId = uidoc.Selection.PickObject(ObjectType.Element, "Select an element to copy").ElementId;
                }
                else
                {
                    selId = selectedIds.First();
                }

                selectedElement = doc.GetElement(selId);
                Location location = selectedElement.Location;
                XYZ basePoint = (location as LocationPoint).Point;

                if (SettingsManager.GetBool("ShowTip_SinglePlace"))
                {
                    TipDialog tip = new TipDialog("You will now select two points that define the placement bounds.", "ShowTip_SinglePlace");
                    tip.ShowDialog();
                }

                while (true)
                {
                    try
                    {
                        XYZ pointA = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick first point");
                        XYZ pointB = selection.PickPoint(ObjectSnapTypes.Intersections, "Pick second point");

                        XYZ direction = (pointB - pointA).Normalize();
                        XYZ targetPoint = pointA + direction * (pointA.DistanceTo(pointB) / 2);

                        using (Transaction t = new Transaction(doc, "Place Object Between Points"))
                        {
                            t.Start();
                            XYZ offset = new XYZ(targetPoint.X - basePoint.X, targetPoint.Y - basePoint.Y, 0);
                            ElementTransformUtils.CopyElement(doc, selId, offset);
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
