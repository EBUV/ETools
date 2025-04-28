using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ETools
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class PanelReassignCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Selection selection = uidoc.Selection;

            ICollection<ElementId> selectedIds = selection.GetElementIds();
            if (selectedIds.Count == 0)
            {
                message = "No elements selected.";
                return Result.Failed;
            }

            List<ElectricalSystem> elSystems = ExtractElectricalSystemsFromSelection(doc, selectedIds);

            if (!elSystems.Any())
            {
                TaskDialog.Show("Info", "No electrical systems found from selection or connected elements.");
                return Result.Failed;
            }

            // Collect available panels
            FilteredElementCollector panelCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_ElectricalEquipment)
                .WhereElementIsNotElementType();

            List<string> panelNames = panelCollector
                .Select(p => p.Name)
                .Distinct()
                .ToList();

            // Show panel selection window
            Panels window = new Panels(panelNames);
            window.ShowDialog();

            FamilyInstance targetPanel = null;

            if (window.pickFromModel)
            {
                try
                {
                    Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Select a panel in the model.");
                    targetPanel = doc.GetElement(pickedRef) as FamilyInstance;
                }
                catch
                {
                    TaskDialog.Show("Canceled", "No panel selected.");
                    return Result.Cancelled;
                }
            }
            else
            {
                string selectedPanelName = window.selectedPanelName;
                if (string.IsNullOrEmpty(selectedPanelName))
                {
                    TaskDialog.Show("Canceled", "No panel selected.");
                    return Result.Cancelled;
                }

                targetPanel = panelCollector
                    .Cast<FamilyInstance>()
                    .FirstOrDefault(p => p.Name == selectedPanelName);
            }

            if (targetPanel == null)
            {
                TaskDialog.Show("Error", "Selected panel not found or invalid.");
                return Result.Failed;
            }

            int reassignedCount = 0;

            using (Transaction t = new Transaction(doc, "Reassign Circuits to Panel"))
            {
                t.Start();
                foreach (var sys in elSystems)
                {
                    if (sys.BaseEquipment != null && sys.BaseEquipment.Id == targetPanel.Id)
                        continue;

                    try
                    {
                        sys.SelectPanel(targetPanel);
                        reassignedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        message = $"Failed to assign circuit '{sys.Name}' to panel: {ex.Message}";
                        return Result.Failed;
                    }
                }
                t.Commit();
            }

            TaskDialog.Show("Done", $"Circuits reassigned: {reassignedCount}\nPanel: {targetPanel.Name}");
            return Result.Succeeded;
        }

        private List<ElectricalSystem> ExtractElectricalSystemsFromSelection(Document doc, ICollection<ElementId> selectedIds)
        {
            var systems = new List<ElectricalSystem>();

            foreach (var id in selectedIds)
            {
                Element element = doc.GetElement(id);

                if (element is ElectricalSystem es)
                {
                    systems.Add(es);
                }
                else if (element is FamilyInstance fi && fi.MEPModel != null)
                {
                    var elSysA = fi.MEPModel.GetElectricalSystems();
                    if (elSysA != null)
                    {
                        foreach (var elSys in elSysA)
                        {
                            if (elSys != null)
                            {
                                systems.Add(elSys);
                            }
                        }
                    }
                }
            }

            return systems.Distinct().ToList();
        }
    }
}
