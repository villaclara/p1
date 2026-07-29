using Microsoft.EntityFrameworkCore;
using P1.Core.Data.Entities;

namespace P1.Core.Data;

public class AppDbContext : DbContext
{
    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<DriverTeam> DriverTeams => Set<DriverTeam>();

    public DbSet<Constructor> Constructors => Set<Constructor>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<Circuit> Circuits => Set<Circuit>();

    public DbSet<Race> Races => Set<Race>();

    public DbSet<DriverResult> DriverResults => Set<DriverResult>();
}
