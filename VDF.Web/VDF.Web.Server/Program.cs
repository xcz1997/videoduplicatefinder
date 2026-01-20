using VDF.Web.Server.Services;
using VDF.GUI.Utils;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        // Use camelCase for JSON serialization to match frontend conventions
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Removed to avoid dependency issues

// Load settings first before initializing services that depend on them
try {
    VDF.GUI.Data.SettingsFile.LoadSettings();
}
catch { /* Ignore settings load error, will use defaults */ }

// Initialize persistent thumbnail cache (after settings are loaded)
try {
    ThumbCacheHelpers.Provider = ThumbCacheHelpers.OpenPersistentCache();
}
catch { /* Ignore cache initialization errors */ }

// Register ScanService as Singleton
builder.Services.AddSingleton<ScanService>();

// Register SystemMonitorService as Singleton
builder.Services.AddSingleton<SystemMonitorService>();

// Configure CORS for Vite
builder.Services.AddCors(options => {
    options.AddPolicy("AllowVite",
        policy => {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseCors("AllowVite");

app.UseAuthorization();

app.MapControllers();

// Handle graceful shutdown to save thumbnail cache index
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => {
    try {
        // Flush and dispose thumbnail cache to ensure index is saved
        ThumbCacheHelpers.Provider?.Dispose();
        ThumbCacheHelpers.Provider = null;
    }
    catch { /* Ignore errors during shutdown */ }
});

app.Run();
