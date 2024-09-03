using AeFinder.Sdk.Entities;

namespace Schrodinger.Entities;

public class GenerationCountIndex : AeFinderEntity, IAeFinderEntity
{
    public int Generation { get; set; }
    public long Count { get; set; }
    
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
}