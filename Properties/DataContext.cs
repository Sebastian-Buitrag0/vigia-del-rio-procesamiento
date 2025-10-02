using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using vigia_del_rio_procesamiento.models;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Lectura> DatosMqtt { get; set; }
    public DbSet<Sensor> Sensores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lectura>().HasKey(l => l.Id);
    }
}
