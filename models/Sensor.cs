using System.ComponentModel.DataAnnotations;

namespace vigia_del_rio_procesamiento.models
{
    public class Sensor
    {
        [Key]
        public Guid Id { get; set; } = new();
        public string? Nombre { get; set; }
    }
}
