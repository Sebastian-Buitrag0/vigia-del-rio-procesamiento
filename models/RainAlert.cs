using System.ComponentModel.DataAnnotations;

namespace vigia_del_rio_procesamiento.models
{
    public class RainAlert
    {
        [Key]
        public Guid Id { get; set; } = new();
        public Guid SensorId { get; set; }
        public string? SensorNombre { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public DateTime TriggeredAt { get; set; }
        public double Millimeters { get; set; }
        public string Codigo { get; set; } = "00";
    }
}
