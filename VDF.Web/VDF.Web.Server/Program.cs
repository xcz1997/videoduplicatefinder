using VDF.Web.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Removed to avoid dependency issues

// Register ScanService as Singleton
builder.Services.AddSingleton<ScanService>();

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

app.Run();
