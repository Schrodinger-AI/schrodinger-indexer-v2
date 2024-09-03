using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SchrodingerResetIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Ancestor { get; set; }
    [Keyword] public string Recipient { get; set; }
    public long Amount { get; set; }
}