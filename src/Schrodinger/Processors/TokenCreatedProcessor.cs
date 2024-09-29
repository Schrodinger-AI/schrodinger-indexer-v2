using AeFinder.Sdk.Attachments;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Processors.Provider.dto;
using Schrodinger.Utils;


namespace Schrodinger.Processors;


public class ProbabilityRankMapAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityRankMap>
{
    public override string Key => "ProbabilityRankMapNew";
}

// public class ProbabilityMapPartAAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityMapPartA>
// {
//     public override string Key => "ProbabilityMapPartA";
// }
//
// public class ProbabilityMapPartBAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityMapPartB>
// {
//     public override string Key => "ProbabilityMapPartB";
// }

public class ProbabilityMapAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityMap>
{
    public override string Key => "ProbabilityMapNew";
}


public class TokenCreatedProcessor() : TokenProcessorBase<TokenCreated>
{
    private readonly Dictionary<string, Dictionary<string, double>> _traitValueProbability;
    private readonly Dictionary<string, Dictionary<string, int>> _traitTypeProbability;
    
    private readonly IAppAttachmentValueProvider<ProbabilityRankMap> _probabilityRankMapAppAttachmentValueProvider;
    // private readonly IAppAttachmentValueProvider<ProbabilityMapPartA> _probabilityMapPartAAppAttachmentValueProvider;
    // private readonly IAppAttachmentValueProvider<ProbabilityMapPartB> _probabilityMapPartBAppAttachmentValueProvider;
    private readonly IAppAttachmentValueProvider<ProbabilityMap> _probabilityMapAppAttachmentValueProvider;
    
    public  TokenCreatedProcessor(
        IAppAttachmentValueProvider<ProbabilityRankMap> probabilityRankMapAppAttachmentValueProvider, 
        IAppAttachmentValueProvider<ProbabilityMap> probabilityMapAppAttachmentValueProvider) : this()
    {
        _probabilityRankMapAppAttachmentValueProvider = probabilityRankMapAppAttachmentValueProvider;
        // _probabilityMapPartAAppAttachmentValueProvider = probabilityMapPartAAppAttachmentValueProvider;
        // _probabilityMapPartBAppAttachmentValueProvider = probabilityMapPartBAppAttachmentValueProvider;
        _probabilityMapAppAttachmentValueProvider = probabilityMapAppAttachmentValueProvider;
        
        _traitValueProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(TraitProbabilityConstants.TraitValueProbability);
        _traitTypeProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(TraitProbabilityConstants.TraitTypeProbability);
    }
    
    public override async Task ProcessAsync(TokenCreated eventValue, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = eventValue.Symbol;
        try
        {
            var tick = TokenSymbolHelper.GetTickBySymbol(symbol);
            var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(IdGenerateHelper.GetId(chainId, tick));
            if (schrodingerIndex == null)
            {
                return;
            }
            
            var isCollection = TokenSymbolHelper.GetIsCollectionFromSymbol(symbol);
            if (isCollection)
            {
                return;
            }
            
            Logger.LogDebug("[TokenCreated] start chainId:{chainId} symbol:{symbol}", chainId, symbol);
            var isGen0 = TokenSymbolHelper.GetIsGen0FromSymbol(symbol);
            var symbolIndex = new SchrodingerSymbolIndex
            {
                Id = GetSymbolIndexId(chainId, symbol), Symbol = symbol,
                SchrodingerInfo = TokenSymbolHelper.OfSchrodingerInfo(schrodingerIndex, eventValue, symbol, eventValue.TokenName)
            };
            if (!isGen0)
            {
                if (eventValue.ExternalInfo.Value.TryGetValue(SchrodingerConstants.NftAttributes, out var attributesJson))
                {
                    var attributeList = JsonConvert.DeserializeObject<List<Entities.Attribute>>(attributesJson?? string.Empty) ?? new List<Entities.Attribute>();
                    symbolIndex.Traits = Mapper.Map<List<Entities.Attribute>, List<TraitInfo>>(attributeList);
                    symbolIndex.TraitValues = string.Join(",", symbolIndex.Traits.Select(x =>
                    {
                        var traitType = x.TraitType.Replace(" ", "");
                        var value = x.Value.Replace(" ", "");
                        return traitType + value;
                    }))+",";
                    Logger.LogDebug("[TokenCreated] TraitValues:{TraitValues}", symbolIndex.TraitValues);
                }

                foreach (var trait in symbolIndex.Traits)
                {
                    await GenerateSchrodingerCountAsync(chainId, tick, trait.TraitType, trait.Value);
                }
            }

            var isGen9 = TokenSymbolHelper.GetIsGen9FromSchrodingerSymbolIndex(symbolIndex);
            if (isGen9)
            {
                var traitsGenOne = new List<List<string>>();
                var traitsGenTwoToNine = new List<List<string>>();
                GetTraitsInput(symbolIndex.Traits, traitsGenOne, traitsGenTwoToNine);
                var rank = GetRank(traitsGenOne, traitsGenTwoToNine);
                symbolIndex = SetRankRarity(symbolIndex, rank);
                Logger.LogDebug("[TokenCreated] get rank:{rank}, symbol:{symbol}", rank, symbol);
            }

            await SaveEntityAsync(symbolIndex);
            Logger.LogDebug("[TokenCreated] end chainId:{chainId} symbol:{symbol}", chainId, symbol);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[TokenCreated] Exception chainId:{chainId} symbol:{symbol}", chainId, symbol);
            throw;
        }
    }

    
    private void GetTraitsInput(List<TraitInfo> traitInfos, List<List<string>> traitsGenOne, List<List<string>> traitsGenTwoToNine)
    {
        var genOneTraitType = new List<string>();
        var genOneTraitValue = new List<string>();
        var genTwoTraitType = new List<string>();
        var genTwoTraitValue = new List<string>();
    
        for (int i=0; i<traitInfos.Count;i++)
        {
            if (i < 3)
            {
                genOneTraitType.Add(traitInfos[i].TraitType);
                genOneTraitValue.Add(traitInfos[i].Value);
                continue;
            }
            genTwoTraitType.Add(traitInfos[i].TraitType);
            genTwoTraitValue.Add(traitInfos[i].Value);
        }
        traitsGenOne.Add(genOneTraitType);
        traitsGenOne.Add(genOneTraitValue);
        traitsGenTwoToNine.Add(genTwoTraitType);
        traitsGenTwoToNine.Add(genTwoTraitValue);
    }
    
    
    private static SchrodingerSymbolIndex SetRankRarity(SchrodingerSymbolIndex symbolIndex, int rank)
    {
        symbolIndex.Rank = rank;
        symbolIndex.Level = "";
        symbolIndex.Grade = "";
        symbolIndex.Star = "";
        symbolIndex.Rarity = "";
        
        //get level
        var rankRes = LevelConstant.RankLevelGradeDictionary.TryGetValue(rank.ToString(), out var leaveGradeStar);
        if (!rankRes)
        {
            return symbolIndex;
        }
        symbolIndex.Level = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[0];
        symbolIndex.Grade = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[1];
        symbolIndex.Star = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[2];
    
        //get rarity
        symbolIndex.Rarity = LevelConstant.RarityDictionary.TryGetValue(symbolIndex.Level ?? "", out var rarity)
            ? rarity 
            : "";
        return symbolIndex;
    }
    
    
    private int GetRank(List<List<string>> traitsGenOne, List<List<string>> traitsGenTwoToNine)
    {
        try
        {
            // var traitTypes = traitsGenTwoToNine[0];
            // var traitValue = traitsGenTwoToNine[1];
            // if (traitTypes.Contains("Face") && traitValue.Contains("WUKONG Face Paint"))
            // {
            //     var index = traitValue.IndexOf("WUKONG Face Paint");
            //     traitsGenTwoToNine[1][index] = "Monkey King Face Paint";
            // }
            
            var newTraitOneType = TraitHelper.ReplaceTraitValues(traitsGenOne[0], traitsGenOne[1]);
            var newTraitTwoToNineType = TraitHelper.ReplaceTraitValues(traitsGenTwoToNine[0], traitsGenTwoToNine[1]);
            traitsGenOne[1] = newTraitOneType;
            traitsGenTwoToNine[1] = newTraitTwoToNineType;
            
            var rankOfGenOneProbabilityTypes = getRankOfGenOne(traitsGenOne);
            var rankTwoToNineProbabilityTypes = getRankTwoToNine(traitsGenTwoToNine);
            var rank = getRankOneToNine(rankOfGenOneProbabilityTypes, rankTwoToNineProbabilityTypes);
            return rank;
        }
        catch (Exception e)
        {
            Logger.LogError("GetRank Error: {err}", e.Message);
            return 0;
        }
    }
    
    
    private int getRankOneToNine(List<string> rankOfGenOneProbabilityTypes, List<string> rankTwoToNineProbabilityTypes) 
    {
        
        rankOfGenOneProbabilityTypes.AddRange(rankTwoToNineProbabilityTypes);
        var wordType = string.Join("", rankOfGenOneProbabilityTypes);
    
        var probabilityMap = _probabilityMapAppAttachmentValueProvider.GetValue().Data;
        string probability = "";
        if (probabilityMap.TryGetValue(wordType, out var value))
        {
            probability = value;
        }
        else
        {
            // var probabilityPartB = _probabilityMapPartBAppAttachmentValueProvider.GetValue().Data;
            // if (probabilityPartB.TryGetValue(wordType, out var value2))
            // {
            //     probability = value2;
            // }
            // else
            // {
            //     Logger.LogError("wordType not found in probability map:{wordType}", wordType);
            // }
            
            Logger.LogError("wordType not found in probability map:{wordType}", wordType);
        }
    
        int rankIndex = 0;
        if (!probability.IsNullOrEmpty())
        {
            rankIndex = _probabilityRankMapAppAttachmentValueProvider.GetValue().Data.IndexOf(probability) + 1;
        }
        
        return rankIndex;
    }
    
    
    private  string GetAGWord(string type)
    {
        return Constants.Mappings.TypeABCDEFGMap[type];
    }
    
    
    private int GetTraitLevel(string type, string trait)
    {
        var traitProbability = _traitValueProbability[type][trait];
        return _traitTypeProbability[type][traitProbability.ToString()];
    }
    
    
    private List<string> getRankOfGenOne(List<List<string>> traits)
    {
        var probabilityTypes = traits[0].Zip(
            traits[1],
            (type, trait) =>
            {
                var word = GetAGWord(type);
                var level = GetTraitLevel(type, trait);
                return word + level;
            }).ToList();
        
        return probabilityTypes;
    }
    
    
    private List<string> getRankTwoToNine(List<List<string>> traits)
    {
        var probabilityTypes = traits[0].Zip(
            traits[1],
            (type, trait) =>
            {
                var typeMapped = type;
                if (Constants.Mappings.TwoToNineTypeMap.TryGetValue(type, out var value))
                {
                    typeMapped = value;
                }
                var typeMappedTrimmed = typeMapped.Trim();
                var word = GetAGWord(typeMappedTrimmed);
                var level = GetTraitLevel(typeMappedTrimmed, trait);
                return word + level;
            }).ToList();
        
        var sortedProbabilityTypes = sortProbability(probabilityTypes);
        return sortedProbabilityTypes;
    }
    
    
    private List<string> sortProbability(List<string> probabilityTypes)
    {
        var list1 = new  List<string>();
        var list2 = new  List<string>();
        foreach (var item in probabilityTypes)
        {
            if (item.StartsWith("D") || item.StartsWith("G"))
            {
                list1.Add(item);
            }
            else
            {
                list2.Add(item);
            }
        }
        
        var sortedList1 = list1.OrderBy(x => x[0]).ThenBy(x => x[1]).ToList();
        var sortedList2 = list2.OrderBy(x => x[0]).ThenBy(x => x[1]).ToList();
        sortedList2.AddRange(sortedList1);
        return sortedList2;
    }
}