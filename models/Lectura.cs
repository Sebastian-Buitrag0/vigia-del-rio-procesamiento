using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace vigia_del_rio_procesamiento.models
{
    public class Lectura
    {
        [Key]
        public Guid Id { get; set; } = new();
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Topic { get; set; }
        public Guid? SensorId { get; set; }
        public Sensor? Sensor { get; set; }
        public double Valor { get; set; } = -1; //-> mm acumulados por evento
        public string? Evento { get; set; }
    }
}
