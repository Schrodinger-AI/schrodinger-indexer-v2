using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SchrodingerTraitValueIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Tick { get; set; }
    [Keyword] public string TraitType { get; set; } 
    [Keyword] public string Value { get; set; } 
    public long SchrodingerCount { get; set; }
}