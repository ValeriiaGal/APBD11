using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Middleware;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly List<ValidationRule> _validationRules;

    public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var json = File.ReadAllText("example_validation_rules.json");
        var doc = JsonDocument.Parse(json);
        _validationRules = doc.RootElement.GetProperty("validations").Deserialize<List<ValidationRule>>() ?? new();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("ValidationMiddleware START");

        if (context.Request.Path.StartsWithSegments("/api/device") &&
            (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put))
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            try
            {
                var jsonNode = JsonNode.Parse(body);
                var typeName = jsonNode?["deviceTypeName"]?.ToString();
                var props = jsonNode?["additionalProperties"]?.AsObject();
                var preName = jsonNode?["isEnabled"]?.ToString();

                var ruleSet = _validationRules.FirstOrDefault(v =>
                    v.Type == typeName &&
                    v.PreRequestName == "isEnabled" &&
                    v.PreRequestValue.Equals(preName, StringComparison.OrdinalIgnoreCase));

                if (ruleSet != null && props != null)
                {
                    foreach (var rule in ruleSet.Rules)
                    {
                        if (!props.ContainsKey(rule.ParamName))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync($"Missing field '{rule.ParamName}' for device type '{typeName}'.");
                            return;
                        }

                        var value = props[rule.ParamName]?.ToString();
                        if (string.IsNullOrWhiteSpace(value)) continue;

                        if (rule.Regex.ValueKind == JsonValueKind.Array)
                        {
                            var valid = rule.Regex.EnumerateArray().Any(v => v.ToString() == value);
                            if (!valid)
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync($"Invalid value '{value}' for '{rule.ParamName}'.");
                                return;
                            }
                        }
                        else
                        {
                            var pattern = rule.Regex.ToString();
                            if (!Regex.IsMatch(value, pattern))
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync($"Value '{value}' for '{rule.ParamName}' does not match pattern.");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid JSON format.");
                return;
            }
        }

        await _next(context);
        _logger.LogInformation("ValidationMiddleware END");
    }
}  

