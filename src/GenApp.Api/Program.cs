using GenApp.Api;
using GenApp.Application;
using GenApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("GenApp")
    ?? throw new InvalidOperationException("Connection string 'GenApp' is not configured."));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// docs/api-design.md section 1: the OpenAPI description is shown at /swagger in the development environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");
app.MapControllers();

app.Run();
