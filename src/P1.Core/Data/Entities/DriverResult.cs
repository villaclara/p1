namespace P1.Core.Data.Entities;

public class DriverResult
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    public int SeasonId { get; set; }

    public int RaceId { get; set; }

    public int FinishPosition { get; set; }

    public double PointsScored { get; set; }

    public Driver Driver { get; set; } = default!;

    public Season Season { get; set; } = default!;

    public Race Race { get; set; } = default!;
}
