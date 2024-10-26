using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SpinRewardConfigIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string TransactionId { get; set; }
    public SpinRewardConfig Config { get; set; }
    public long CreatedTime { get; set; }
    
}

public class SpinRewardConfig
{
    public RewardList RewardList { get; set; }
    public string Pool { get; set; }
}