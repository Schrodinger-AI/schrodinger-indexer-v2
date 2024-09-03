using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class TransferredProcessor : TokenProcessorBase<Transferred>
{
    public override async Task ProcessAsync(Transferred eventValue, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = eventValue.Symbol;
        var oldOwner = eventValue.From?.ToBase58();
        var newOwner = eventValue.To?.ToBase58();
        var amount = eventValue.Amount;
        try
        {
            var tick = TokenSymbolHelper.GetTickBySymbol(symbol);
            var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(IdGenerateHelper.GetId(chainId, tick));
            if (schrodingerIndex == null)
            {
                return;
            }
            
            Logger.LogDebug("[Transferred] start chainId:{chainId} symbol:{symbol}, newOwner:{newOwner}, oldOwner:{oldOwner}, amount:{amount}", chainId, symbol, newOwner, oldOwner, amount);
            await UpdatedHolderRelatedAsync(chainId, symbol, oldOwner, -amount, 
                0, SchrodingerConstants.TransferredFrom, context);
            await UpdatedHolderRelatedAsync(chainId, symbol, newOwner, amount, 
                amount, SchrodingerConstants.TransferredTo, context);
            await SaveSchrodingerHolderDailyChangeAsync(symbol, oldOwner, -amount, context);
            await SaveSchrodingerHolderDailyChangeAsync(symbol, newOwner, amount, context);
            
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Transferred] Exception chainId:{chainId} symbol:{symbol}, newOwner:{owner}, oldOwner:{oldOwner}, amount:{amount}", chainId, symbol, newOwner, oldOwner, amount);
            throw;
        }
    }
}