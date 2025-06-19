using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace ETools
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MoveCircuitsDown : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View originalView = doc.ActiveView;

            // Save current selection to restore it after the command
            var selectedIds = uidoc.Selection.GetElementIds();
            if (!selectedIds.Any())
            {
                TaskDialog.Show("Info", "Please select circuits or elements connected to circuits.");
                return Result.Cancelled;
            }

            var circuits = GetElectricalSystemsFromSelection(doc, selectedIds)
                .Where(c => !string.IsNullOrEmpty(c.CircuitNumber) && int.TryParse(c.CircuitNumber, out _))
                .ToList();

            if (!circuits.Any())
            {
                TaskDialog.Show("Info", "No circuits with numeric circuit numbers found.");
                return Result.Cancelled;
            }

            int movedCount = 0;

            var groupedByPanel = circuits.GroupBy(c => c.BaseEquipment.Id);

            foreach (var group in groupedByPanel)
            {
                var panelId = group.Key;
                var panel = doc.GetElement(panelId) as FamilyInstance;
                if (panel == null) continue;

                var psv = new FilteredElementCollector(doc)
                    .OfClass(typeof(PanelScheduleView))
                    .Cast<PanelScheduleView>()
                    .FirstOrDefault(v =>
                    {
                        try
                        {
                            Element panelElem = doc.GetElement(v.GetPanel());
                            return panelElem != null && panelElem.Id == panelId;
                        }
                        catch { return false; }
                    });

                if (psv == null) continue;

                uidoc.ActiveView = psv;

                using (Transaction t = new Transaction(doc, "Move Circuits Down"))
                {
                    t.Start();

                    Dictionary<int, int> rowByNumber = new Dictionary<int, int>();
                    for (int row = 2; row < 400; row++)
                    {
                        string numText = psv.GetCellText(SectionType.Body, row, 1);
                        if (int.TryParse(numText, out int num))
                        {
                            rowByNumber[num] = row;
                        }
                    }

                    // Sort circuits from top to bottom (descending)
                    var sortedCircuits = group.OrderByDescending(c => int.Parse(c.CircuitNumber)).ToList();

                    foreach (var circuit in sortedCircuits)
                    {
                        int currentNum = int.Parse(circuit.CircuitNumber);
                        if (!rowByNumber.TryGetValue(currentNum, out int currentRow))
                            continue;

                        int targetRow = currentRow + 1;

                        string belowNumberText = psv.GetCellText(SectionType.Body, targetRow, 1);

                        if (string.IsNullOrEmpty(belowNumberText))
                        {
                            psv.MoveSlotTo(currentRow, 2, targetRow, 2);
                            rowByNumber[currentNum] = targetRow;
                            movedCount++;
                        }
                        else
                        {
                            if (int.TryParse(belowNumberText, out int belowNum))
                            {
                                int tempRow = FindTemporaryEmptyRow(psv, 350, 400);
                                if (tempRow != -1)
                                {
                                    psv.MoveSlotTo(targetRow, 2, tempRow, 2);
                                    psv.MoveSlotTo(currentRow, 2, targetRow, 2);
                                    psv.MoveSlotTo(tempRow, 2, currentRow, 2);

                                    rowByNumber[currentNum] = targetRow;
                                    rowByNumber[belowNum] = currentRow;

                                    movedCount++;
                                }
                            }
                        }
                    }

                    t.Commit();
                }
            }

            uidoc.ActiveView = originalView;

            // Restore selection after running the command
            uidoc.Selection.SetElementIds(selectedIds);

            // Optionally show message:
            // TaskDialog.Show("Done", $"Circuits moved down: {movedCount}");

            return Result.Succeeded;
        }

        private int FindTemporaryEmptyRow(PanelScheduleView psv, int from, int to)
        {
            for (int row = from; row <= to; row++)
            {
                string check = psv.GetCellText(SectionType.Body, row, 2);
                if (string.IsNullOrEmpty(check))
                    return row;
            }
            return -1;
        }

        private List<ElectricalSystem> GetElectricalSystemsFromSelection(Document doc, ICollection<ElementId> ids)
        {
            var result = new List<ElectricalSystem>();

            foreach (var id in ids)
            {
                var el = doc.GetElement(id);
                if (el is ElectricalSystem es)
                {
                    result.Add(es);
                }
                else if (el is FamilyInstance fi && fi.MEPModel != null)
                {
                    var systems = fi.MEPModel.GetElectricalSystems();
                    if (systems != null)
                        result.AddRange(systems);
                }
            }
            return result.Distinct().ToList();
        }
    }
}