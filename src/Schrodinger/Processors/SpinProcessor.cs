using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class SpinProcessor : SchrodingerProcessorBase<Spun>
{
    public override async Task ProcessAsync(Spun eventValue, LogEventContext context)
    {
        Logger.LogDebug("[Spun] begin, seed:{symbol}", eventValue.Seed.ToHex());
        var spinIndexId = eventValue.Seed.ToHex();
            
        var spinIndex = await GetEntityAsync<SpinResultIndex>(spinIndexId);
        if (spinIndex != null)
        {
            Mapper.Map(eventValue, spinIndex);
        }
        else
        {
            spinIndex = Mapper.Map<Spun, SpinResultIndex>(eventValue);
            spinIndex.Id = spinIndexId;
            spinIndex.SpinId = spinIndexId;
            spinIndex.Seed = eventValue.Seed.ToHex();
            spinIndex.RewardType = eventValue.SpinInfo.Type;
            spinIndex.Name = eventValue.SpinInfo.Name;
            spinIndex.Amount = eventValue.SpinInfo.Amount;
            spinIndex.CreatedTime = DateTimeHelper.GetCurrentTimestamp();
        }
        await SaveEntityAsync(spinIndex);
        Logger.LogDebug("[Spun] finished, seed:{symbol}", eventValue.Seed.ToHex());
    }
}