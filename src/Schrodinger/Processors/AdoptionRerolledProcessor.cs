using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class AdoptionRerolledProcessor : SchrodingerProcessorBase<AdoptionRerolled>
{
    public override async Task ProcessAsync(AdoptionRerolled eventValue, LogEventContext context)
    {
        Logger.LogDebug("[AdoptionRerolled] id:{id}", eventValue.AdoptId?.ToHex());
        try
        {
            var AdoptionRerolledIndexId = IdGenerateHelper.GetId(context.ChainId, context.Transaction.TransactionId);
            var AdoptionRerolledIndex = await GetEntityAsync<SchrodingerCancelIndex>(AdoptionRerolledIndexId);
            if (AdoptionRerolledIndex != null)
            {
                Mapper.Map(eventValue, AdoptionRerolledIndex);
            }
            else
            {
                AdoptionRerolledIndex = Mapper.Map<AdoptionRerolled, SchrodingerCancelIndex>(eventValue);
                AdoptionRerolledIndex.Id = AdoptionRerolledIndexId;
            }
            
            AdoptionRerolledIndex.From = eventValue.Account?.ToBase58();
            await SaveEntityAsync(AdoptionRerolledIndex);
            Logger.LogDebug("[AdoptionRerolled] end, index: {index}", AdoptionRerolledIndex);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[AdoptionRerolled] Exception");
            throw;
        }
    }
}