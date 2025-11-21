namespace vigia_del_rio_procesamiento.Configuration
{
    public class MqttOptions
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 1883;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Topic { get; set; } = "test";
    }
}
