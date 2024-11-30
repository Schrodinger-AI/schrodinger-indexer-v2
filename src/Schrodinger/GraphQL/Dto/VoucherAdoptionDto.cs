namespace Schrodinger.GraphQL.Dto;

public class VoucherAdoptionDto
{
    public string? VoucherId { get; set; }
    public string? Rarity { get; set; }
    public int Rank { get; set; }
    public string? AdoptId { get; set; }
    public string? Adopter { get; set; }
    public long CreatedTime { get; set; }
}