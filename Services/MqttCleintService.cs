using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using vigia_del_rio_procesamiento.models;

// Este servicio se ejecutará en segundo plano durante toda la vida de la API
public class MqttClienteService(
    ILogger<MqttClienteService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private readonly ILogger<MqttClienteService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private IMqttClient? _mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("mqtt.vigiadelrio.website", 8883)
            .WithCredentials("vigia-del-rio", "Sebas_1006")
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

            var lecturas = new List<Lectura>(lecturasDto.Count);
            foreach (var dto in lecturasDto)
            {
                double valorParsed = -1;
                if (
                    !string.IsNullOrEmpty(dto.Valor)
                    && double.TryParse(
                        dto.Valor,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var v
                    )
                )
                {
                    valorParsed = v;
                }

                var sensor = await dbContext.Sensores.FirstOrDefaultAsync(
                    s => s.Nombre == dto.IdSensor,
                    stoppingToken
                );

                lecturas.Add(
                    new Lectura
                    {
                        Fecha = dto.DataTime,
                        Topic = topic,
                        SensorId = sensor?.Id,
                        Valor = valorParsed,
                    }
                );
            }

            dbContext.DatosMqtt.AddRange(lecturas);
            await dbContext.SaveChangesAsync(stoppingToken);
        };

        await _mqttClient.ConnectAsync(options, stoppingToken);

        var subscribeOptions = mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("test")) // <-- Cambiar a "test"
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
