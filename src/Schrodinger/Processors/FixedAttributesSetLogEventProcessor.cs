using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public class FixedAttributesSetLogEventProcessor : SchrodingerProcessorBase<FixedAttributeSet>
{
    public override async Task ProcessAsync(FixedAttributeSet eventValue, LogEventContext context)
    {
        Logger.LogDebug("[FixedAttributeSet] begin");
        if (eventValue == null || context == null) return;
        
        var schrodingerId = IdGenerateHelper.GetId(context.ChainId, eventValue.Tick);
        var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(schrodingerId);
        if(schrodingerIndex == null) return;

        if (eventValue.AddedAttribute != null)
        {
            schrodingerIndex.AttributeSets.FixedAttributes.Add(
                    Mapper.Map<AttributeSet, Schrodinger.Entities.AttributeSet>(eventValue.AddedAttribute));
        }

        if (eventValue.RemovedAttribute != null)
        {
            for (int i = schrodingerIndex.AttributeSets.FixedAttributes.Count - 1; i >= 0; i--)
            {
                var fixedAttribute =
                    Mapper.Map<Schrodinger.Entities.AttributeSet, AttributeSet>(schrodingerIndex.AttributeSets
                        .FixedAttributes[i]);
                if (eventValue.RemovedAttribute.Equals(fixedAttribute))
                {
                    schrodingerIndex.AttributeSets.FixedAttributes.RemoveAt(i);
                }
            }
        }

        await SaveEntityAsync(schrodingerIndex);
        // Logger.LogDebug("[FixedAttributeSet] end, index: {index}", schrodingerIndex);
    }
}