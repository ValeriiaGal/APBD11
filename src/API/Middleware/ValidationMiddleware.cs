
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Middleware;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _rules;

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

                if (typeName != null && props != null && _rules.TryGetValue(typeName, out var expected))
                {
                    foreach (var rule in expected)
                    {
                        if (!props.ContainsKey(rule.Key))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync($"Missing field '{rule.Key}' for device type '{typeName}'.");
                            return;
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
