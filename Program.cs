using TransactionApi.Services;
using TransactionApi.Middleware;
using log4net;
using log4net.Config;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Configure logging to use log4net
builder.Logging.ClearProviders();
builder.Logging.AddLog4Net("log4net.config");

// Add services to the container.

builder.Services.AddControllers();

// Register custom services
builder.Services.AddSingleton<IPartnerAuthenticationService, PartnerAuthenticationService>();
builder.Services.AddScoped<ISignatureValidationService, SignatureValidationService>();
builder.Services.AddScoped<ITransactionValidationService, TransactionValidationService>();
builder.Services.AddScoped<IDiscountCalculationService, DiscountCalculationService>();
builder.Services.AddSingleton<IPasswordEncryptionService, PasswordEncryptionService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for testing and demonstration
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
