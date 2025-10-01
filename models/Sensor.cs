namespace vigia_del_rio_procesamiento.models
{
    public class Sensor
    {
        public Guid Guid { get; set; } = new();
        public string? Nombre { get; set; }
    }
}
