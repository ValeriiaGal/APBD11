using API;
using DTOs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("UniversityDatabase")
    ?? throw new InvalidOperationException("Connection string 'UniversityDatabase' not found.");

builder.Services.AddDbContext<DeviceContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// GET: /api/devices
app.MapGet("/api/devices", async (DeviceContext db) =>
{
    var devices = await db.Devices
        .Select(d => new DeviceDto
        {
            Id = d.Id,
            Name = d.Name
        })
        .ToListAsync();

    return Results.Ok(devices);
});

// GET: /api/devices/{id}
app.MapGet("/api/devices/{id}", async (int id, DeviceContext db) =>
{
    var device = await db.Devices
        .Include(d => d.DeviceType)
        .Include(d => d.DeviceEmployees)
            .ThenInclude(de => de.Employee)
                .ThenInclude(e => e.Person)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (device == null)
        return Results.NotFound("Device not found.");

    var employee = device.DeviceEmployees
        .Where(de => de.ReturnDate == null)
        .OrderByDescending(de => de.IssueDate)
        .Select(de => new EmployeeDto
        {
            Id = de.Employee.Id,
            FullName = $"{de.Employee.Person.FirstName} {de.Employee.Person.LastName}"
        })
        .FirstOrDefault();

    var details = new DeviceInfoDto
    {
        DeviceType = device.DeviceType?.Name ?? "Unknown",
        IsEnabled = device.IsEnabled,
        AdditionalProperties = System.Text.Json.JsonDocument.Parse(device.AdditionalProperties).RootElement,
        CurrentEmployee = employee
    };

    return Results.Ok(details);
});

// POST: /api/devices
app.MapPost("/api/devices", async (DeviceContext db, CreateUpdateDeviceDto dto) =>
{
    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceType);
    if (deviceType == null)
        return Results.BadRequest("Invalid device type.");

    var additionalJson = dto.AdditionalProperties.ToString() ?? "{}";

    var device = new Device
    {
        Name = $"New {dto.DeviceType}",
        IsEnabled = dto.IsEnabled,
        DeviceTypeId = deviceType.Id,
        AdditionalProperties = additionalJson
    };

    db.Devices.Add(device);
    await db.SaveChangesAsync();

    return Results.Created($"/api/devices/{device.Id}", new { device.Id });
});

// PUT: /api/devices/{id}
app.MapPut("/api/devices/{id}", async (int id, DeviceContext db, CreateUpdateDeviceDto dto) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound("Device not found.");

    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceType);
    if (deviceType == null)
        return Results.BadRequest("Invalid device type.");

    device.IsEnabled = dto.IsEnabled;
    device.DeviceTypeId = deviceType.Id;
    device.AdditionalProperties = dto.AdditionalProperties.ToString() ?? "{}";

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/devices/{id}", async (int id, DeviceContext db) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound("Device not found.");

    db.Devices.Remove(device);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
