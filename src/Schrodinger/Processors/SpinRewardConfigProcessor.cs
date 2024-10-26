using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class SpinRewardConfigProcessor : SchrodingerProcessorBase<RewardConfigSet>
{
    public override async Task ProcessAsync(RewardConfigSet eventValue, LogEventContext context)

    {
        Logger.LogDebug("[RewardConfigSet]");
            
        // var rewardConfigIndex = Mapper.Map<RewardConfigSet, SpinRewardConfigIndex>(eventValue);
        var rewardConfigIndex = new  SpinRewardConfigIndex();
        rewardConfigIndex.Id = Guid.NewGuid().ToString();
        rewardConfigIndex.TransactionId = context.Transaction.TransactionId;
        rewardConfigIndex.Config = new SpinRewardConfig
        {
            Pool = eventValue.Pool.ToBase58(),
            RewardList = eventValue.List
        };
        rewardConfigIndex.CreatedTime = DateTimeHelper.GetCurrentTimestamp();
       
        await SaveEntityAsync(rewardConfigIndex);
        Logger.LogDebug("[RewardConfigSet Finished]");
    }
}