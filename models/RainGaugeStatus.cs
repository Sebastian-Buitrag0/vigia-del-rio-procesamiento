using System.ComponentModel.DataAnnotations;

namespace vigia_del_rio_procesamiento.models
{
    public class RainGaugeStatus
    {
        [Key]
        public Guid Id { get; set; } = new();
        public Guid SensorId { get; set; }
        public string? SensorNombre { get; set; }
        public string LastCode { get; set; } = "00";
        public double LastSumMillimeters { get; set; }
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
