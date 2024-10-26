using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class AdoptWithVoucherIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string VoucherId { get; set; }
    [Keyword] public string Adopter { get; set; }
    [Keyword] public string Tick { get; set; }
   
    [Keyword] public string AdoptId { get; set; }
    [Keyword] public string Rarity { get; set; }
    public List<Attribute> Attributes { get; set; }
    public int Rank { get; set; }
    public long CreatedTime { get; set; }
    public long UpdatedTime { get; set; }
}