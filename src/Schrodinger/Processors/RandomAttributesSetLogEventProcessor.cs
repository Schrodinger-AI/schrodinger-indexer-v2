using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public class RandomAttributesSetLogEventProcessor : SchrodingerProcessorBase<RandomAttributeSet>
{

    public override async Task ProcessAsync(RandomAttributeSet eventValue, LogEventContext context)
    {
        Logger.LogDebug("[RandomAttributeSet] begin");
        if (eventValue == null || context == null) return;
        
        var schrodingerId = IdGenerateHelper.GetId(context.ChainId, eventValue.Tick);
        var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(schrodingerId);
        if(schrodingerIndex == null) return;

        if (eventValue.AddedAttribute != null)
        {
            schrodingerIndex.AttributeSets.RandomAttributes.Add(
                    Mapper.Map<AttributeSet, Entities.AttributeSet>(eventValue.AddedAttribute));
        }
        
        if (eventValue.RemovedAttribute != null)
        {
            for (int i = schrodingerIndex.AttributeSets.RandomAttributes.Count - 1; i >= 0; i--)
            {
                var randomAttribute =
                    Mapper.Map<Entities.AttributeSet, AttributeSet>(schrodingerIndex.AttributeSets
                        .RandomAttributes[i]);
                if (eventValue.RemovedAttribute.Equals(randomAttribute))
                {
                    schrodingerIndex.AttributeSets.RandomAttributes.RemoveAt(i);
                }
            }
        }
        
        await SaveEntityAsync(schrodingerIndex);
        // Logger.LogDebug("[RandomAttributeSet] end, index: {index}", JsonConvert.SerializeObject(schrodingerIndex));
    }
}