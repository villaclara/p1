namespace P1.Core.Data.Entities;

public class Race
{
    public int Id { get; set; }

    public int RaceNumber { get; set; }

    public int CircuitId { get; set; }

    public Circuit Circuit { get; set; } = default!;
}
