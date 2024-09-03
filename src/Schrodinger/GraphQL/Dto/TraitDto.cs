namespace Schrodinger.GraphQL.Dto;

public class TraitDto
{
    public string TraitType { get; set; }
    public string Value { get; set; }
    public decimal Percent { get; set; }
    public bool IsRare { get; set; }
    
    public long SchrodingerCount { get; set; }
    public string Tick { get; set; }
}