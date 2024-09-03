using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class SchrodingerAdoptProcessor : SchrodingerProcessorBase<Adopted>
{
    public override async Task ProcessAsync(Adopted adopted, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = adopted.Symbol;
        var adoptId = adopted.AdoptId?.ToHex();
        var parent = adopted.Parent;
        Logger.LogDebug("[Adopted] start chainId:{chainId} symbol:{symbol}, adoptId:{adoptId}, parent:{parent}", chainId,
            symbol, adoptId, parent);
        try
        {
            var adopt = Mapper.Map<Adopted, SchrodingerAdoptIndex>(adopted);

            adopt.Id = IdGenerateHelper.GetId(chainId, symbol);
            adopt.AdoptTime = context.Block.BlockTime;
            adopt.ParentInfo = await getSchrodingerInfo(chainId, parent);
            adopt.TransactionId = context.Transaction.TransactionId;

            await SaveEntityAsync(adopt);
            Logger.LogDebug("[Adopted] end chainId:{chainId} symbol:{symbol}, adoptId:{adoptId}, parent:{parent}, transactionId:{TransactionId}", chainId, symbol,
                adoptId, parent, adopt.TransactionId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Adopted] Exception chainId:{chainId} symbol:{symbol}, adoptId:{adoptId}, parent:{parent}", chainId,
                symbol,
                adoptId, parent);
            throw;
        }
    }
    
    private async Task<SchrodingerInfo> getSchrodingerInfo(string chainId, string symbol)
    {
        var symbolId = IdGenerateHelper.GetId(chainId, symbol);
        var symbolIndex = await GetEntityAsync<SchrodingerSymbolIndex>(symbolId);
        return symbolIndex?.SchrodingerInfo ?? new SchrodingerInfo();
    }
}