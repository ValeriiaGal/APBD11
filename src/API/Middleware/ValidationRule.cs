using System.Text.Json;
using System.Text.Json.Serialization;

public class ValidationRule
{
    public string Type { get; set; } = null!;
    public string PreRequestName { get; set; } = null!;
    public string PreRequestValue { get; set; } = null!;
    public List<Rule> Rules { get; set; } = new();
}

public class Rule
{
    public string ParamName { get; set; } = null!;
    
    [JsonPropertyName("regex")]
    public JsonElement Regex { get; set; } 
}