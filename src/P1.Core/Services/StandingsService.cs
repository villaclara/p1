using Microsoft.EntityFrameworkCore;
using P1.Core.Data;
using P1.Core.Dtos;

namespace P1.Core.Services;

public class StandingsService
{
    private readonly AppDbContext _db;

    public StandingsService(AppDbContext ctx)
        => _db = ctx;

    public async Task<Dictionary<int, IEnumerable<StandingDtos.Race>>> GetSeasonsAndRaces(CancellationToken token = default)
    {
        var seasons = await _db.Seasons
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Year, token);

        var races = await _db.Races
            .AsNoTracking()
            .OrderBy(r => r.RaceNumber)
            .ToListAsync(token);

        var result = races
            .GroupBy(r => r.SeasonId)
            .Where(r => seasons.ContainsKey(r.Key))
            .ToDictionary(
                g => seasons[g.Key],
                g => g.Select(e => new StandingDtos.Race(e.Id, e.RaceNumber, e.RaceName)));

        return result;
    }

    public async Task<IEnumerable<StandingDtos.Driver>> GetDriverStandings(int year, int? raceNumber, CancellationToken token = default)
    {
        // There will be no more than 50 races per season for sure
        raceNumber ??= 50;

        var standings = await _db.DriverResults
            .AsNoTracking()
            .AsSplitQuery()
            .Where(dr => dr.Race.Season.Year == year && dr.Race.RaceNumber <= raceNumber)
            .GroupBy(dr => dr.Driver)
            .Select(g => new
            {
                Driver = g.Key,
                TotalPoints = g.Sum(dr => dr.PointsScored),
                ConstructorName = _db.DriverTeams
                    .Where(dt => dt.DriverId == g.Key.Id && dt.Season.Year == year)
                    .Select(dt => dt.Constructor.Name)
                    .FirstOrDefault() ?? "Unknown"
            })
            .OrderByDescending(x => x.TotalPoints)
            .Select(x => new StandingDtos.Driver(
                x.Driver.Id,
                $"{x.Driver.FirstName} {x.Driver.LastName}",
                x.ConstructorName,
                x.TotalPoints
            ))
            .ToListAsync(token);

        return standings;
    }
}
