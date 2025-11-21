using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet;
using vigia_del_rio_procesamiento.Configuration;
using vigia_del_rio_procesamiento.models;

// Este servicio se ejecutará en segundo plano durante toda la vida de la API
public class MqttClienteService(
    ILogger<MqttClienteService> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<RainAlertOptions> rainOptions,
    IOptions<MqttOptions> mqttOptions
) : BackgroundService
{
    private readonly ILogger<MqttClienteService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly RainAlertOptions _rainOptions = rainOptions.Value;
    private readonly MqttOptions _mqttOptions = mqttOptions.Value;
    private IMqttClient? _mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttOptions.Server, _mqttOptions.Port)
            .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
            .WithCleanSession()
            .WithTlsOptions(o => o.UseTls())
            .Build();

        // Evento que se dispara cuando se recibe un mensaje
        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            List<LecturaDto>? lecturasDto;
            try
            {
                lecturasDto = JsonSerializer.Deserialize<List<LecturaDto>>(
                    payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "Payload MQTT inválido.");
                return;
            }

            if (lecturasDto == null || lecturasDto.Count == 0)
                return;

            using var messageScope = _scopeFactory.CreateScope();
            var dbContext = messageScope.ServiceProvider.GetRequiredService<DataContext>();

            var eventoObjetivo = _rainOptions.EventName;
            var lecturas = new List<Lectura>();

            var sensoresSolicitados = lecturasDto
                .Where(dto =>
                    dto.Evento != null
                    && eventoObjetivo != null
                    && dto.Evento.Equals(eventoObjetivo, StringComparison.OrdinalIgnoreCase)
                )
                .Select(dto => dto.IdSensor)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sensoresSolicitados.Count == 0)
            {
                return;
            }

            var sensoresExistentes = await dbContext
                .Sensores.Where(s => s.Nombre != null && sensoresSolicitados.Contains(s.Nombre))
                .ToListAsync(stoppingToken);

            var sensoresPorNombre = new Dictionary<string, Sensor>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (var sensor in sensoresExistentes)
            {
                if (sensor.Nombre != null)
                {
                    sensoresPorNombre[sensor.Nombre] = sensor;
                }
            }

            foreach (var dto in lecturasDto)
            {
                if (
                    dto.Evento == null
                    || eventoObjetivo == null
                    || !dto.Evento.Equals(eventoObjetivo, StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                if (!sensoresPorNombre.TryGetValue(dto.IdSensor, out var sensor))
                {
                    sensor = new Sensor { Nombre = dto.IdSensor };
                    sensoresPorNombre[dto.IdSensor] = sensor;
                    dbContext.Sensores.Add(sensor);
                }

                var milimetros = _rainOptions.MillimetersPerEvent;
                if (
                    milimetros <= 0
                    && double.TryParse(
                        dto.Valor,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var parsedValor
                    )
                )
                {
                    milimetros = parsedValor;
                }

                lecturas.Add(
                    new Lectura
                    {
                        Fecha = DateTime.UtcNow,
                        Topic = topic,
                        Sensor = sensor,
                        SensorId = sensor.Id,
                        Valor = milimetros,
                        Evento = dto.Evento,
                    }
                );
            }

            if (lecturas.Count > 0)
            {
                dbContext.DatosMqtt.AddRange(lecturas);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        };

        await _mqttClient.ConnectAsync(options, stoppingToken);

        var subscribeOptions = mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(_mqttOptions.Topic))
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
        _logger.LogInformation("Cliente MQTT conectado y suscrito al tópico.");

        // Mantenemos el servicio corriendo
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        await _mqttClient.DisconnectAsync();
    }
}
