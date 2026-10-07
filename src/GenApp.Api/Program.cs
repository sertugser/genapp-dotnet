using GenApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("GenApp")
    ?? throw new InvalidOperationException("Connection string 'GenApp' is not configured."));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
