using System;
using System.Threading.Tasks;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class RedeemedProcessor : SchrodingerProcessorBase<Redeemed>
{
    public override async Task ProcessAsync(Redeemed eventValue, LogEventContext context)
    {
        Logger.LogDebug("[Redeemed] begin, adoptId:{id}", eventValue.AdoptId);
        try
        {
            var redeemedIndexId = IdGenerateHelper.GetId(context.ChainId, context.Transaction.TransactionId);
            var redeemedIndex = await GetEntityAsync<RedeemedRecordIndex>(redeemedIndexId);
            if (redeemedIndex != null)
            {
                Mapper.Map(eventValue, redeemedIndex);
            }
            else
            {
                redeemedIndex = Mapper.Map<Redeemed, RedeemedRecordIndex>(eventValue);
                redeemedIndex.Id = redeemedIndexId;
            }
            await SaveEntityAsync(redeemedIndex);
            
            var id = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol);
            var adoptIndex = await GetEntityAsync<SchrodingerAdoptIndex>(id);
            adoptIndex.OutputAmount = adoptIndex.OutputAmount - eventValue.Amount;
            await SaveEntityAsync(adoptIndex);
            
            Logger.LogDebug("[Redeemed] end");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Redeemed] Exception");
            throw;
        }
    }
}