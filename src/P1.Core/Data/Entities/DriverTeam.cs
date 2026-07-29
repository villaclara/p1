namespace P1.Core.Data.Entities;

public class DriverTeam
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    public int ConstructorId { get; set; }

    public int SeasonId { get; set; }

    public Driver Driver { get; set; } = default!;

    public Constructor Constructor { get; set; } = default!;

    public Season Season { get; set; } = default!;
}
