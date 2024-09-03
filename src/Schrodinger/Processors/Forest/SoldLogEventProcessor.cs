using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors.Forest;

public class SoldLogEventProcessor :  LogEventProcessorBase<Sold>
{
    public override string GetContractAddress(string chainId)
    {
        return chainId switch
        {
            "tDVV" => "2cGT3RZZy6UJJ3eJPZdWMmuoH2TZBihvMtAtKvLJUaBnvskK2x",
            "tDVW" => "zv7YnQ2dLM45ssfifN1dpwqBwdxH13pqGm9GDH6peRdH8F3hD",
            _ => string.Empty
        };
    }

    public static decimal ToPrice(long amount, int decimals)
    {
        return amount / (decimal)Math.Pow(10, decimals);
    }

    public override async Task ProcessAsync(Sold eventValue, LogEventContext context)
    {
        
        Logger.LogDebug("[HandleEventAsyncSold], symbol={id}", eventValue.NftSymbol);
            
        var nftInfoIndexId = "";

        nftInfoIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol);

        if (nftInfoIndexId.IsNullOrEmpty())
        {
            Logger.LogError("eventValue.NftSymbol is not nft, symbol={symbol}", eventValue.NftSymbol);
            return;
        }
        
        var totalPrice = ToPrice(eventValue.PurchaseAmount, TokenHelper.GetDecimal(eventValue.PurchaseSymbol));
        var totalCount = (int)TokenHelper.GetIntegerDivision(eventValue.NftQuantity, TokenHelper.GetDecimal(eventValue.NftSymbol));
        var singlePrice = CalSinglePrice(totalPrice,
            totalCount);

        // NFT activity
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol, "SOLD", context.Transaction.TransactionId, Guid.NewGuid());
        var index = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Sale,
            From = FullAddressHelper.ToFullAddress(eventValue.NftFrom.ToBase58(), context.ChainId),
            To = FullAddressHelper.ToFullAddress(eventValue.NftTo.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.NftQuantity, TokenHelper.GetDecimal(eventValue.NftSymbol)),
            Price = singlePrice,
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = nftInfoIndexId
        };
        
        
        var nftActivityIndexExists =
            await GetEntityAsync<NFTActivityIndex>(nftActivityIndexId);
        if (nftActivityIndexExists != null)
        {
            Logger.LogWarning("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}", nftActivityIndexId);
            return;
        }
        
        await SaveEntityAsync(index);
        Logger.LogDebug("HandleEventAsyncSold finished, index: {index}", index);
    }
    
    
    private decimal CalSinglePrice(decimal totalPrice, int count)
    {
        return Math.Round(totalPrice / Math.Max(1, count), 8);
    }
}