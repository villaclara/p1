namespace P1.Core.Data.Entities;

public class Driver
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Code { get; set; }
}
