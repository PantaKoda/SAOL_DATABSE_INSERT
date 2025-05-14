using System.Text.Json.Serialization;

namespace SAOL_DATABSE_INSERT.JsonModels;

public class JsonAdverb
{
    [JsonPropertyName("class")]
    public string? ClassName { get; set; }

    [JsonPropertyName("forms")]
    public List<string>? Forms { get; set; } // Adverb forms are a simple list
}