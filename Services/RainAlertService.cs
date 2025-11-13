using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using vigia_del_rio_procesamiento.Configuration;
using vigia_del_rio_procesamiento.models;

namespace vigia_del_rio_procesamiento.Services
{
    public class RainAlertService : BackgroundService
    {
        private readonly ILogger<RainAlertService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RainAlertOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IReadOnlyList<RainAlertLevel> _orderedLevels;

        public RainAlertService(
            ILogger<RainAlertService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<RainAlertOptions> options,
            IHttpClientFactory httpClientFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _orderedLevels = _options.Levels.OrderBy(l => l.ThresholdMillimeters).ToList();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AnalizarSensoresAsync(stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado analizando sensores de lluvia.");
                }

                try
                {
                    var delay = TimeSpan.FromSeconds(
                        Math.Max(5, _options.EvaluationIntervalSeconds)
                    );
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task AnalizarSensoresAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var sensoresQuery = context.Sensores.AsQueryable();
            if (_options.SensorNames.Count > 0)
            {
                sensoresQuery = sensoresQuery.Where(s =>
                    s.Nombre != null && _options.SensorNames.Contains(s.Nombre)
                );
            }

            var sensores = await sensoresQuery.ToListAsync(cancellationToken);
            if (sensores.Count == 0)
            {
                return;
            }

            var sensorIds = sensores.Select(s => s.Id).ToList();
            var estados = await context
                .EstadosSensores.Where(e => sensorIds.Contains(e.SensorId))
                .ToListAsync(cancellationToken);
            var estadosPorSensor = estados.ToDictionary(e => e.SensorId);

            var now = DateTime.UtcNow;
            var window = TimeSpan.FromMinutes(Math.Max(1, _options.WindowMinutes));
            var windowStart = now - window;

            foreach (var sensor in sensores)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sumaMm = await context
                    .DatosMqtt.Where(l =>
                        l.SensorId == sensor.Id
                        && l.Evento == _options.EventName
                        && l.Fecha >= windowStart
                    )
                    .SumAsync(l => l.Valor, cancellationToken);

                if (sumaMm < 0)
                {
                    sumaMm = 0;
                }

                _logger.LogInformation(
                    "😎😎😎 Sensor {Sensor} acumula {Milimetros} mm en los últimos {Minutos} minutos.",
                    sensor.Nombre ?? sensor.Id.ToString(),
                    Math.Round(sumaMm, 2),
                    _options.WindowMinutes
                );

                var codigoActual = DeterminarCodigo(sumaMm);

                if (!estadosPorSensor.TryGetValue(sensor.Id, out var estado))
                {
                    estado = new RainGaugeStatus
                    {
                        SensorId = sensor.Id,
                        SensorNombre = sensor.Nombre,
                        LastCode = "00",
                        LastSumMillimeters = 0,
                        LastUpdatedUtc = now,
                    };
                    context.EstadosSensores.Add(estado);
                    estadosPorSensor[sensor.Id] = estado;
                }

                if (codigoActual == estado.LastCode)
                {
                    continue;
                }

                var webhookEnviado = await EnviarWebhookAsync(codigoActual, cancellationToken);

                if (!webhookEnviado)
                {
                    continue;
                }

                estado.LastCode = codigoActual;
                estado.LastSumMillimeters = sumaMm;
                estado.LastUpdatedUtc = now;

                context.AlertasLluvia.Add(
                    new RainAlert
                    {
                        SensorId = sensor.Id,
                        SensorNombre = sensor.Nombre,
                        WindowStart = windowStart,
                        WindowEnd = now,
                        TriggeredAt = now,
                        Millimeters = sumaMm,
                        Codigo = codigoActual,
                    }
                );

                _logger.LogInformation(
                    "Sensor {Sensor} cambió a código {Codigo} con {Milimetros} mm acumulados.",
                    sensor.Nombre ?? sensor.Id.ToString(),
                    codigoActual,
                    Math.Round(sumaMm, 2)
                );
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private string DeterminarCodigo(double milimetros)
        {
            if (milimetros < _options.ResetBelowMillimeters)
            {
                return "00";
            }

            var codigo = "00";
            foreach (var level in _orderedLevels)
            {
                if (milimetros >= level.ThresholdMillimeters)
                {
                    codigo = level.Code;
                }
                else
                {
                    break;
                }
            }

            return codigo;
        }

        private async Task<bool> EnviarWebhookAsync(
            string codigo,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
            {
                _logger.LogWarning(
                    "WebhookUrl no configurado. Se omite envío del código {Codigo}.",
                    codigo
                );
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient(nameof(RainAlertService));
                var payload = JsonSerializer.Serialize(new { codigo });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "😎😎😎 Enviando código {Codigo} al webhook {Url}.",
                    codigo,
                    _options.WebhookUrl
                );

                var response = await client.PostAsync(
                    _options.WebhookUrl,
                    content,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Webhook respondió {Status} al enviar código {Codigo}.",
                        response.StatusCode,
                        codigo
                    );
                    return false;
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando webhook para código {Codigo}.", codigo);
                return false;
            }
        }
    }
}
