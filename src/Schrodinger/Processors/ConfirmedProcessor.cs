using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Processors;
using Schrodinger.Utils;


namespace Schrodinger.Indexer.Plugin.Processors;

public class ConfirmedProcessor : SchrodingerProcessorBase<Confirmed>
{
    public override async Task ProcessAsync(Confirmed eventValue, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = eventValue.Symbol;
        var owner = eventValue.Owner?.ToBase58();
        var adoptId = eventValue.AdoptId?.ToHex();
        Logger.LogDebug("[Confirmed] start chainId:{chainId} symbol:{symbol}, owner:{owner}, adoptId: {adoptId}", chainId, symbol, owner, adoptId);
        try
        {
            var adoptIndexId = IdGenerateHelper.GetId(chainId, symbol);
            // var adoptIndex = await SchrodingerAdoptRepository.GetFromBlockStateSetAsync(adoptIndexId, chainId);
            var adoptIndex = await GetEntityAsync<SchrodingerAdoptIndex>(adoptIndexId);
            if (adoptIndex != null)
            {
                Mapper.Map(eventValue, adoptIndex);
            }
            else
            {
                adoptIndex = Mapper.Map<Confirmed, SchrodingerAdoptIndex>(eventValue);
                adoptIndex.Id = adoptIndexId;
            }

            adoptIndex.IsConfirmed = true;
            adoptIndex.TransactionId = context.Transaction.TransactionId;
            await SaveEntityAsync(adoptIndex);
            // Logger.LogDebug("[Confirmed] end chainId:{chainId} symbol:{symbol}, owner:{owner}, adoptId: {adoptId}", chainId, symbol, owner, adoptId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Confirmed] Exception chainId:{chainId} symbol:{symbol}, owner:{owner}, adoptId: {adoptId}", chainId, symbol, owner, adoptId);
            throw;
        }
    }
}