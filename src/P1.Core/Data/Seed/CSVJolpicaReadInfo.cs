#pragma warning disable
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace P1.Core.Data.Seed;

/*
 * This code was used to read the data from db dump of jolpica
 * It reads different files and converts them into objects of one class F1SeedRow
 * with needed data for me. 
 * Later that data is written into single csv file - f1HistoryData.csv
 * /
/* 
* Code below is example of reading properties in Program.cs
* var csvFolder = args.Length > 0
*          ? args[0]
*          : Directory.GetCurrentDirectory();
*
* if (!File.Exists(Path.Combine(csvFolder, "formula_one_season.csv"))) 
* csvFolder = Directory.GetCurrentDirectory();
* 
* int? year = 2020;
* string outputPath;
* 
* if (args.Length >= 3 && int.TryParse(args[2], out var parsedYear))
* {
* year = parsedYear;
* outputPath = args[1];
* }
* else if (args.Length >= 2 && int.TryParse(args[1], out parsedYear))
* {
* year = parsedYear;
* outputPath = Path.Combine(csvFolder, $"f1_{parsedYear}_seed_data.csv");
* }
* else if (args.Length >= 2)
* {
* outputPath = args[1];
* }
* else
* {
* outputPath = Path.Combine(csvFolder, "f1_all_seasons_seed_data.csv");
* } 
*/
internal class CSVJolpicaReadInfo
{
    private static readonly HashSet<string> ResultSessionTypes = new(StringComparer.Ordinal)
    {
        "R",  // main race
        "SR"  // sprint race
    };

    /// <summary>
    /// Builds seed rows for all seasons, or for one season when <paramref name="year"/> is set.
    /// </summary>
    public static IReadOnlyList<F1SeedRow> BuildRows(string csvFolder, int? year = 2020)
    {
        var seasons = ReadCsv<SeasonRow>(Path.Combine(csvFolder, "formula_one_season.csv"));
        var seasonsById = seasons.ToDictionary(s => s.Id);

        if (year.HasValue && !seasons.Any(s => s.Year >= year.Value))
            throw new InvalidOperationException($"Season {year.Value} not found in CSV data.");

        var allowedSeasonIds = year.HasValue
            ? seasons.Where(s => s.Year >= year.Value).Select(s => s.Id).ToHashSet()
            : seasons.Select(s => s.Id).ToHashSet();

        var rounds = ReadCsv<RoundRow>(Path.Combine(csvFolder, "formula_one_round.csv"))
            .Where(r => allowedSeasonIds.Contains(r.SeasonId) && r.IsCancelled != "t")
            .ToDictionary(r => r.Id);

        var circuits = ReadCsv<CircuitRow>(Path.Combine(csvFolder, "formula_one_circuit.csv"))
            .ToDictionary(c => c.Id);

        var drivers = ReadCsv<DriverRow>(Path.Combine(csvFolder, "formula_one_driver.csv"))
            .ToDictionary(d => d.Id);

        var teams = ReadCsv<TeamRow>(Path.Combine(csvFolder, "formula_one_team.csv"))
            .ToDictionary(t => t.Id);

        // Load all team-driver links for selected seasons (not just one season).
        var teamDrivers = ReadCsv<TeamDriverRow>(Path.Combine(csvFolder, "formula_one_teamdriver.csv"))
            .Where(td => allowedSeasonIds.Contains(td.SeasonId))
            .ToDictionary(td => td.Id);

        var roundEntries = ReadCsv<RoundEntryRow>(Path.Combine(csvFolder, "formula_one_roundentry.csv"))
            .Where(re => rounds.ContainsKey(re.RoundId))
            .ToDictionary(re => re.Id);

        var resultSessions = ReadCsv<SessionRow>(Path.Combine(csvFolder, "formula_one_session.csv"))
            .Where(s => rounds.ContainsKey(s.RoundId)
                        && ResultSessionTypes.Contains(s.Type)
                        && s.IsCancelled != "t")
            .ToDictionary(s => s.Id);

        var sessionEntries = ReadCsv<SessionEntryRow>(Path.Combine(csvFolder, "formula_one_sessionentry.csv"))
            .Where(se => resultSessions.ContainsKey(se.SessionId));

        var rows = new List<F1SeedRow>();

        foreach (var entry in sessionEntries)
        {
            var session = resultSessions[entry.SessionId];
            var round = rounds[session.RoundId];

            if (!seasonsById.TryGetValue(round.SeasonId, out var season))
                continue;

            if (!roundEntries.TryGetValue(entry.RoundEntryId, out var roundEntry))
                continue;

            if (!teamDrivers.TryGetValue(roundEntry.TeamDriverId, out var teamDriver))
                continue;

            if (!drivers.TryGetValue(teamDriver.DriverId, out var driver))
                continue;

            if (!teams.TryGetValue(teamDriver.TeamId, out var team))
                continue;

            circuits.TryGetValue(round.CircuitId, out var circuit);

            rows.Add(new F1SeedRow
            {
                Id = entry.Id,
                DriverId = teamDriver.DriverId,
                SeasonId = season.Id,
                Year = season.Year,
                RaceId = round.Id,
                RaceName = round.Name,
                RaceNumber = round.Number.Value,
                RaceDate = round.Date,
                CircuitId = round.CircuitId,
                CircuitName = circuit?.Name ?? "",
                CircuitCountry = circuit?.Country ?? "",
                RoundId = round.Id,
                SessionId = session.Id,
                SessionType = session.Type,
                SessionName = session.Type == "SR" ? "Sprint" : "Race",
                IsSprint = session.Type == "SR",
                FinishPosition = entry.Position,
                PointsScored = entry.Points,
                DriverFirstName = driver.Forename,
                DriverLastName = driver.Surname,
                DriverCode = driver.Abbreviation,
                DriverNumber = roundEntry.CarNumber > 0
                    ? roundEntry.CarNumber
                    : driver.PermanentCarNumber,
                ConstructorId = teamDriver.TeamId,
                ConstructorName = team.Name,
                DriverTeamId = teamDriver.Id,
                GridPosition = entry.Grid,
                LapsCompleted = entry.LapsCompleted,
                IsClassified = entry.IsClassified
            });
        }

        return rows
            .OrderBy(r => r.Year)
            .ThenBy(r => r.RaceNumber)
            .ThenBy(r => r.IsSprint ? 0 : 1)
            .ThenBy(r => r.FinishPosition ?? int.MaxValue)
            .ToList();
    }

    public static void WriteCsv(string csvFolder, string outputPath, int? year = null)
    {
        var rows = BuildRows(csvFolder, year);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(rows);

        var label = year.HasValue ? year.Value.ToString() : "all seasons";

        Console.WriteLine($"Wrote {rows.Count} rows to {outputPath} ({label})");
        Console.WriteLine($"  Seasons: {rows.Select(r => r.Year).Distinct().Count()}");
        Console.WriteLine($"  Main races (R): {rows.Count(r => !r.IsSprint)}");
        Console.WriteLine($"  Sprint races (SR): {rows.Count(r => r.IsSprint)}");
        Console.WriteLine($"  Unique drivers: {rows.Select(r => r.DriverId).Distinct().Count()}");
        Console.WriteLine($"  Unique rounds: {rows.Select(r => r.RaceId).Distinct().Count()}");
    }

    private static List<T> ReadCsv<T>(string path) where T : class
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<T>().ToList();
    }
}

public sealed class F1SeedRow
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public int SeasonId { get; set; }
    public int Year { get; set; }
    public int RaceId { get; set; }
    public string RaceName { get; set; } = "";
    public int RaceNumber { get; set; }
    public string RaceDate { get; set; } = "";
    public int CircuitId { get; set; }
    public string CircuitName { get; set; } = "";
    public string CircuitCountry { get; set; } = "";
    public int RoundId { get; set; }
    public int SessionId { get; set; }
    public string SessionType { get; set; } = "";
    public string SessionName { get; set; } = "";
    public bool IsSprint { get; set; }
    public int? FinishPosition { get; set; }
    public double? PointsScored { get; set; }
    public string DriverFirstName { get; set; } = "";
    public string DriverLastName { get; set; } = "";
    public string DriverCode { get; set; } = "";
    public int? DriverNumber { get; set; }
    public int ConstructorId { get; set; }
    public string ConstructorName { get; set; } = "";
    public int DriverTeamId { get; set; }
    public int? GridPosition { get; set; }
    public int? LapsCompleted { get; set; }
    public string IsClassified { get; set; }
}

public sealed class SeasonRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("api_id")]
    public string ApiId { get; set; }

    [Name("championship_system_id")]
    public int ChampionshipSystemId { get; set; }

    [Name("wikipedia")]
    public string Wikipedia { get; set; }

    [Name("year")]
    public int Year { get; set; }
}

public sealed class RoundRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("circuit_id")]
    public int CircuitId { get; set; }

    [Name("date")]
    public string Date { get; set; } = "";

    [Name("is_cancelled")]
    public string IsCancelled { get; set; }

    [Name("name")]
    public string Name { get; set; } = "";

    [Name("number")]
    public int? Number { get; set; }

    [Name("season_id")]
    public int SeasonId { get; set; }
}

public sealed class SessionRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("is_cancelled")]
    public string IsCancelled { get; set; }

    [Name("round_id")]
    public int RoundId { get; set; }

    [Name("type")]
    public string Type { get; set; } = "";
}

public sealed class SessionEntryRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("grid")]
    public int? Grid { get; set; }

    [Name("is_classified")]
    public string IsClassified { get; set; }

    [Name("laps_completed")]
    public int? LapsCompleted { get; set; }

    [Name("points")]
    public double? Points { get; set; }

    [Name("position")]
    public int? Position { get; set; }

    [Name("round_entry_id")]
    public int RoundEntryId { get; set; }

    [Name("session_id")]
    public int SessionId { get; set; }
}

public sealed class RoundEntryRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("car_number")]
    public int? CarNumber { get; set; }

    [Name("round_id")]
    public int RoundId { get; set; }

    [Name("team_driver_id")]
    public int TeamDriverId { get; set; }
}

public sealed class TeamDriverRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("driver_id")]
    public int DriverId { get; set; }

    [Name("season_id")]
    public int SeasonId { get; set; }


    [Name("team_id")]
    public int TeamId { get; set; }
}

public sealed class DriverRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("abbreviation")]
    public string Abbreviation { get; set; } = "";

    [Name("forename")]
    public string Forename { get; set; } = "";

    [Name("permanent_car_number")]
    public int? PermanentCarNumber { get; set; }

    [Name("surname")]
    public string Surname { get; set; } = "";
}

public sealed class CircuitRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("country")]
    public string Country { get; set; } = "";

    [Name("name")]
    public string Name { get; set; } = "";
}

public sealed class TeamRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("name")]
    public string Name { get; set; } = "";
}
