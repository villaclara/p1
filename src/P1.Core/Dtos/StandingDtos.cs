namespace P1.Core.Dtos;

public class StandingDtos
{
    public record Season(int Id, int Year);

    public record Race(int Id, int Number, string Name);

    public record Driver(int Id, string Name, string Constructor, double Points);
}
