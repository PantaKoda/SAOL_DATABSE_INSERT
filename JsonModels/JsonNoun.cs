using System.Text.Json.Serialization;

namespace SAOL_DATABSE_INSERT.JsonModels;

public class JsonNoun
{
    [JsonPropertyName("class")]
    public string? ClassName { get; set; }

    [JsonPropertyName("forms")]
    public Dictionary<string, List<string>>? Forms { get; set; }
}