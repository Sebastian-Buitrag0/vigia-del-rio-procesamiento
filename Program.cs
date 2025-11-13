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

app.Run();
