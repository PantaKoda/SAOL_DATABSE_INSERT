namespace SAOL_DATABSE_INSERT.JsonModels;
using System.Text.Json.Serialization;

public class JsonAdjective
{
    [JsonPropertyName("class")]
    public string? ClassName { get; set; } // Use ClassName to avoid conflict with 'class' keyword

    [JsonPropertyName("forms")]
    public Dictionary<string, List<string>>? Forms { get; set; }
}