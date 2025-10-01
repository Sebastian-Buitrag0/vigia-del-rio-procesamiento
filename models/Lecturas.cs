namespace vigia_del_rio_procesamiento.models
{
    public class DatosMQTT
    {
        public Guid Guid { get; set; } = new();
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Topic { get; set; }
        public Guid? SensorId { get; set; }
        public Sensor? Sensor { get; set; }
        public double Valor { get; set; } = -1; //-> No hay lectura
    }
}
