using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Models;

namespace HGT.EAM.WebServices.Conector.Architecture.Extensions;

public static class DataRowExtensions
{
    public static List<Dictionary<string, object>> GetDTORows(this List<DATAROW> records, List<Field> fields) 
    {
        var recordsDTO = new List<Dictionary<string, object>>();
        foreach (var record in records) 
        {
            var recordDTO = new Dictionary<string, object>();
            var rows = record.D;
            foreach (var row in rows)
            {
                var currentId = Convert.ToInt32(row.n);
                var field = fields.FirstOrDefault(filter => filter.Id == currentId);
                var value = row.Text != null && row.Text.Length > 0 ? row.Text[0] : null;
                recordDTO.Add(field.Name, value);
            }
            recordsDTO.Add(recordDTO.Keys.ToDictionary(_ => _, _ => recordDTO[_]));
            recordDTO.Clear();
        }
        return recordsDTO;
    }

    public static List<Field> MapFieldsDTO(this List<FIELD> fields)
    {
        var fieldsDTO = new List<Field>();
        foreach (var field in fields)
        {
            _ = int.TryParse(field.aliasnum, out int id);
            var label = string.Empty + field.label;
            var name = string.Empty + field.name;
            int order = !string.IsNullOrEmpty(field.order) ? Convert.ToInt32(field.order) : 0;
            var type = string.Empty + field.type;
            bool isVisible = !string.IsNullOrEmpty(field.visible) && field.visible == "+";
            int width = !string.IsNullOrEmpty(field.width) ? Convert.ToInt32(field.width) : 0;
            fieldsDTO.Add(new Field 
            {
                Id = id,
                Label = label,
                Name = name,
                Order = order,
                Type = type,
                Visible = isVisible,
                Width = width,
            });
        }
        return fieldsDTO;
    }
}
