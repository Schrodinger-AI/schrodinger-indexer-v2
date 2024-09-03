using Nest;

namespace Schrodinger.Entities;

public class Attribute
{
    [Keyword] public string TraitType { get; set; }
    [Keyword] public string Value { get; set; }
}