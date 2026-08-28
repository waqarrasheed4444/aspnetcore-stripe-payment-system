using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.WebApi.Middleware;
using CleanArchitecture.WebApi.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Current User Service (HTTP context based)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Controllers — EnableRawRequestBodyReading is required by the Stripe webhook endpoint
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ASP.NET Core Stripe Payment System",
        Version = "v1",
        Description = """
            Enterprise .NET 8 Web API demonstrating Clean Architecture, CQRS (MediatR),
            EF Core, FluentValidation, and a production-ready Stripe Payment + Subscription integration.

            ## Stripe Features
            - One-time payments via Stripe Checkout
            - Subscription billing with monthly/yearly plans
            - Secure webhook processing with idempotency
            - Full & partial refunds
            - Stripe Customer Billing Portal

            ## Security
            - Prices are always loaded from the database (never trusted from clients)
            - Webhook events are verified using Stripe-Signature before processing
            - Duplicate webhook events are detected and safely ignored
            """,
        Contact = new OpenApiContact
        {
            Name = "Waqar Hussain — Full Stack .NET Developer",
            Url = new Uri("https://github.com/waqarrasheed4444")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Enable XML comments for Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET Core Stripe Payment System v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root URL
        c.DocumentTitle = "Stripe Payment System API";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

