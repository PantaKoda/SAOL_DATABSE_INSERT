using System.Text.Json.Serialization;

namespace SAOL_DATABSE_INSERT.JsonModels;

public class JsonVerb
{
    [JsonPropertyName("class")]
    public string? ClassName { get; set; }

    [JsonPropertyName("forms")]
    public Dictionary<string, List<string>>? Forms { get; set; }
}