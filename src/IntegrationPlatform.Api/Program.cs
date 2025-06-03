using IntegrationPlatform.Api.Middleware;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Contracts.Models;
using IntegrationPlatform.SFTP;
using IntegrationPlatform.Email;
using IntegrationPlatform.Monitoring;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IntegrationPlatform.Email.Services;
using IntegrationPlatform.SFTP.Services;
using IntegrationPlatform.Monitoring.Services;
using IntegrationPlatform.Infrastructure.Services;
using IntegrationPlatform.Core.Services;
using Serilog;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using IntegrationPlatform.Infrastructure.Data;
using IntegrationPlatform.Infrastructure.Messaging;
using IntegrationPlatform.Infrastructure.ErrorHandling;
using IntegrationPlatform.Infrastructure.FileSystem;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration Platform API", Version = "v1" });
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource("IntegrationPlatform")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("IntegrationPlatform"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter());

// Configure Audit Trail
builder.Services.Configure<AuditTrailOptions>(builder.Configuration.GetSection("AuditTrail"));

// Configure Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Dapper
builder.Services.AddSingleton<DapperContext>();

// Configure RabbitMQ
builder.Services.AddSingleton<RabbitMQService>();

// Configure Transaction Manager
builder.Services.AddScoped<TransactionManager>();

// Inject all projects as components
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISftpService, SftpService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<IBusinessLogicService, BusinessLogicService>();

// Configure Error Handler
builder.Services.AddScoped<ErrorHandler>();

// Configure File Service
builder.Services.AddScoped<IFileService, FileService>();

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddApplicationInsights();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Add Audit Trail middleware
app.UseMiddleware<AuditTrailMiddleware>();

app.MapControllers();

app.Run();
