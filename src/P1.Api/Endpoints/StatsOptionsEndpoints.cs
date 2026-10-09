using P1.Core.Services;

namespace P1.Api.Endpoints;

public static class StatsOptionsEndpoints
{
    public static void MapStatsOptionsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/stats")
            .WithTags("StatsOptions");

        group.MapGet("/years", GetAvailableYearsStandings)
            .Produces<IList<int>>();

        group.MapGet("/races", GetDriverStandingsStats);
    }

    private async static Task<IResult> GetAvailableYearsStandings(StandingsService service, CancellationToken token = default)
    {
        var result = await service.GetSeasonsAndRaces(token);
        return TypedResults.Ok(result);
    }

    private async static Task<IResult> GetDriverStandingsStats(int year, int? raceNumber, StandingsService service, CancellationToken token)
    {
        var result = await service.GetDriverStandings(year, raceNumber, token);
        return TypedResults.Ok(result);
    }
}
