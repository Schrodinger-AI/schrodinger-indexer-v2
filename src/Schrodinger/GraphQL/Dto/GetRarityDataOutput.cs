namespace Schrodinger.GraphQL.Dto;

public class GetRarityDataOutput
{
    public List<RarityInfo> RarityInfo { get; set; }
}

public class RarityInfo
{
    public string Symbol { get; set; }
    public int Rank { get; set; }
    public int Generation { get; set; }
    public string AdoptId { get; set; }
    public long OutputAmount { get; set; }
    public string Adopter { get; set; }
}