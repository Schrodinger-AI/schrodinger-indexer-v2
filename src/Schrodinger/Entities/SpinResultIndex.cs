using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SpinResultIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string SpinId { get; set; }
    [Keyword] public string Seed { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Tick { get; set; }
    public RewardType RewardType { get; set; }
    public long Amount { get; set; }
    public long CreatedTime { get; set; }
}