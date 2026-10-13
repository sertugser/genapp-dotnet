using GenApp.Api;
using GenApp.Application;
using GenApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("GenApp")
    ?? throw new InvalidOperationException("Connection string 'GenApp' is not configured."));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // docs/api-design.md section 3: a rejected request is a 400 Problem Details with type "validation-error"
        // and the list of invalid fields in "errors".
        options.ClientErrorMapping[StatusCodes.Status400BadRequest].Link = "validation-error";
    });
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
