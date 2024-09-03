using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.CSharp.Core;
using AutoMapper;
using Schrodinger.Entities;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public abstract class TokenProcessorBase<TEvent> : LogEventProcessorBase<TEvent>
    where TEvent : IEvent<TEvent>, new()
{
    protected readonly IMapper Mapper;
    protected TokenProcessorBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SchrodingerProfile>();
        });
        Mapper = config.CreateMapper();
    }
    

    public override string GetContractAddress(string chainId)
    {
        return chainId switch
        {
            "AELF" => "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            "tDVV" => "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            "tDVW" => "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
            _ => string.Empty
        };
    }

    protected string GetSymbolIndexId(string chainId, string symbol)
    {
        return IdGenerateHelper.GetId(chainId, symbol);
    }
    
    protected string GetTraitTypeValueIndexId(string chainId, string tick, string traitType, string traitValue)
    {
        return IdGenerateHelper.GetId(chainId, tick, traitType, traitValue);
    }

    protected string GetHolderIndexId(string chainId, string symbol, string owner)
    {
        return IdGenerateHelper.GetId(chainId, symbol, owner);
    }

    protected async Task<long> GetSymbolHolderCountAsync(string chainId, string symbol)
    {
        return (await GetSymbolAsync(chainId, symbol))?.HolderCount ?? 0;
    }

    protected async Task<SchrodingerSymbolIndex> GetSymbolAsync(string chainId, string symbol)
    {
        var symbolId = IdGenerateHelper.GetId(chainId, symbol);
        return await GetEntityAsync<SchrodingerSymbolIndex>(symbolId);
    }

    protected async Task GenerateSchrodingerCountAsync(string chainId, string tick, string traitType, string traitValue)
    {
        var traitValueIndex =
            await GetEntityAsync<SchrodingerTraitValueIndex>(GetTraitTypeValueIndexId(chainId, tick, traitType,
                traitValue));
        if (traitValueIndex == null)
        {
            await SaveEntityAsync(new SchrodingerTraitValueIndex
            {
                Id = GetTraitTypeValueIndexId(chainId, tick, traitType, traitValue),
                Tick = tick,
                TraitType = traitType,
                Value = traitValue,
                SchrodingerCount = 0
            });
        }
    }

    protected async Task UpdateSchrodingerCountAsync(SchrodingerHolderIndex holderIndex, string tick,
        long deltaCount, LogEventContext context)
    {
        foreach (var traitInfo in holderIndex.Traits)
        {
            var schrodingerTraitValueIndex = await GetEntityAsync<SchrodingerTraitValueIndex>(
                IdGenerateHelper.GetId(holderIndex.Metadata.ChainId, tick, traitInfo.TraitType, traitInfo.Value));
            schrodingerTraitValueIndex.SchrodingerCount += deltaCount;
            if (schrodingerTraitValueIndex.SchrodingerCount < 0)
            {
                schrodingerTraitValueIndex.SchrodingerCount = 0;
            }
            await SaveEntityAsync(schrodingerTraitValueIndex);
            // Logger.LogDebug("UpdateSchrodingerCountAsync after count: {count}, type: {type}, value: {value}",
            //     schrodingerTraitValueIndex.SchrodingerCount, traitInfo.TraitType, traitInfo.Value);  
        }
    }

    protected async Task<SchrodingerHolderIndex> UpdatedHolderRelatedAsync(string chainId, string symbol, string owner,
        long deltaAmount, long initAmount, string tokenEventType, LogEventContext context)
    {
        var holderId = GetHolderIndexId(chainId, symbol, owner);
        // var holderIndex = await SchrodingerHolderRepository.GetFromBlockStateSetAsync(holderId, chainId);
        var holderIndex = await GetEntityAsync<SchrodingerHolderIndex>(holderId);
        var holderExist = holderIndex != null;
        var beforeAmount = holderExist ? holderIndex.Amount : 0;
        
        if (!holderExist)
        {
            var symbolIndex = await GetSymbolAsync(chainId, symbol);
            holderIndex = Mapper.Map<SchrodingerSymbolIndex, SchrodingerHolderIndex>(symbolIndex) ?? new SchrodingerHolderIndex();
            holderIndex.Address = owner;
            holderIndex.Id = holderId;
            holderIndex.Amount = initAmount;
        }
        else
        {
            holderIndex.Amount += deltaAmount;
        }

        if (holderIndex.Amount < 0)
        {
            holderIndex.Amount = 0;
        }

        var afterAmount = holderIndex.Amount;
        await SaveEntityAsync(holderIndex);
        await UpdateSymbolHolderAsync(beforeAmount, afterAmount, tokenEventType, chainId, symbol, context, deltaAmount);
        
        return holderIndex;
    }

    protected async Task UpdateSymbolHolderAsync(long beforeAmount, long afterAmount, string tokenEventType,
        string chainId, string symbol, LogEventContext context, long deltaAmount)
    {
        var symbolId = GetSymbolIndexId(chainId, symbol);
        // var symbolIndex = await SchrodingerSymbolRepository.GetFromBlockStateSetAsync(symbolId, chainId);
        var symbolIndex = await GetEntityAsync<SchrodingerSymbolIndex>(symbolId);
        
        switch (tokenEventType)
        {
            case SchrodingerConstants.Issued:
                symbolIndex = ChangeHolderCount(beforeAmount, 1, symbolIndex);
                break;
            case SchrodingerConstants.Burned:
                symbolIndex = ChangeHolderCount(afterAmount, -1, symbolIndex);
                break;
            case SchrodingerConstants.CrossChainReceived:
                symbolIndex = ChangeHolderCount(beforeAmount, 1, symbolIndex);
                break;
            case SchrodingerConstants.TransferredFrom:
                symbolIndex = ChangeHolderCount(afterAmount, -1, symbolIndex);
                break;
            case SchrodingerConstants.TransferredTo:
                symbolIndex = ChangeHolderCount(beforeAmount, 1, symbolIndex);
                break;
        }
        
        if (symbolIndex.HolderCount < 0)
        {
            symbolIndex.HolderCount = 0;
        }

        symbolIndex.Amount += deltaAmount;
        // await SaveIndexAsync(symbolIndex, context);
        await SaveEntityAsync(symbolIndex);
    }
    
    private static SchrodingerSymbolIndex ChangeHolderCount(long amount, long deltaCount, SchrodingerSymbolIndex symbolIndex)
    {
        if (amount <= 0)
        {
            symbolIndex.HolderCount += deltaCount;
        }

        return symbolIndex;
    }
    
    public async Task SaveSchrodingerHolderDailyChangeAsync(string symbol, string address, long amount, LogEventContext context)
    {
        var holderId = IdGenerateHelper.GetId(context.ChainId, symbol, address);
        var holderIndex = await GetEntityAsync<SchrodingerHolderIndex>(holderId);
        if (holderIndex == null)
        {
            Logger.LogWarning("holderIndex is null, chainId:{chainId} symbol:{symbol}, address:{address}", context.ChainId, symbol, address);
            return;
        }

        var date =  context.Block.BlockTime.ToString("yyyyMMdd");
        var schrodingerHolderDailyChangeId = IdGenerateHelper.GetSchrodingerHolderDailyChangeId(context.ChainId,date,symbol,address );
        var schrodingerHolderDailyChangeIndex =
            await GetEntityAsync<SchrodingerHolderDailyChangeIndex>(schrodingerHolderDailyChangeId);
        if (schrodingerHolderDailyChangeIndex == null)
        {
            schrodingerHolderDailyChangeIndex = new SchrodingerHolderDailyChangeIndex
            {
                Id = schrodingerHolderDailyChangeId,
                Address = address,
                Symbol = symbol,
                Date = date,
                ChangeAmount = amount,
                Balance = holderIndex.Amount
            };
        }
        else
        {
            schrodingerHolderDailyChangeIndex.ChangeAmount += amount;
            schrodingerHolderDailyChangeIndex.Balance = holderIndex.Amount;
        }
       
        await SaveEntityAsync(schrodingerHolderDailyChangeIndex);
    }
}