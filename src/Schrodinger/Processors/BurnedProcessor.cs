using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class BurnedProcessor : TokenProcessorBase<Burned>
{
    
    public override async Task ProcessAsync(Burned eventValue, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = eventValue.Symbol;
        var owner = eventValue.Burner?.ToBase58();
        var amount = eventValue.Amount;
        try
        {
            var tick = TokenSymbolHelper.GetTickBySymbol(symbol);
            // var schrodingerIndex = await SchrodingerRepository.GetFromBlockStateSetAsync(IdGenerateHelper.GetId(chainId, tick), chainId);
            var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(IdGenerateHelper.GetId(chainId, tick));
            if (schrodingerIndex == null)
            {
                return;
            }
            
            Logger.LogDebug("[Burned] start chainId:{chainId} symbol:{symbol}, owner:{owner}, amount:{amount}", chainId, symbol, owner, amount);
            var isGen0 = TokenSymbolHelper.GetIsGen0FromSymbol(symbol);
            var holderIndex = await UpdatedHolderRelatedAsync(chainId, symbol, owner, -amount,
                0, SchrodingerConstants.Burned, context);
            var holderCountAfterUpdate = await GetSymbolHolderCountAsync(chainId, symbol);

            if (!isGen0 && holderCountAfterUpdate <= 0)
            {
                await UpdateSchrodingerCountAsync(holderIndex, tick, -1, context);
                await UpdateTraitCountAsync(chainId, symbol, context);
            }
            
            await SaveSchrodingerHolderDailyChangeAsync(symbol, owner, -amount, context);
            // Logger.LogDebug("[Burned] end chainId:{chainId} symbol:{symbol}, owner:{owner}, amount:{amount}", chainId, symbol, owner, amount);

        }
        catch (Exception e)
        {
            Logger.LogError(e, "[Burned] Exception chainId:{chainId} symbol:{symbol}, owner:{owner}, amount:{amount}", chainId, symbol, owner, amount);
            throw;
        }
    }
    
    private async Task UpdateTraitCountAsync(string chainId, string symbol, LogEventContext context)
    {
        var symbolIndex = await GetSymbolAsync(chainId, symbol);
        foreach (var traitInfo in symbolIndex.Traits)
        {
            var traitType = traitInfo.TraitType;
            var traitValue = traitInfo.Value;

            await ReduceTraitCountAsync(traitType, traitValue, chainId, context);
        }

        await ReduceGenerationCountAsync(symbolIndex.SchrodingerInfo.Gen, chainId, context);
    }
    
    private async Task ReduceTraitCountAsync(string traitType, string traitValue, string chainId, LogEventContext context)
    {
        var traitCountIndexId = IdGenerateHelper.GetTraitCountId(chainId, traitType);
        var traitCountIndex = await GetEntityAsync<TraitsCountIndex>(traitCountIndexId);
        var now = DateTimeHelper.GetCurrentTimestamp();
        if (traitCountIndex == null)
        {
            // Logger.LogError( "[Burned] TraitCountIndex Not Exist, chainId:{chainId} traitType:{type}, traitValue:{value}",  chainId, traitType, traitValue);
            return;
        }
           
        var valueInfos = traitCountIndex.Values;
        bool valueExist = false;
    
        for (int i = 0; i < valueInfos.Count; i++)
        {
            if (valueInfos[i].Value == traitValue)
            {
                valueInfos[i].Amount--;
                valueExist = true;
                break;
            }
        }
        
        if (!valueExist)
        {
            // Logger.LogError( "[Burned] Trait Value Not Exist In Trait Type, chainId:{chainId} traitType:{type}, traitValue:{value}",  chainId, traitType, traitValue);
            return;
        }
        
        traitCountIndex.Values = valueInfos;
        traitCountIndex.Amount--;
        traitCountIndex.UpdateTime = now;

        await SaveEntityAsync(traitCountIndex);
        //
        // Logger.LogDebug("[Issued] UpdateTraitCountAsync index:{holderCountBeforeUpdate}", 
        //     JsonConvert.SerializeObject(traitCountIndex));
    }
    
    private async Task ReduceGenerationCountAsync(int generation, string chainId, LogEventContext context)
    {
        var generationCountIndexId = IdGenerateHelper.GetId(chainId, generation);
        var generationCountIndex = await GetEntityAsync<GenerationCountIndex>(generationCountIndexId);
        
        if (generationCountIndex == null)
        {
            // Logger.LogError( "[Burned] GenerationCountIndex Not Exist, chainId:{chainId} generation:{generation}",  chainId, generation);
            return;
        }
        
        var now = DateTimeHelper.GetCurrentTimestamp();
        if (generationCountIndex.Count > 0)
        {
            generationCountIndex.Count --;
        }
        generationCountIndex.UpdateTime = now;
        
        await SaveEntityAsync(generationCountIndex);
    }
}