namespace P1.Core.Data.Entities;

public class Race
{
    public int Id { get; set; }

    public int RaceNumber { get; set; }

    public string RaceName { get; set; } = default!;

    public int CircuitId { get; set; }

    public int SeasonId { get; set; }

    public Circuit Circuit { get; set; } = default!;

    public Season Season { get; set; } = default!;
}
