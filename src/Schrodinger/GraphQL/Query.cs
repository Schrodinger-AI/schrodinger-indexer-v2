using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;
using AeFinder.Sdk.Logging;
using Schrodinger.Entities;
using GraphQL;
using Newtonsoft.Json;
using Schrodinger.GraphQL.Dto;
using Schrodinger.Utils;
using Volo.Abp.ObjectMapping;

namespace Schrodinger.GraphQL;

public class Query
{
    [Name("getSchrodingerList")]  
    public static async Task<SchrodingerListDto> GetSchrodingerListAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderRepository,
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> adoptRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAeFinderLogger logger,
        GetSchrodingerListInput input)
    {
        try
        {
            var holderQueryable = await holderRepository.GetQueryableAsync();

            holderQueryable = holderQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            holderQueryable = holderQueryable.Where(a => a.Amount > 0);
            holderQueryable = holderQueryable.Where(a => !a.SchrodingerInfo.TokenName.StartsWith("SSGGRRCATTT"));

            if (input.FilterSgr)
            {
                holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Gen > 0);
                holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.TokenName != "SGR");
            }

            if (!string.IsNullOrEmpty(input.Address))
            {
                holderQueryable = holderQueryable.Where(a => a.Address == input.Address);
            }

            if (!string.IsNullOrEmpty(input.Keyword))
            {
                holderQueryable = holderQueryable.Where(a =>
                    a.SchrodingerInfo.Symbol == input.Keyword || a.SchrodingerInfo.TokenName == input.Keyword);
            }

            if (!input.Traits.IsNullOrEmpty())
            {
                var traitValueList = new List<string>();
                foreach (var traitValue in input.Traits)
                {
                    if (traitValue.Values.IsNullOrEmpty())
                    {
                        continue;
                    }
                    
                    traitValueList.AddRange(traitValue.Values.Select(value =>
                    {
                        var traitType = traitValue.TraitType.Replace(" ", "");
                        return traitType + value.Replace(" ", "") + ",";
                    }));
                }

                if (!traitValueList.IsNullOrEmpty())
                {
                    holderQueryable = holderQueryable.Where(
                        traitValueList.Select(traitValue =>
                                (Expression<Func<SchrodingerHolderIndex, bool>>)(o => o.TraitValues.Contains(traitValue)))
                            .Aggregate((prev, next) => prev.Or(next)));
                }
            }

            if (!input.Generations.IsNullOrEmpty())
            {
                holderQueryable = holderQueryable.Where(
                    input.Generations.Select(gen =>
                            (Expression<Func<SchrodingerHolderIndex, bool>>)(o => o.SchrodingerInfo.Gen == gen))
                        .Aggregate((prev, next) => prev.Or(next)));
            }

            var count = holderQueryable.Count();
            var data = holderQueryable.OrderByDescending(o => o.Metadata.Block.BlockTime).Skip(input.SkipCount)
                .Take(input.MaxResultCount).ToList();

            //query adopt
            var symbolList = data.Select(x => x.SchrodingerInfo.Symbol).Where(x => !string.IsNullOrEmpty(x)).Distinct()
                .ToList();

            var adoptQueryable = await adoptRepository.GetQueryableAsync();
            adoptQueryable = adoptQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            adoptQueryable = adoptQueryable.Where(
                symbolList.Select(symbol => (Expression<Func<SchrodingerAdoptIndex, bool>>)(o => o.Symbol == symbol))
                    .Aggregate((prev, next) => prev.Or(next)));

            var adoptData = adoptQueryable.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

            var adopterDict = adoptData
                .GroupBy(x => x.Symbol)
                .ToDictionary(g => g.Key, g => g.First().Adopter);
            var adoptTimeDict = adoptData
                .GroupBy(x => x.Symbol)
                .ToDictionary(g => g.Key, g => g.First().AdoptTime);

            var response = new SchrodingerListDto
            {
                TotalCount = count,
                Data = objectMapper.Map<List<SchrodingerHolderIndex>, List<SchrodingerDto>>(data)
            };

            foreach (var schrodingerDto in response.Data)
            {
                schrodingerDto.AdoptTime = 0;
                schrodingerDto.Adopter = string.Empty;
                if (adoptTimeDict.TryGetValue(schrodingerDto.Symbol, out var adoptTime))
                {
                    schrodingerDto.AdoptTime = DateTimeHelper.ToUnixTimeMilliseconds(adoptTime);
                }

                if (adopterDict.TryGetValue(schrodingerDto.Symbol, out var adopter))
                {
                    schrodingerDto.Adopter = adopter ?? string.Empty;
                }
            }

            return response;
        }
        catch (Exception e)
        {
            logger.LogDebug("getSchrodingerList failed:{err}", e.Message);
            return new SchrodingerListDto();
        }
    }
    
    
    [Name("getSchrodingerDetail")]   
    public static async Task<SchrodingerDetailDto> GetSchrodingerDetailAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderRepository,
        [FromServices] IReadOnlyRepository<SchrodingerTraitValueIndex> traitValueRepository,
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolRepository,
        [FromServices] IObjectMapper objectMapper, GetSchrodingerDetailInput input)
    {
        var chainId = input.ChainId;
        var symbol = input.Symbol;
        var tick = TokenSymbolHelper.GetTickBySymbol(symbol);
        var holderId = IdGenerateHelper.GetId(chainId, symbol, input.Address);
        var holderQueryable = await holderRepository.GetQueryableAsync();
        holderQueryable = holderQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
        holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Symbol == input.Symbol);
        holderQueryable = holderQueryable.Where(a => a.Address == input.Address);
        var holderIndex = holderQueryable.ToList().FirstOrDefault();
        
        List<TraitInfo> traitList;
        SchrodingerDetailDto schrodingerDetailDto;
        if (holderIndex == null || holderIndex.Amount == 0)
        {
            var symbolQueryable = await  symbolRepository.GetQueryableAsync();
            symbolQueryable = symbolQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Symbol == input.Symbol);

            var result = symbolQueryable.ToList().FirstOrDefault();
            if (result == null)
            {
                return new SchrodingerDetailDto();
            }
            schrodingerDetailDto = objectMapper.Map<SchrodingerSymbolIndex, SchrodingerDetailDto>(result);
            traitList = result.Traits;
        }
        else
        {
            schrodingerDetailDto = objectMapper.Map<SchrodingerHolderIndex, SchrodingerDetailDto>(holderIndex);
            traitList = holderIndex.Traits;
        }
        
        var traitTypeList = traitList.Select(x => x.TraitType).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

        if (traitTypeList.IsNullOrEmpty())
        {
            return schrodingerDetailDto;
        }
        
        var traitQueryable = await traitValueRepository.GetQueryableAsync();
        traitQueryable = traitQueryable.Where(a => a.Tick == tick);
        traitQueryable = traitQueryable.Where(
            traitTypeList.Select(traitType => (Expression<Func<SchrodingerTraitValueIndex, bool>>)(o => o.TraitType == traitType))
                .Aggregate((prev, next) => prev.Or(next)));
        
        var traitTypeValueList = GetAllIndex(traitQueryable);

        var traitTypeValueDic = traitTypeValueList.GroupBy(x => x.TraitType)
            .ToDictionary(x => x.Key, x => 
                x.GroupBy(x => x.Value)
                    .ToDictionary(y => y.Key, y =>
                        y.Select(i => i.SchrodingerCount).Sum()));
        var traitListWithPercent = new List<TraitDto>();
        foreach (var trait in traitList)
        {
            var traitValue = trait.Value;
            var traitType = trait.TraitType;
            decimal percent = 1;

            if (traitTypeValueDic.TryGetValue(traitType, out var traitValueDic)
                && traitValueDic.TryGetValue(traitValue, out var numerator))
            {
                var denominator = traitValueDic.Values.Sum();
                if (denominator > 0 && numerator > 0)
                {
                    percent = (Convert.ToDecimal(numerator) / Convert.ToDecimal(denominator)) * 100;
                }
            }

            traitListWithPercent.Add(new TraitDto
            {
                TraitType = traitType,
                Value = traitValue,
                Percent = percent,
                IsRare = IsRare(traitType, traitValue)
            });
        }
        
        schrodingerDetailDto.Traits = traitListWithPercent;
        return schrodingerDetailDto;
    }
    
    [Name("getTraits")]
    public static async Task<SchrodingerTraitsDto> GetTraitsAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderRepository,
        GetTraitsInput input)
    {
        var queryable = await holderRepository.GetQueryableAsync();

        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        queryable = queryable.Where(a => a.Address == input.Address);
        queryable = queryable.Where(a => a.Amount > 0);

        var list = GetAllIndex(queryable);
        var traitsFilter = list
            .SelectMany(s => s.Traits)
            .GroupBy(t => t.TraitType)
            .Where(g => string.IsNullOrEmpty(input.TraitType) || input.TraitType == g.Key)
            .OrderBy(g => g.Key)
            .Select(g => new SchrodingerTraitsFilterDto
            {
                TraitType = g.Key,
                Amount = list.Count(s => s.Traits.Any(t => t.TraitType == g.Key)),
                Values = g.GroupBy(tv => tv.Value)
                    .OrderBy(tvGroup => tvGroup.Key)
                    .Select(gg => new TraitValueDto
                    {
                        Value = gg.Key,
                        Amount = gg.Count()
                    })
                    .ToList()
            })
            .ToList();

        var generationFilter = list
            .GroupBy(s => s.SchrodingerInfo.Gen)
            .Select(g => new GenerationDto
            {
                // GenerationName = GenerationOrdinalHelper.ConvertToOrdinal(g.Key),
                GenerationName = g.Key,
                GenerationAmount = g.Count()
            }).OrderBy(s => s.GenerationName).ToList();
        
        if (input.Address.IsNullOrEmpty())
        {
            generationFilter = generationFilter.Where(x => x.GenerationName > 0).ToList();
        }
        
        return new SchrodingerTraitsDto
        {
            TraitsFilter = traitsFilter,
            GenerationFilter = generationFilter
        };
    }
    
    
    [Name("getAllTraits")]
    public static async Task<SchrodingerTraitsDto> GetAllTraitsAsync(
        [FromServices] IReadOnlyRepository<TraitsCountIndex> traitValueRepository,
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> schrodingerSymbolRepository,
        [FromServices] IObjectMapper objectMapper,
        GetAllTraitsInput input)
    {
        var traitsCountQueryable = await traitValueRepository.GetQueryableAsync();
        traitsCountQueryable = traitsCountQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
        traitsCountQueryable = traitsCountQueryable.Where(a => a.Amount > 0);
        
        if (!input.TraitType.IsNullOrEmpty())
        {
            traitsCountQueryable = traitsCountQueryable.Where(a => a.TraitType == input.TraitType);
        }

        var traitsCountList = traitsCountQueryable.ToList();
        
        List<GenerationDto> generationFilter = new List<GenerationDto>();
        
        var traitsFilter = traitsCountList.Select(traits => objectMapper.Map<TraitsCountIndex, SchrodingerTraitsFilterDto>(traits)).ToList();
        
        for (int i = 0; i < 9; i++)
        {
            int generation = GenerationEnum.Generations[i];
            
            var symbolQueryable = await schrodingerSymbolRepository.GetQueryableAsync();
            symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Gen == generation);
            symbolQueryable = symbolQueryable.Where(a => a.HolderCount > 0);
            symbolQueryable = symbolQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Symbol.Contains("SGR-"));
            
            var count = symbolQueryable.Count();
        
            var generationDto = new GenerationDto
            {
                GenerationName = generation,
                GenerationAmount = count
            };
            
            generationFilter.Add(generationDto);
        }

        return new SchrodingerTraitsDto
        {
            TraitsFilter = traitsFilter,
            GenerationFilter = generationFilter
        };
    }
    
    
    [Name("getAdoptInfo")]
    public static async Task<AdoptInfoDto?> GetAdoptInfoAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IReadOnlyRepository<SchrodingerTraitValueIndex> traitValueRepository,
        GetAdoptInfoInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.AdoptId))
        {
            return null;
        }
            
        var queryable = await repository.GetQueryableAsync();

        queryable = queryable.Where(a => a.AdoptId == input.AdoptId);

        var result = queryable.ToList();
        if (result.Count == 0)
        {
            return null;
        }

        var adopt = result.FirstOrDefault();

        var tick = TokenSymbolHelper.GetTickBySymbol(adopt.Symbol);
        var traitTypes = adopt.Attributes.Select(a => a.TraitType).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        
        var traitQueryable = await traitValueRepository.GetQueryableAsync();
        traitQueryable = traitQueryable.Where(a => a.Tick == tick);
        traitQueryable = traitQueryable.Where(
            traitTypes.Select(traitType => (Expression<Func<SchrodingerTraitValueIndex, bool>>)(o => o.TraitType == traitType))
                .Aggregate((prev, next) => prev.Or(next)));
        var traitIndexList = GetAllIndex(traitQueryable);
        
        var traitTypeDic = traitIndexList
            .GroupBy(t => t.TraitType)
            .ToDictionary(group => group.Key, group => group.ToList());

        var resp = objectMapper.Map<SchrodingerAdoptIndex, AdoptInfoDto>(adopt);
        resp.Attributes = new List<Trait>();
        foreach (var attr in adopt.Attributes)
        {
            decimal percent = 0;
            if (traitTypeDic.TryGetValue(attr.TraitType, out var traitValues))
            {
                var schrodingerCount =
                    traitValues.Where(t => t.Value == attr.Value)
                        .Select(x => x.SchrodingerCount).Sum();
                decimal totalCount = traitValues.Select(t => t.SchrodingerCount).Sum();
                percent = totalCount == 0 ? 0 : schrodingerCount / totalCount;
            }

            resp.Attributes.Add(new Trait()
            {
                TraitType = attr.TraitType,
                Value = attr.Value,
                Percent = percent * 100,
                IsRare = IsRare(attr.TraitType, attr.Value)
            });
        }

        return resp;
    }
    
    
    [Name("getStrayCats")]
    public static async Task<StrayCatListDto> GetStrayCatsAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> adoptRepository,
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolRepository,
        [FromServices] IReadOnlyRepository<SchrodingerCancelIndex> cancelRepository,
        [FromServices] IObjectMapper objectMapper,
        StrayCatInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.Adopter))
        {
            return new StrayCatListDto();
        }
        
        var cancelQueryable = await cancelRepository.GetQueryableAsync();
        cancelQueryable = cancelQueryable.Where(a => a.From == input.Adopter);
        cancelQueryable = cancelQueryable.Where(a => a.Metadata.ChainId == input.ChainId);

        var cancelledAdoptionList = cancelQueryable.ToList();
        
        var cancelledAdoptIdList = cancelledAdoptionList.Select(c => c.AdoptId).ToList();

        var adoptQueryable = await adoptRepository.GetQueryableAsync();
        adoptQueryable = adoptQueryable.Where(a => a.Adopter == input.Adopter);
        adoptQueryable = adoptQueryable.Where(a => !a.IsConfirmed);

        // if (!cancelledAdoptIdList.IsNullOrEmpty())
        // {
        //     adoptQueryable = adoptQueryable.Where(
        //         cancelledAdoptIdList.Select(adoptId => (Expression<Func<SchrodingerAdoptIndex, bool>>)(o => o.AdoptId != adoptId))
        //             .Aggregate((prev, next) => prev.And(next)));
        // }
        
        if (input.AdoptTime != null)
        {
            adoptQueryable = adoptQueryable.Where(a =>
                a.AdoptTime < DateTime.UnixEpoch.AddMilliseconds((double)input.AdoptTime));
        }
        
        var allAdoption = GetAllIndex(adoptQueryable);
        var strayCats = allAdoption.Where(a => !cancelledAdoptIdList.Contains(a.AdoptId)).ToList();
        
        // var count = adoptQueryable.Count();
        // var data = adoptQueryable.OrderByDescending(o => o.Metadata.Block.BlockTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        var count = strayCats.Count;
        var data = strayCats.OrderByDescending(o => o.Metadata.Block.BlockTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        
        var parentSymbolList = data.Select(i => i.ParentInfo?.Symbol ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        
        var symbolQueryable = await symbolRepository.GetQueryableAsync();
        symbolQueryable = symbolQueryable.Where(
            parentSymbolList.Select(symbol => (Expression<Func<SchrodingerSymbolIndex, bool>>)(o => o.Symbol == symbol))
                .Aggregate((prev, next) => prev.Or(next)));
        
        
        symbolQueryable = symbolQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
        var symbolResult = symbolQueryable.ToList();
        
        var symbolDic = symbolResult
            .GroupBy(x => x.Symbol)
            .ToDictionary(s => s.Key, s => s.First().Traits);
        var list = new List<StrayCatDto>();
        foreach (var schrodingerAdoptIndex in data)
        {
            var strayCatDto = objectMapper.Map<SchrodingerAdoptIndex, StrayCatDto>(schrodingerAdoptIndex);
            strayCatDto.ParentTraits = new List<StrayCatTraitsDto>();
            if (symbolDic.TryGetValue(schrodingerAdoptIndex.ParentInfo?.Symbol ?? string.Empty, out var parentTraits))
            {
                strayCatDto.ParentTraits = objectMapper.Map<List<TraitInfo>, List<StrayCatTraitsDto>>(parentTraits);
            }
            
            strayCatDto.DirectAdoption = (schrodingerAdoptIndex.Gen - schrodingerAdoptIndex.ParentGen) > 1;
            strayCatDto.NextSymbol = schrodingerAdoptIndex.Symbol;
            strayCatDto.NextTokenName = schrodingerAdoptIndex.TokenName;
            strayCatDto.NextAmount = schrodingerAdoptIndex.OutputAmount;
            list.Add(strayCatDto);
        }
        return new StrayCatListDto
        {
            TotalCount = count,
            Data = list
        };
    }
    
    
    private static List<T> GetAllIndex<T>(IQueryable<T> queryable) 
        where T : AeFinderEntity, new()
    {
        var res = new List<T>();
        List<T> list;
        var skipCount = 0;
        
        do
        {
            queryable = queryable.Skip(skipCount).Take(10000);
            var count = queryable.Count();
            
            list = queryable.ToList();
            // var count = list.Count;
            res.AddRange(list);
            if (list.IsNullOrEmpty() || count < 10000)
            {
                break;
            }
            skipCount += count;
        } while (!list.IsNullOrEmpty());

        return res;
    }
    
    
    [Name("getTrait")]
    public static async Task<List<TraitDto>> GetTraitInfoAsync(
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IReadOnlyRepository<SchrodingerTraitValueIndex> traitValueRepository,
        GetTraitInput input)
    {
        var traitQueryable = await traitValueRepository.GetQueryableAsync();
        traitQueryable = traitQueryable.Where(a => a.TraitType == input.TraitType);
        traitQueryable = traitQueryable.Where(a => a.Tick == input.Tick);

        var res = traitQueryable.ToList();

        return objectMapper.Map<List<SchrodingerTraitValueIndex>, List<TraitDto>>(res);
    }
    
    
    [Name("getSchrodingerHolderDailyChangeList")]
    public static async Task<SchrodingerHolderDailyChangeListDto> GetSchrodingerHolderDailyChangeAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderDailyChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSchrodingerHolderDailyChangeListInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        
        if (!input.Date.IsNullOrEmpty())
        {
            queryable = queryable.Where(a => a.Date == input.Date);
        }
        
        if (!input.Address.IsNullOrEmpty())
        {
            queryable = queryable.Where(a => a.Address == input.Address);
        }
        
        if (!input.Symbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(a => a.Symbol == input.Symbol);
        }
        
        if (!input.ExcludeDate.IsNullOrEmpty())
        {
            queryable = queryable.Where(
                input.ExcludeDate.Select(date => (Expression<Func<SchrodingerHolderDailyChangeIndex, bool>>)(o => o.Date != date))
                    .Aggregate((prev, next) => prev.And(next)));
        }
        
        var resp = new SchrodingerHolderDailyChangeListDto
        {
            TotalCount = queryable.Count(),
            Data = objectMapper.Map<List<SchrodingerHolderDailyChangeIndex>, List<SchrodingerHolderDailyChangeDto>>(
                queryable.OrderBy(a => a.Metadata.Block.BlockTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList())
        };
        
        return resp;
    }
    
    
    [Name("getAdoptInfoList")]
    public static async Task<AdoptInfoListDto> GetAdoptInfoListAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetAdoptInfoListInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        if (input.FromBlockHeight > 0)
        {
            queryable = queryable.Where(a => a.Metadata.Block.BlockHeight >= input.FromBlockHeight);
        }
        
        if (input.ToBlockHeight > 0)
        { 
            queryable = queryable.Where(a => a.Metadata.Block.BlockHeight <= input.ToBlockHeight);
        }
        
        var result = GetAllIndex(queryable);
        return new AdoptInfoListDto
        {
            TransactionIds = result.Select(t => t.TransactionId).ToList(),
            AdoptionIds = result.Select(t => t.AdoptId).ToList(),
            SymbolIds = result.Select(t => t.Symbol).ToList()
        };
    }
    
    
    [Name("getAllSchrodingerList")]
    public static async Task<AllSchrodingerListDto> GetAllSchrodingerListAsync(
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolRepository,
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> adoptRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAeFinderLogger logger,
        GetAllSchrodingerListInput input)
    {
        try
        {
            var symbolQueryable = await symbolRepository.GetQueryableAsync();
            symbolQueryable = symbolQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Symbol.Contains("SGR-"));

            var minAmount = input.MinAmount.IsNullOrEmpty() ? 0 : long.Parse(input.MinAmount);
            if (minAmount > 0)
            {
                symbolQueryable = symbolQueryable.Where(a => a.Amount >= minAmount);
            }
            else
            {
                symbolQueryable = symbolQueryable.Where(a => a.Amount > 0);
            }

            if (input.FilterSgr)
            {
                symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Gen > 0);
                symbolQueryable = symbolQueryable.Where(a => !a.SchrodingerInfo.TokenName.StartsWith("SSGGRRCATTT"));
                symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.TokenName != "SGR");
            }

            if (!string.IsNullOrEmpty(input.Keyword))
            {
                symbolQueryable = symbolQueryable.Where(a =>
                    a.SchrodingerInfo.Symbol == input.Keyword || a.SchrodingerInfo.TokenName == input.Keyword);
            }

            if (!input.Traits.IsNullOrEmpty())
            {
                var traitValueList = new List<string>();
                foreach (var traitValue in input.Traits)
                {
                    if (traitValue.Values.IsNullOrEmpty())
                    {
                        continue;
                    }
                    
                    traitValueList.AddRange(traitValue.Values.Select(value =>
                    {
                        var traitType = traitValue.TraitType.Replace(" ", "");
                        return traitType + value.Replace(" ", "") + ",";
                    }));
                }

                if (!traitValueList.IsNullOrEmpty())
                {
                    symbolQueryable = symbolQueryable.Where(
                        traitValueList.Select(traitValue =>
                                (Expression<Func<SchrodingerSymbolIndex, bool>>)(o => o.TraitValues.Contains(traitValue)))
                            .Aggregate((prev, next) => prev.Or(next)));
                }
            }

            if (!input.Generations.IsNullOrEmpty())
            {
                symbolQueryable = symbolQueryable.Where(
                    input.Generations.Select(gen =>
                            (Expression<Func<SchrodingerSymbolIndex, bool>>)(o => o.SchrodingerInfo.Gen == gen))
                        .Aggregate((prev, next) => prev.Or(next)));
            }

            if (!input.Raritys.IsNullOrEmpty())
            {
                symbolQueryable = symbolQueryable.Where(
                    input.Raritys.Select(rarity =>
                            (Expression<Func<SchrodingerSymbolIndex, bool>>)(o => o.Rarity == rarity))
                        .Aggregate((prev, next) => prev.Or(next)));
            }

            var count = symbolQueryable.Count();
            var data = symbolQueryable.OrderByDescending(o => o.Metadata.Block.BlockTime).Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();
            
            //query adopt
            var symbolList = data.Select(x => x.SchrodingerInfo.Symbol).Where(x => !string.IsNullOrEmpty(x)).Distinct()
                .ToList();

            var adoptQueryable = await adoptRepository.GetQueryableAsync();
            adoptQueryable = adoptQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
            adoptQueryable = adoptQueryable.Where(
                symbolList.Select(symbol => (Expression<Func<SchrodingerAdoptIndex, bool>>)(o => o.Symbol == symbol))
                    .Aggregate((prev, next) => prev.Or(next)));

            var adoptResult = GetAllIndex(adoptQueryable);

            var adopterDict = adoptResult
                .GroupBy(x => x.Symbol)
                .ToDictionary(g => g.Key, g => g.First().Adopter);
            var adoptTimeDict = adoptResult
                .GroupBy(x => x.Symbol)
                .ToDictionary(g => g.Key, g => g.First().AdoptTime);

            var response = new AllSchrodingerListDto
            {
                TotalCount = count,
                Data = objectMapper.Map<List<SchrodingerSymbolIndex>, List<AllSchrodingerDto>>(data)
            };

            foreach (var schrodingerDto in response.Data)
            {
                schrodingerDto.AdoptTime = 0;
                schrodingerDto.Adopter = string.Empty;
                if (adoptTimeDict.TryGetValue(schrodingerDto.Symbol, out var adoptTime))
                {
                    schrodingerDto.AdoptTime = DateTimeHelper.ToUnixTimeMilliseconds(adoptTime);
                }

                if (adopterDict.TryGetValue(schrodingerDto.Symbol, out var adopter))
                {
                    schrodingerDto.Adopter = adopter ?? string.Empty;
                }
            }

            return response;
        }
        catch (Exception e)
        {
            logger.LogError("getAllSchrodingerList failed:{err}", e.Message);
            return new AllSchrodingerListDto();
        }
    }
    
    
    [Name("getSchrodingerSoldRecord")]
    public static async Task<NFTActivityPageResultDto> GetSchrodingerSoldRecordAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetSchrodingerSoldRecordInput input, [FromServices] IObjectMapper objectMapper)
    {
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        if (input.Types?.Count > 0)
        {
            queryable = queryable.Where(
                input.Types.Select(type => (Expression<Func<NFTActivityIndex, bool>>)(o => (int)o.Type == type))
                    .Aggregate((prev, next) => prev.Or(next)));
        }
        
        if (input.TimestampMin is > 0)
        {
            queryable = queryable.Where(a => a.Timestamp >= DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMin));
        }
        
        if (!input.FilterSymbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(a => a.NftInfoId.Contains(input.FilterSymbol+"-"));
        }
        
        if (!input.Address.IsNullOrEmpty())
        {
            queryable = queryable.Where(a => a.From == input.Address);
        }

        var response = new NFTActivityPageResultDto
        {
            TotalRecordCount = queryable.Count()
        };
        
        if (input.SortType.IsNullOrEmpty() || input.SortType.Equals("DESC"))
        {
            response.Data = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(
                queryable.OrderByDescending(a => a.Timestamp).Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
        }
        else
        {
            response.Data = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(
                queryable.OrderBy(a => a.Timestamp).Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
        }

        return response;
    }
    
    [Name("getSchrodingerRank")]
    public static async Task<SchrodingerRankDto> GetSchrodingerRankAsync(
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSchrodingerRankInput input)
    {
        var queryable = await symbolRepository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        queryable = queryable.Where(a => a.Symbol == input.Symbol);
        var result = queryable.ToList().FirstOrDefault();
        if (result == null)
        {
            return new SchrodingerRankDto();
        }
        
        var resp = objectMapper.Map<SchrodingerSymbolIndex, SchrodingerRankDto>(result);
        resp.TokenName = result.SchrodingerInfo.TokenName;
        resp.InscriptionImageUri = result.SchrodingerInfo.InscriptionImageUri;
        resp.Generation = result.SchrodingerInfo.Gen;
        return resp;
    }
    
    [Name("getAdoptInfoByTime")]
    public static async Task<List<AdoptInfoDto>> GetAdoptInfoByTimeAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetAdoptInfoByTimeInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        if (input.BeginTime > 0)
        {
            queryable = queryable.Where(a => a.AdoptTime >= DateTime.UnixEpoch.AddSeconds(input.BeginTime));
        }
        if (input.EndTime > 0)
        {
            queryable = queryable.Where(a => a.AdoptTime < DateTime.UnixEpoch.AddSeconds(input.EndTime));
        }
        
        return objectMapper.Map<List<SchrodingerAdoptIndex>, List<AdoptInfoDto>>(queryable.ToList());
    }
    
    private static bool IsRare(string traitType, string traitValue)
    {
        var rareType = new List<string>
        {
            "Background",
            "Clothes",
            "Breed",
            "Hat",
            "Eyes",
            "Pet",
            "Mouth",
            "Face",
            "Necklace",
            "Paw",
            "Trousers",
            "Belt",
            "Shoes",
            "Mustache",
            "Wings",
            "Tail",
            "Ride",
            "Weapon",
            "Accessory"
        };
        
        var traitsProbabilityMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(TraitProbabilityConstants.TraitValueProbability);
        
        var traitsProbabilityList = traitsProbabilityMap.ToDictionary(x => x.Key,
            x => x.Value.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).Take(10).ToList());

        if (!rareType.Contains(traitType))
        {
            return false;
        }

        var rareValueInType = traitsProbabilityList[traitType];
        return rareValueInType.Contains(traitValue);
    }
    
    [Name("getHoldingRank")]
    public static async Task<List<RankItem>> GetHoldingRankAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> repository,
        GetHoldingRankInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        
        queryable = queryable.Where(a => a.Amount > 0);
        queryable = queryable.Where(a => a.SchrodingerInfo.Gen > 0);
        queryable = queryable.Where(a => a.SchrodingerInfo.Symbol.Contains("SGR-"));
        
        var holderList = GetAllIndex(queryable);
        var holderRankList = holderList
        .GroupBy(x => x.Address)
        .Select(x => new RankItem
        {
            Address = x.Key,
            Amount = x.Sum(y => y.Amount) / (decimal)Math.Pow(10, 8),
            UpdateTime = x.Max(y => y.Metadata.Block.BlockTime)
        }).ToList();

        holderRankList = holderRankList.OrderByDescending(a => a.Amount).ThenBy(a => a.UpdateTime).ToList();
        return holderRankList.Take(input.RankNumber).ToList();
    }
    
    [Name("getRarityRank")]
    public static async Task<List<RarityRankItem>> GetRarityRankAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderIndexRepository,
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolIndexRepository,
        [FromServices] IAeFinderLogger logger,
        GetHoldingRankInput input)
    {
        string pattern = "SGR-";
        var holderQueryable = await holderIndexRepository.GetQueryableAsync();
        holderQueryable = holderQueryable.Where(a => a.Amount > 0);
        holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Gen == 9);
        holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Symbol.StartsWith(pattern));
        
        var holderList = GetAllIndex(holderQueryable);
  
        var symbolQueryable = await symbolIndexRepository.GetQueryableAsync();
        symbolQueryable = symbolQueryable.Where(
            LevelConstant.RarityList.Select(rarity => (Expression<Func<SchrodingerSymbolIndex, bool>>)(o => o.Rarity == rarity))
                .Aggregate((prev, next) => prev.Or(next)));
        
        var symbolList = GetAllIndex(symbolQueryable);
        
        var rarityDict = symbolList.GroupBy(x => x.Rarity).ToDictionary(g => g.Key, g => g.Select(x => x.Symbol).ToList());
        
        var rarityRankItemDict = new Dictionary<string, RarityRankItem>();
        foreach (var holderInfo in holderList)
        {
            var symbol = holderInfo.SchrodingerInfo.Symbol;
            var rarity = GetRarity(rarityDict, symbol);
            if (rarity.IsNullOrEmpty())
            {
                continue;
            }
            
            var address = holderInfo.Address;
            if (rarityRankItemDict.TryGetValue(address, out var rarityRankItem))
            {
                rarityRankItemDict[address] = SetRarityRankItem(rarity, holderInfo.Amount, holderInfo.Metadata.Block.BlockTime, rarityRankItem);
            }
            else
            {
                rarityRankItem = new RarityRankItem
                {
                    Address = address
                };
                rarityRankItemDict[address] = SetRarityRankItem(rarity, holderInfo.Amount, holderInfo.Metadata.Block.BlockTime, rarityRankItem);
            }
        }

        var rarityRankItemList = rarityRankItemDict.Select(x => x.Value).ToList();
        
        var sortedList = rarityRankItemList.OrderByDescending(item => item.Diamond)
            .ThenByDescending(item => item.Emerald)
            .ThenByDescending(item => item.Platinum)
            .ThenByDescending(item => item.Gold)
            .ThenByDescending(item => item.Silver)
            .ThenByDescending(item => item.Bronze)
            .ThenBy(item => item.UpdateTime)
            .ToList();
        
        return sortedList.Take(input.RankNumber).ToList();
    }
    
    
    private static string GetRarity(Dictionary<string, List<string>> rarityDict, string symbol)
    {
        foreach (var item in rarityDict)
        {
            if (item.Value.Contains(symbol))
            {
                return item.Key;
            }
        }

        return "";
    }

    private static RarityRankItem SetRarityRankItem(string rarity, long amount, DateTime updateTime, RarityRankItem rarityRankItem)
    {
        if (updateTime > rarityRankItem.UpdateTime)
        {
            rarityRankItem.UpdateTime = updateTime;
        }
        
        var decimals = (decimal)Math.Pow(10, 8);
        switch (rarity)
        {
            case "Diamond":
                rarityRankItem.Diamond += amount/decimals;
                break;
            case "Gold":
                rarityRankItem.Gold += amount/decimals;
                break;
            case "Silver":
                rarityRankItem.Silver += amount/decimals;
                break;
            case "Bronze":
                rarityRankItem.Bronze += amount/decimals;
                break;
            case "Emerald":
                rarityRankItem.Emerald += amount/decimals;
                break;
            case "Platinum":
                rarityRankItem.Platinum += amount/decimals;
                break;
        }
        return rarityRankItem;
    }
    
    
    [Name("getHomeData")]
    public static async Task<HomeDataDto> GetHomeDataAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderIndexRepository,
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolIndexRepository,
        [FromServices] IReadOnlyRepository<NFTActivityIndex> nftActivityIndexRepository,
        GetHomeDataInput input)
    {
        string pattern = "SGR-";
        var symbolQueryable = await symbolIndexRepository.GetQueryableAsync();
        symbolQueryable = symbolQueryable.Where(a => a.Amount >= 100000000);
        symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Gen > 0);
        symbolQueryable = symbolQueryable.Where(a => a.SchrodingerInfo.Symbol.StartsWith(pattern));
        symbolQueryable = symbolQueryable.Where(a => a.Metadata.ChainId == input.ChainId);

        var schrodingerSymbolCount = symbolQueryable.Count();
        
        var holderQueryable = await holderIndexRepository.GetQueryableAsync();
        holderQueryable = holderQueryable.Where(a => a.Amount > 0);
        holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Gen > 0);
        holderQueryable = holderQueryable.Where(a => a.SchrodingerInfo.Symbol.StartsWith(pattern));
        holderQueryable = holderQueryable.Where(a => a.Metadata.ChainId == input.ChainId);
        
        var holderList = GetAllIndex(holderQueryable);
        var uniqueHolderCnt = holderList.GroupBy(i => i.Address).Select(i => i.Key).Count();
        
        var activityQueryable = await nftActivityIndexRepository.GetQueryableAsync();
        activityQueryable = activityQueryable.Where(a => a.Type == NFTActivityType.Sale);
        
        activityQueryable = activityQueryable.Where(a => a.NftInfoId.StartsWith(input.ChainId+"-"+pattern));

        var totalTradeList = GetAllIndex(activityQueryable);
        
        var totalTradeVolume = totalTradeList.Sum(i => i.Price * i.Amount);
        var tradeVolumeData = Math.Round(totalTradeVolume, 1);

        return new HomeDataDto
        {
            SymbolCount = schrodingerSymbolCount,
            HoldingCount = uniqueHolderCnt,
            TradeVolume = tradeVolumeData
        };
    }
    
    
    [Name("getSchrodingerSoldList")]
    public static async Task<List<NFTActivityDto>> GetSchrodingerSoldListAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetSchrodingerSoldListInput input, [FromServices] IObjectMapper objectMapper)
    {
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Type == NFTActivityType.Sale);

        if (input.TimestampMin  > 0)
        {
            queryable = queryable.Where(a => a.Timestamp >= DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMin));
        }
        
        if (input.TimestampMax  > 0)
        {
            queryable = queryable.Where(a => a.Timestamp < DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMax));
        }
        
        var pattern = input.ChainId + "-SGR-";
        queryable = queryable.Where(a => a.NftInfoId.StartsWith(pattern));
        
        var baseTokenList = new List<string>
        {
            "tDVW-SGR-1",
            "tDVV-SGR-1"
        };
        queryable = queryable.Where(
            baseTokenList.Select(tokenId => (Expression<Func<NFTActivityIndex, bool>>)(o => o.NftInfoId != tokenId))
                .Aggregate((prev, next) => prev.And(next)));

        
        var nftSoldList =  GetAllIndex(queryable);
        var sortedList = nftSoldList.OrderBy(item => item.Timestamp).ToList();
        return objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(sortedList);
    }
    
    [Name("getHoldingPointBySymbol")]
    public static async Task<HoldingPointBySymbolDto> GetHoldingPointBySymbol(
        [FromServices] IReadOnlyRepository<SchrodingerSymbolIndex> symbolRepository,
        [FromServices] IObjectMapper objectMapper, GetHoldingPointBySymbolInput input)
    {
        var queryable = await symbolRepository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        queryable = queryable.Where(a => a.SchrodingerInfo.Symbol == input.Symbol);
        
        var result =  queryable.ToList().FirstOrDefault();
        if (result == null)
        {
            return new HoldingPointBySymbolDto();
        }

        var level = result.Level;
        var res = new HoldingPointBySymbolDto
        {
            Level = level,
            Point = 9
        };

        if (!level.IsNullOrEmpty() && LevelConstant.LevelPointDictionary.TryGetValue(level, out var pointOfLevel))
        {
            res.Point = pointOfLevel;
        }
        
        return res;
    }
    
    [Name("getSchrodingerHoldingList")]
    public static async Task<SchrodingerListDto> GetSchrodingerHoldingListAsync(
        [FromServices] IReadOnlyRepository<SchrodingerHolderIndex> holderRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSchrodingerHoldingListInput input)
    {
        var queryable = await holderRepository.GetQueryableAsync();

        var pattern = "SGR-";
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        queryable = queryable.Where(a => a.Amount >= 100000000);
        queryable = queryable.Where(a => a.SchrodingerInfo.Symbol.StartsWith(pattern));
        queryable = queryable.Where(a => a.SchrodingerInfo.Gen > 0);
            
        if (!string.IsNullOrEmpty(input.Address))
        {
            queryable = queryable.Where(a => a.Address == input.Address);
        }

        var totalCount = queryable.Count();
        var list = queryable.OrderByDescending(o => o.Metadata.Block.BlockTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
            
        var response = new SchrodingerListDto
        {
            TotalCount = totalCount,
            Data = objectMapper.Map<List<SchrodingerHolderIndex>, List<SchrodingerDto>>(list)
        };
    
        return response;
    }
    
    [Name("getSchrodingerTradeRecord")]
    public static async Task<List<NFTActivityDto>> GetSchrodingerTradeRecordAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetSchrodingerTradeRecordInput input, [FromServices] IObjectMapper objectMapper)
    {
        var nftId = input.ChainId + "-" + input.Symbol;
        var to = input.Buyer;
        
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        queryable = queryable.Where(a => a.To == to);
        queryable = queryable.Where(a => a.NftInfoId == nftId);
        queryable = queryable.Where(a => a.Type == NFTActivityType.Sale);
        queryable = queryable.Where(a => a.Timestamp <= input.TradeTime);

        var nftSoldList =  GetAllIndex(queryable).ToList();
        var sortedList = nftSoldList.OrderBy(item => item.Timestamp).ToList();
        return objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(sortedList);
    }
    
    [Name("getBlindBoxList")]
    public static async Task<BlindBoxListDto> GetBlindBoxListAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> repository,
        [FromServices] IReadOnlyRepository<SchrodingerCancelIndex> cancelRepository,
        [FromServices] IObjectMapper objectMapper,
        BlindBoxListInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.Adopter))
        {
            return  new BlindBoxListDto();
        }
        
        // var cancelMustQuery = new List<Func<QueryContainerDescriptor<SchrodingerCancelIndex>, QueryContainer>>
        // {
        //     q => q.Term(f => f.Field(f => f.From).Value(input.Adopter)),
        // };
        // QueryContainer CancelFilter(QueryContainerDescriptor<SchrodingerCancelIndex> f) => f.Bool(b => b.Must(cancelMustQuery));
        // var cancelledAdoptionList = await GetAllIndex(CancelFilter, cancelRepository);
        // var cancelledAdoptIdList = cancelledAdoptionList.Select(c => c.AdoptId).ToList();
        
        var cancelQueryable = await cancelRepository.GetQueryableAsync();
        cancelQueryable = cancelQueryable.Where(a => a.From == input.Adopter);

        var cancelledAdoptionList = cancelQueryable.ToList();
        var cancelledAdoptIdList = cancelledAdoptionList.Select(c => c.AdoptId).ToList();
        
        var adoptQueryable = await repository.GetQueryableAsync();
        adoptQueryable = adoptQueryable.Where(a => a.Adopter == input.Adopter);
        if (input.AdoptTime != null)
        {
            adoptQueryable = adoptQueryable.Where(a => a.AdoptTime >= DateTime.UnixEpoch.AddMilliseconds((double)input.AdoptTime));
        }
        
        // if (!cancelledAdoptIdList.IsNullOrEmpty())
        // {
        //     adoptQueryable = adoptQueryable.Where(
        //         cancelledAdoptIdList.Select(adoptId => (Expression<Func<SchrodingerAdoptIndex, bool>>)(o => o.AdoptId != adoptId))
        //             .Aggregate((prev, next) => prev.And(next)));
        // }

        var result =  GetAllIndex(adoptQueryable).ToList();
        
        var list = new List<BlindBoxDto>();
        var unconfirmedList = result.Where(x => !x.IsConfirmed).ToList();
        var parentSymbolList = result.Select(i => i.ParentInfo.Symbol).Distinct().ToList();
        
        foreach (var schrodingerAdoptIndex in unconfirmedList)
        {
            if (parentSymbolList.Contains(schrodingerAdoptIndex.Symbol))
            {
                continue;
            }

            if (!cancelledAdoptIdList.IsNullOrEmpty() && cancelledAdoptIdList.Contains(schrodingerAdoptIndex.AdoptId))
            {
                continue;
            }
            
            var blindBoxDto = objectMapper.Map<SchrodingerAdoptIndex, BlindBoxDto>(schrodingerAdoptIndex);
            
            blindBoxDto.AdoptTime =  DateTimeHelper.ToUnixTimeMilliseconds(schrodingerAdoptIndex.AdoptTime);

            if (!schrodingerAdoptIndex.Attributes.IsNullOrEmpty())
            {
                var traitsList = schrodingerAdoptIndex.Attributes.Select(item => new TraitDto
                    { TraitType = item.TraitType, Value = item.Value }).ToList();
                blindBoxDto.Traits = traitsList;
            }
            
            list.Add(blindBoxDto);
        }
        
        return new BlindBoxListDto()
        {
            TotalCount = list.Count,
            Data = list
        };
    }
    
    [Name("getBlindBoxDetail")]
    public static async Task<BlindBoxDto> GetBlindBoxDetailAsync(
        [FromServices] IReadOnlyRepository<SchrodingerAdoptIndex> repository,
        [FromServices] IReadOnlyRepository<SchrodingerTraitValueIndex> traitValueRepository,
        [FromServices] IObjectMapper objectMapper,
        BlindBoxDetailInput input)
    {
        if (input == null || string.IsNullOrEmpty(input.Symbol))
        {
            return new BlindBoxDto();
        }
        
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Symbol == input.Symbol);
        var result = queryable.ToList().FirstOrDefault(); 

        if (result == null)
        {
            return new BlindBoxDto();;
        }
        
        var resp = objectMapper.Map<SchrodingerAdoptIndex, BlindBoxDto>(result);
      
        resp.AdoptTime =  DateTimeHelper.ToUnixTimeMilliseconds(result.AdoptTime);
        resp.DirectAdoption = (result.Gen - result.ParentGen) > 1;
        
        if (!result.Attributes.IsNullOrEmpty())
        {
            var traitList = result.Attributes.Select(item => new SchrodingerDto.TraitsInfo
                { TraitType = item.TraitType, Value = item.Value }).ToList();
            
            var traitTypeList = traitList.Select(x => x.TraitType).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            
            var traitQueryable = await traitValueRepository.GetQueryableAsync();
            traitQueryable = traitQueryable.Where(a => a.Tick == result.Tick);
            traitQueryable = traitQueryable.Where(
                traitTypeList.Select(traitType => (Expression<Func<SchrodingerTraitValueIndex, bool>>)(o => o.TraitType == traitType))
                    .Aggregate((prev, next) => prev.Or(next)));
            var traitTypeValueList = GetAllIndex(traitQueryable);

            var traitTypeValueDic = traitTypeValueList.GroupBy(x => x.TraitType)
                .ToDictionary(x => x.Key, x => 
                    x.GroupBy(x => x.Value)
                        .ToDictionary(y => y.Key, y =>
                            y.Select(i => i.SchrodingerCount).Sum()));
            var traitListWithPercent = new List<TraitDto>();
            foreach (var trait in traitList)
            {
                var traitValue = trait.Value;
                var traitType = trait.TraitType;
                decimal percent = 1;
        
                if (traitTypeValueDic.TryGetValue(traitType, out var traitValueDic)
                    && traitValueDic.TryGetValue(traitValue, out var numerator))
                {
                    var denominator = traitValueDic.Values.Sum();
                    if (denominator > 0 && numerator > 0)
                    {
                        percent = (Convert.ToDecimal(numerator) / Convert.ToDecimal(denominator)) * 100;
                    }
                }
        
                traitListWithPercent.Add(new TraitDto
                {
                    TraitType = traitType,
                    Value = traitValue,
                    Percent = percent,
                    IsRare = IsRare(traitType, traitValue)
                });
            }
        
            resp.Traits = traitListWithPercent;
        }

        return resp;
    }
}