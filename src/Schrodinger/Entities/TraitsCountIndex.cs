using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class TraitsCountIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string TraitType { get; set; }
    public List<ValueInfo> Values { get; set; }
    
    public long Amount { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }

    public class ValueInfo
    {
        [Keyword] public string Value { get; set; }
        public long Amount { get; set; }
    }
}