using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class AdoptionUpdatedIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string AdoptId { get; set; }
    [Keyword] public string Tick { get; set; }
    [Keyword] public string Parent { get; set; }
    [Keyword] public string Ancestor { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Issuer { get; set; }
    [Keyword] public string Owner { get; set; }
    [Keyword] public string Deployer { get; set; }
    [Keyword] public string Adopter { get; set; }
    [Keyword] public string TokenName { get; set; }
    [Keyword] public string TransactionId { get; set; }
    
    public long InputAmount { get; set; }
    public long LossAmount { get; set; }
    public long CommissionAmount { get; set; }
    public long OutputAmount { get; set; }
    public int Gen { get; set; }
    public int ParentGen { get; set; }
    public DateTime AdoptTime { get; set; }
}