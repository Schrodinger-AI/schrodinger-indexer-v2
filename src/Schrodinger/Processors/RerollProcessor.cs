using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Indexer.Plugin;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public class RerolledProcessor : SchrodingerProcessorBase<Rerolled>
{
    public override async Task ProcessAsync(Rerolled eventValue, LogEventContext context)
    {
        Logger.LogDebug("[Rerolled] begin, symbol:{symbol}", eventValue.Symbol);
        try
        {
            var rerolledIndexId = IdGenerateHelper.GetId(context.ChainId, context.Transaction.TransactionId);
            var rerolledIndex = await GetEntityAsync<SchrodingerResetIndex>(rerolledIndexId);
            if (rerolledIndex != null)
            {
                Mapper.Map(eventValue, rerolledIndex);
            }
            else
            {
                rerolledIndex = Mapper.Map<Rerolled, SchrodingerResetIndex>(eventValue);
                rerolledIndex.Id = rerolledIndexId;
            }
            await SaveEntityAsync(rerolledIndex);
            // Logger.LogDebug("[Rerolled] end, index: {index}", rerolledIndex);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Rerolled] Exception");
            throw;
        }
    }
}