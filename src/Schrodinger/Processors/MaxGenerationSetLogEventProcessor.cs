using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public class MaxGenerationSetLogEventProcessor: SchrodingerProcessorBase<MaxGenerationSet>
{
    public override async Task ProcessAsync (MaxGenerationSet eventValue, LogEventContext context)
    {
        Logger.LogDebug("[MaxGenerationSet] begin");
        if (eventValue == null || context == null) return;
        
        var schrodingerId = IdGenerateHelper.GetId(context.ChainId, eventValue.Tick);
        var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(schrodingerId);
        if(schrodingerIndex == null) return;

        schrodingerIndex.MaxGeneration = eventValue.Gen;
        await SaveEntityAsync(schrodingerIndex);
        // Logger.LogDebug("[MaxGenerationSet] end, index: {index}", schrodingerIndex);
    }
}