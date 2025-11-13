using System.Collections.Generic;

namespace vigia_del_rio_procesamiento.Configuration
{
    public class RainAlertOptions
    {
        public List<string> SensorNames { get; set; } = new();
        public string EventName { get; set; } = "lluvia_detectada";
        public double MillimetersPerEvent { get; set; } = 0.3;
        public double ResetBelowMillimeters { get; set; } = 80;
        public int WindowMinutes { get; set; } = 10;
        public int EvaluationIntervalSeconds { get; set; } = 30;
        public string WebhookUrl { get; set; } = string.Empty;
        public double FinalAlertMillimeters { get; set; } = 140;
        public List<RainAlertLevel> Levels { get; set; } =
            new()
            {
                new RainAlertLevel { Code = "01", ThresholdMillimeters = 80 },
                new RainAlertLevel { Code = "02", ThresholdMillimeters = 100 },
                new RainAlertLevel { Code = "03", ThresholdMillimeters = 125 },
            };
    }

    public class RainAlertLevel
    {
        public string Code { get; set; } = "01";
        public double ThresholdMillimeters { get; set; }
    }
}
