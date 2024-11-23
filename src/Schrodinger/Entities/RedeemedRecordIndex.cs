using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class RedeemedRecordIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string AdoptId { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Tick { get; set; }
    public long Amount { get; set; }
    public long Level { get; set; }
}