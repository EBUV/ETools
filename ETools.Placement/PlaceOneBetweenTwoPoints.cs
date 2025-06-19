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
    public class PlaceOneBetweenTwoPoints : IExternalCommand
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

                if (view.SketchPlane == null)
                {
                    using (Transaction t = new Transaction(doc, "Set Work Plane"))
                    {
                        t.Start();

                        double elevation = 0;
                        if (view.GenLevel != null)
                        {
                            elevation = view.GenLevel.Elevation;
                        }

                        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                        SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
                        view.SketchPlane = sketchPlane;

                        t.Commit();
                    }
                }

                // Выбор элемента
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

                selectedElement = doc.GetElement(selId);
                Location location = selectedElement.Location;

                if (!(location is LocationPoint locationPoint))
                {
                    message = "Selected element must have a location point.";
                    return Result.Failed;
                }

                XYZ basePoint = locationPoint.Point;

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

                        XYZ midPoint = pointA + (pointB - pointA) * 0.5;

                        using (Transaction t = new Transaction(doc, "Place Object Between Points"))
                        {
                            t.Start();

                            XYZ offset = midPoint - basePoint;
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
