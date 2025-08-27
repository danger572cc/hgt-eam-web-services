using System.Text.Json.Serialization;

namespace HGT.EAM.WebServices.Conector.Architecture.Models;

public sealed class DataRecord
{
    [JsonPropertyName("fields")]
    public required List<Field> Fields { get; set; } = [];

    [JsonPropertyName("rows")]
    public required List<Dictionary<string, object>> Rows { get; set; } = [];
}

public sealed class Field 
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("order")]
    public required int Order { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }
}

public class ResultDataGridModel
{
    [JsonPropertyName("currentPage")]
    public required int CurrentPage { get; set; }

    [JsonPropertyName("totalRecordsReturned")]
    public required int TotalRecordsReturned { get; set; }

    [JsonPropertyName("totalPages")]
    public required int TotalPages { get; set; }

    [JsonPropertyName("totalRecords")]
    public required int TotalRecords { get; set; }

    [JsonPropertyName("dataRecord")]
    public required DataRecord DataRecord { get; set; }
}