using Microsoft.EntityFrameworkCore;
using P1.Core.Data.Entities;

namespace P1.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<DriverTeam> DriverTeams => Set<DriverTeam>();

    public DbSet<Constructor> Constructors => Set<Constructor>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<Circuit> Circuits => Set<Circuit>();

    public DbSet<Race> Races => Set<Race>();

    public DbSet<DriverResult> DriverResults => Set<DriverResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Season>(b =>
        {
            b.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Driver>(b =>
        {
            b.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Constructor>(b =>
        {
            b.HasKey(e => e.Id);
        });

        modelBuilder.Entity<DriverTeam>(b =>
        {
            b.HasKey(e => e.Id);

            b.HasOne(e => e.Season)
            .WithMany()
            .HasForeignKey(e => e.SeasonId);

            b.HasOne(e => e.Driver)
            .WithMany()
            .HasForeignKey(e => e.DriverId);

            b.HasOne(e => e.Constructor)
            .WithMany()
            .HasForeignKey(e => e.ConstructorId);
        });

        modelBuilder.Entity<Circuit>(b =>
        {
            b.HasKey(e => e.Id);

        });

        modelBuilder.Entity<Race>(b =>
        {
            b.HasKey(e => e.Id);

            b.HasOne(e => e.Circuit)
            .WithMany()
            .HasForeignKey(e => e.CircuitId);
        });

        modelBuilder.Entity<DriverResult>(b =>
        {
            b.HasKey(e => e.Id);

            b.HasOne(e => e.Season)
            .WithMany()
            .HasForeignKey(e => e.SeasonId);

            b.HasOne(e => e.Race)
            .WithMany()
            .HasForeignKey(e => e.RaceId);

            b.HasOne(e => e.Driver)
            .WithMany()
            .HasForeignKey(e => e.DriverId);
        });
    }
}
