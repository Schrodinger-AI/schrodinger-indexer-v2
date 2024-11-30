namespace Schrodinger.GraphQL.Dto;


public class BlindBoxListInput
{
    public string Adopter { get; set; }
    public long? AdoptTime { get; set; }
    public string? MinAmount { get; set; }
    public int? Generation { get; set; }
}