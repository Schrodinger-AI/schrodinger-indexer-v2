using Nest;

namespace Schrodinger.Entities;

public class ExternalInfoDictionary
{
    [Keyword] public string Key { get; set; }
    [Keyword] public string Value { get; set; }
}