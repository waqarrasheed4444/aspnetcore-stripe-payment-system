using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.WebApi.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clean Architecture Web API",
        Version = "v1",
        Description = "Enterprise .NET 8 Web API demonstrating Clean Architecture, MediatR (CQRS), EF Core, and FluentValidation.",
        Contact = new OpenApiContact
        {
            Name = "Full Stack .NET Developer Portfolio",
            Url = new Uri("https://github.com")
        }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed & Initialise Database
using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    await initialiser.InitialiseAsync();
    await initialiser.SeedAsync();
}

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure Swagger UI
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("UseInMemoryDatabase"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root URL
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
