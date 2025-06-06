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
    public class MoveCircuitsUp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View originalView = doc.ActiveView;

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

                using (Transaction t = new Transaction(doc, "Move Circuits Up"))
                {
                    t.Start();

                    // Получаем список всех цепей на панели с их строками
                    var allCircuits = new List<(int CircuitNumber, int Row)>();
                    for (int row = 2; row < 400; row++)
                    {
                        string numText = psv.GetCellText(SectionType.Body, row, 1);
                        if (int.TryParse(numText, out int num))
                            allCircuits.Add((num, row));
                    }

                    // Собираем номера выделенных цепей
                    var selectedNumbers = group.Select(c => int.Parse(c.CircuitNumber)).ToHashSet();

                    // Сортируем цепи по порядку строк (верх — низ)
                    var orderedCircuits = allCircuits.OrderBy(x => x.Row).ToList();

                    // Реализуем bubble-сдвиг вверх
                    bool anyMoved;
                    do
                    {
                        anyMoved = false;
                        for (int i = 1; i < orderedCircuits.Count; i++)
                        {
                            var current = orderedCircuits[i];
                            var above = orderedCircuits[i - 1];

                            if (selectedNumbers.Contains(current.CircuitNumber) && !selectedNumbers.Contains(above.CircuitNumber))
                            {
                                try
                                {
                                    psv.MoveSlotTo(current.Row, 2, above.Row, 2);

                                    // После перемещения обновляем порядок
                                    orderedCircuits[i - 1] = current;
                                    orderedCircuits[i] = above;

                                    anyMoved = true;
                                }
                                catch { /* ignore move errors */ }
                            }
                        }
                    } while (anyMoved);

                    t.Commit();
                }
            }

            uidoc.ActiveView = originalView;
            uidoc.Selection.SetElementIds(selectedIds);

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
