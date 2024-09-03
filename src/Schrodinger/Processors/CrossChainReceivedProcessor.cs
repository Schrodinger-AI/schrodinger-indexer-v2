using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class CrossChainReceivedProcessor : TokenProcessorBase<CrossChainReceived>
{
    public override async Task ProcessAsync(CrossChainReceived eventValue, LogEventContext context)
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

            Logger.LogDebug("[CrossChainReceived] start chainId:{chainId} symbol:{symbol}, newOwner:{newOwner}, oldOwner:{oldOwner}, amount:{amount}", chainId, symbol, newOwner, oldOwner, amount);
            var isGen0 = TokenSymbolHelper.GetIsGen0FromSymbol(symbol);
            var holderCountBeforeUpdate = await GetSymbolHolderCountAsync(chainId, symbol);
            var holderIndex = await UpdatedHolderRelatedAsync(chainId, symbol, newOwner, amount, 
                amount, SchrodingerConstants.CrossChainReceived, context);
            
            if (!isGen0 && holderCountBeforeUpdate <= 0)
            {
                await UpdateSchrodingerCountAsync(holderIndex, tick, 1, context);
                await UpdateTraitCountAsync(chainId, symbol, context);
            }
            await SaveSchrodingerHolderDailyChangeAsync(symbol, newOwner, amount, context);
            // Logger.LogDebug("[CrossChainReceived] end chainId:{chainId} symbol:{symbol}, newOwner:{newOwner}, oldOwner:{oldOwner}, amount:{amount}", chainId, symbol, newOwner, oldOwner, amount);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[CrossChainReceived] Exception chainId:{chainId} symbol:{symbol}, newOwner:{owner}, oldOwner:{oldOwner}, amount:{amount}", chainId, symbol, newOwner, oldOwner, amount);
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

            await AddTraitCountAsync(traitType, traitValue, chainId, context);
        }
        await UpdateGenerationCountAsync(symbolIndex.SchrodingerInfo.Gen, chainId, context);
    }
    
    private async Task AddTraitCountAsync(string traitType, string traitValue, string chainId, LogEventContext context)
    {
        if (traitType.Contains("Random Attributes") || traitType.Contains("Fixed Attributes"))
        {
            return;
        }
        
        var traitCountIndexId = IdGenerateHelper.GetTraitCountId(chainId, traitType);
        var traitCountIndex = await GetEntityAsync<TraitsCountIndex>(traitCountIndexId);
        var now = DateTimeHelper.GetCurrentTimestamp();
        if (traitCountIndex == null)
        {
            traitCountIndex = new TraitsCountIndex
            {
                Id = traitCountIndexId,
                TraitType = traitType,
                CreateTime = now,
                UpdateTime = now,
                Amount = 1,
                Values = new List<TraitsCountIndex.ValueInfo>
                {
                    new()
                    {
                        Value = traitValue,
                        Amount = 1
                    }
                }
            };

            await SaveEntityAsync(traitCountIndex);
        }
        else
        {
            var valueInfos = traitCountIndex.Values;
            bool valueExist = false;
    
            for (int i = 0; i < valueInfos.Count; i++)
            {
                if (valueInfos[i].Value == traitValue)
                {
                    valueInfos[i].Amount++;
                    valueExist = true;
                    break;
                }
            }
    
            if (!valueExist)
            {
                valueInfos.Add(new TraitsCountIndex.ValueInfo
                {
                    Value = traitValue,
                    Amount = 1
                });
            }
            
            traitCountIndex.Values = valueInfos;
            traitCountIndex.Amount++;
            traitCountIndex.UpdateTime = now;

            await SaveEntityAsync(traitCountIndex);
        }
        // Logger.LogDebug("[Issued] UpdateTraitCountAsync index:{holderCountBeforeUpdate}", 
        //     JsonConvert.SerializeObject(traitCountIndex));
    }
    
    private async Task UpdateGenerationCountAsync(int generation, string chainId, LogEventContext context)
    {
        var generationCountIndexId = IdGenerateHelper.GetId(chainId, generation);
        var generationCountIndex = await GetEntityAsync<GenerationCountIndex>(generationCountIndexId);
        var now = DateTimeHelper.GetCurrentTimestamp();
        if (generationCountIndex == null)
        {
            generationCountIndex = new GenerationCountIndex() {
                Id = generationCountIndexId,
                Generation = generation,
                CreateTime = now,
                UpdateTime = now,
                Count = 1
            };
            
            await SaveEntityAsync(generationCountIndex);
        }
        else
        {
            generationCountIndex.Count ++;
            generationCountIndex.UpdateTime = now;
            
            await SaveEntityAsync(generationCountIndex);
        }
    }
}