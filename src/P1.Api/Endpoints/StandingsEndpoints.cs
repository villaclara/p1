namespace P1.Api.Endpoints;

public static class StandingsEndpoints
{
    public static void MapStandingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/s")
            .WithTags("Standings");

    }

}
