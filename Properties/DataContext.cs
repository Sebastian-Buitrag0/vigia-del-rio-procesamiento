using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using vigia_del_rio_procesamiento.models;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Lectura> DatosMqtt { get; set; }
    public DbSet<Sensor> Sensores { get; set; }
    public DbSet<RainAlert> AlertasLluvia { get; set; }
    public DbSet<RainGaugeStatus> EstadosSensores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lectura>().HasKey(l => l.Id);
        modelBuilder.Entity<RainAlert>().HasKey(a => a.Id);
        modelBuilder
            .Entity<RainAlert>()
            .HasIndex(a => new { a.SensorId, a.TriggeredAt })
            .HasDatabaseName("IX_RainAlert_Sensor_TriggeredAt");
        modelBuilder.Entity<RainGaugeStatus>().HasKey(s => s.Id);
        modelBuilder.Entity<RainGaugeStatus>().HasIndex(s => s.SensorId).IsUnique();
    }
}
