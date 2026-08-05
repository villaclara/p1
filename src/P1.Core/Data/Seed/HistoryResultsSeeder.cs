using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using P1.Core.Data.Entities;
using System.Globalization;

namespace P1.Core.Data.Seed;

public class HistoryResultsSeeder
{
    private readonly AppDbContext _context;

    public HistoryResultsSeeder(AppDbContext dbContext)
    {
        _context = dbContext;
    }

    public async Task SeedAsync(bool recreateSchema = false)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            string baseDir = AppContext.BaseDirectory;

            // Combine with the relative folder structure inside your Class Library
            string filePath = Path.Combine(baseDir, "Data", "Seed", "f1HistoryResults.csv");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found at: {filePath}");
            }

            var rows = ReadSeedCsv(filePath);
            if (rows.Count == 0)
                throw new InvalidOperationException($"No rows found in {filePath}");

            if (recreateSchema)
            {
                await _context.Database.EnsureDeletedAsync();
            }

            await _context.Database.EnsureCreatedAsync();

            var seasons = rows
                .GroupBy(r => r.SeasonId)
                .Select(g => new Season { Id = g.Key, Year = g.First().Year })
                .ToList();

            var circuits = rows
                .GroupBy(r => r.CircuitId)
                .Select(g => new Circuit
                {
                    Id = g.Key,
                    Name = g.First().CircuitName,
                    Country = g.First().CircuitCountry
                })
                .ToList();

            var constructors = rows
                .GroupBy(r => r.ConstructorId)
                .Select(g => new Constructor { Id = g.Key, Name = g.First().ConstructorName })
                .ToList();

            var drivers = rows
                .GroupBy(r => r.DriverId)
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.DriverNumber.HasValue).First();
                    return new Driver
                    {
                        Id = g.Key,
                        FirstName = best.DriverFirstName,
                        LastName = best.DriverLastName,
                        Code = best.DriverCode,
                        Number = best.DriverNumber.Value
                    };
                })
                .ToList();

            var races = rows
                .GroupBy(r => r.RaceId)
                .Select(g => new Race
                {
                    Id = g.Key,
                    RaceNumber = g.First().RaceNumber,
                    CircuitId = g.First().CircuitId
                })
                .ToList();

            var driverTeams = rows
                .GroupBy(r => r.DriverTeamId)
                .Select(g => new DriverTeam
                {
                    Id = g.Key,
                    DriverId = g.First().DriverId,
                    ConstructorId = g.First().ConstructorId,
                    SeasonId = g.First().SeasonId
                })
                .ToList();

            // Main race + sprint results share the same RaceId (round).
            // Id comes from sessionentry and is unique per session result.
            var driverResults = rows.Select(r => new DriverResult
            {
                Id = r.Id,
                DriverId = r.DriverId,
                SeasonId = r.SeasonId,
                RaceId = r.RaceId,
                FinishPosition = r.FinishPosition.Value,
                PointsScored = r.PointsScored.Value
            }).ToList();

            // 1. Circuits
            await EnableIdentityInsertAsync("Circuits");
            await _context.Circuits.AddRangeAsync(circuits);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("Circuits");

            // 2. Constructors
            await EnableIdentityInsertAsync("Constructors");
            await _context.Constructors.AddRangeAsync(constructors);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("Constructors");

            // 3. Seasons
            await EnableIdentityInsertAsync("Seasons");
            await _context.Seasons.AddRangeAsync(seasons);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("Seasons");

            // 4. Drivers
            await EnableIdentityInsertAsync("Drivers");
            await _context.Drivers.AddRangeAsync(drivers);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("Drivers");

            // 5. Races (Depends on Circuits)
            await EnableIdentityInsertAsync("Races");
            await _context.Races.AddRangeAsync(races);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("Races");

            // 6. DriverTeams (Depends on Drivers, Constructors, Seasons)
            await EnableIdentityInsertAsync("DriverTeams");
            await _context.DriverTeams.AddRangeAsync(driverTeams);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("DriverTeams");

            // 7. DriverResults (Depends on Drivers, Seasons, Races)
            await EnableIdentityInsertAsync("DriverResults");
            await _context.DriverResults.AddRangeAsync(driverResults);
            await _context.SaveChangesAsync();
            await DisableIdentityInsertAsync("DriverResults");

            // Commit the entire transaction
            await transaction.CommitAsync();

            await _context.SaveChangesAsync();

            Console.WriteLine($"Seeded {seasons.Count} season(s)");
            Console.WriteLine($"Seeded {circuits.Count} circuit(s)");
            Console.WriteLine($"Seeded {constructors.Count} constructor(s)");
            Console.WriteLine($"Seeded {drivers.Count} driver(s)");
            Console.WriteLine($"Seeded {races.Count} race(s)");
            Console.WriteLine($"Seeded {driverTeams.Count} driver-team link(s)");
            Console.WriteLine($"Seeded {driverResults.Count} driver result(s)");
            Console.WriteLine($"  Main race results: {rows.Count(r => !r.IsSprint)}");
            Console.WriteLine($"  Sprint results: {rows.Count(r => r.IsSprint)}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Exception occurred when seeding db with history results - {ex.Message}");
        }
    }

    private Task EnableIdentityInsertAsync(string tableName) =>
        _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] ON;");

    private Task DisableIdentityInsertAsync(string tableName) =>
        _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] OFF;");

    private static List<F1SeedRow> ReadSeedCsv(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return [.. csv.GetRecords<F1SeedRow>()];
    }
}
