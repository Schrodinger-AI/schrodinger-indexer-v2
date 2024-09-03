using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SchrodingerCancelIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string AdoptId { get; set; }
    [Keyword] public string From { get; set; }
    public long Amount { get; set; }
}