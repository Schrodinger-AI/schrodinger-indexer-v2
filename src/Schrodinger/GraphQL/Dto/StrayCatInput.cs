namespace Schrodinger.GraphQL.Dto;

public class StrayCatInput
{
    public string Adopter { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
    public string ChainId { get; set; }
    public long? AdoptTime { get; set; }
}