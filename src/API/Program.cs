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
        .ThenInclude(e => e.Person).Include(device => device.DeviceEmployees)
        .ThenInclude(deviceEmployee => deviceEmployee.Employee).ThenInclude(employee => employee.Person)
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

// DELETE: /api/devices/{id}
app.MapDelete("/api/devices/{id}", async (int id, DeviceContext db) =>
{
    var device = await db.Devices
        .Include(d => d.DeviceEmployees)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (device == null)
        return Results.NotFound("Device not found.");

    if (device.DeviceEmployees.Any())
        return Results.BadRequest("Cannot delete device. It is currently assigned to employees.");

    db.Devices.Remove(device);
    await db.SaveChangesAsync();

    return Results.NoContent();
});



// GET: /api/employees
app.MapGet("/api/employees", async (DeviceContext db) =>
{
    var employees = await db.Employees
        .Include(e => e.Person)
        .Select(e => new EmployeeDto
        {
            Id = e.Id,
            FullName = $"{e.Person.FirstName} {e.Person.LastName}"
        })
        .ToListAsync();

    return Results.Ok(employees);
});

// GET: /api/employees/{id}
app.MapGet("/api/employees/{id}", async (int id, DeviceContext db) =>
{
    var employee = await db.Employees
        .Include(e => e.Person)
        .Include(e => e.Position)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (employee == null)
        return Results.NotFound("Employee not found.");

    var dto = new EmployeeDetailsDto
    {
        PassportNumber = employee.Person.PassportNumber,
        FirstName = employee.Person.FirstName,
        MiddleName = employee.Person.MiddleName,
        LastName = employee.Person.LastName,
        PhoneNumber = employee.Person.PhoneNumber,
        Email = employee.Person.Email,
        Salary = employee.Salary,
        HireDate = employee.HireDate,
        Position = new PositionDto
        {
            Id = employee.Position.Id,
            Name = employee.Position.Name
        }
    };

    return Results.Ok(dto);
});

app.Run();
