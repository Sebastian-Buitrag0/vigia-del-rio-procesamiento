using Microsoft.EntityFrameworkCore;
using vigia_del_rio_procesamiento.Configuration;
using vigia_del_rio_procesamiento.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Registrar el DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"))
);

builder.Services.Configure<RainAlertOptions>(builder.Configuration.GetSection("RainAlert"));
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddHttpClient();

// Registrar servicios en segundo plano
builder.Services.AddHostedService<MqttClienteService>();
builder.Services.AddHostedService<RainAlertService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Aplicar migraciones automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
    }
}

app.Run();
