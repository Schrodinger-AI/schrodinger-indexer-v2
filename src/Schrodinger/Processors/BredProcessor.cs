using AeFinder.Sdk.Attachments;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Processors.Provider.dto;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class BredProcessor: SchrodingerProcessorBase<Merged>
{
    private readonly Dictionary<string, Dictionary<string, double>> _traitValueProbability;
    private readonly Dictionary<string, Dictionary<string, int>> _traitTypeProbability;
    
    private readonly IAppAttachmentValueProvider<ProbabilityRankMap> _probabilityRankMapAppAttachmentValueProvider;
    private readonly IAppAttachmentValueProvider<ProbabilityMap> _probabilityMapAppAttachmentValueProvider;
    
    public  BredProcessor(
        IAppAttachmentValueProvider<ProbabilityRankMap> probabilityRankMapAppAttachmentValueProvider, 
        IAppAttachmentValueProvider<ProbabilityMap> probabilityMapAppAttachmentValueProvider)
    {
        _probabilityRankMapAppAttachmentValueProvider = probabilityRankMapAppAttachmentValueProvider;
        _probabilityMapAppAttachmentValueProvider = probabilityMapAppAttachmentValueProvider;
        
        _traitValueProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(TraitProbabilityConstants.TraitValueProbability);
        _traitTypeProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(TraitProbabilityConstants.TraitTypeProbability);
    }
    
    public override async Task ProcessAsync(Merged bred, LogEventContext context)
    {
        var chainId = context.ChainId;

        var adoptInfo = bred.AdoptInfo;
        var symbol = adoptInfo.Symbol;
        var adoptId = adoptInfo.AdoptId?.ToHex();
        Logger.LogDebug("[Bred] start chainId:{chainId} symbol:{symbol} adoptId:{adoptId} level:{level}", chainId,
            symbol, adoptId, adoptInfo.Level);
        try
        {
            var adopt = Mapper.Map<AdoptInfo, SchrodingerAdoptIndex>(adoptInfo);

            adopt.Id = IdGenerateHelper.GetId(chainId, symbol);
            adopt.AdoptTime = context.Block.BlockTime;
            // adopt.ParentInfo = await getSchrodingerInfo(chainId, parent);
            adopt.TransactionId = context.Transaction.TransactionId;
            adopt.LossAmount = bred.LossAmount;
            adopt.CommissionAmount = bred.CommissionAmount;

            adopt.ParentInfo = new SchrodingerInfo
            {
                Symbol = "",
                Tick = "SGR",
                Decimals = 8,
                TokenName = "",
                Gen = adoptInfo.Gen - 1,
                InscriptionImageUri = ""
            };
            
            if (adopt.Gen == 9)
            {
                var traitsGenOne = new List<List<string>>();
                var traitsGenTwoToNine = new List<List<string>>();
                GetTraitsInput(Mapper.Map<List<Entities.Attribute>, List<TraitInfo>>(adopt.Attributes), traitsGenOne, traitsGenTwoToNine);
                if (adoptInfo.Level > 0)
                {
                    var rank = LevelConstant.LevelRankList[(int)(adoptInfo.Level - 1)];
                    adopt = SetRankRarity(adopt, rank);
                    Logger.LogDebug("[Bred] get rank:{rank}, symbol:{symbol}", rank, symbol);
                }
                else
                {
                    var rank = GetRank(traitsGenOne, traitsGenTwoToNine);
                    adopt = SetRankRarity(adopt, rank);
                    Logger.LogDebug("[Bred] compute rank:{rank}, symbol:{symbol}", rank, symbol);
                }
            }
            
            // save new adopt index
            await SaveEntityAsync(adopt);
            
            
            // save bred index
            var bredIndex = Mapper.Map<Merged, BredIndex>(bred);
            bredIndex.Id = IdGenerateHelper.GetId(chainId, symbol);
            bredIndex.AdoptId = adoptId;
            bredIndex.Symbol = symbol;
            bredIndex.Adopter = adoptInfo.Adopter.ToBase58();
            bredIndex.AdoptTime = context.Block.BlockTime;
            bredIndex.InputAmount = adoptInfo.InputAmount;
            bredIndex.OutputAmount = adoptInfo.OutputAmount;
            bredIndex.Rank = adopt.Rank;
            bredIndex.Gen = adopt.Gen;
            bredIndex.Level = adopt.Level.IsNullOrEmpty() ? 0 : int.Parse(adopt.Level);
            
            bredIndex.TransactionId = context.Transaction.TransactionId;
            await SaveEntityAsync(bredIndex);
            
            
            // modify amount in two old adopt index
            var idA = IdGenerateHelper.GetId(chainId, bred.SymbolA);
            var parentAIndex = await GetEntityAsync<SchrodingerAdoptIndex>(idA);
            parentAIndex.OutputAmount = bred.AmountA;
            await SaveEntityAsync(parentAIndex);
            
            var idB = IdGenerateHelper.GetId(chainId, bred.SymbolB);
            var parentBIndex = await GetEntityAsync<SchrodingerAdoptIndex>(idB);
            parentBIndex.OutputAmount = bred.AmountB;
            await SaveEntityAsync(parentBIndex);
            
            
            Logger.LogDebug("[Bred] end chainId:{chainId} symbol:{symbol}, adoptId:{adoptId}, transactionId:{TransactionId}", chainId, symbol,
                adoptId, adopt.TransactionId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[AdoptionUpdated] Exception chainId:{chainId} symbol:{symbol}, adoptId:{adoptId}", chainId,
                symbol,
                adoptId);
            throw;
        }
    }
    
    private async Task<SchrodingerInfo> getSchrodingerInfo(string chainId, string symbol)
    {
        var symbolId = IdGenerateHelper.GetId(chainId, symbol);
        var symbolIndex = await GetEntityAsync<SchrodingerSymbolIndex>(symbolId);
        return symbolIndex?.SchrodingerInfo ?? new SchrodingerInfo();
    }
    
    private static SchrodingerAdoptIndex SetRankRarity(SchrodingerAdoptIndex adoptIndex, int rank)
    {
        adoptIndex.Rank = rank;
        adoptIndex.Level = "";
        adoptIndex.Grade = "";
        adoptIndex.Star = "";
        adoptIndex.Rarity = "";
        
        //get level
        var rankRes = LevelConstant.RankLevelGradeDictionary.TryGetValue(rank.ToString(), out var leaveGradeStar);
        if (!rankRes)
        {
            return adoptIndex;
        }
        adoptIndex.Level = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[0];
        adoptIndex.Grade = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[1];
        adoptIndex.Star = leaveGradeStar.Split(SchrodingerConstants.RankLevelSegment)[2];
    
        //get rarity
        adoptIndex.Rarity = LevelConstant.RarityDictionary.TryGetValue(adoptIndex.Level ?? "", out var rarity)
            ? rarity 
            : "";
        return adoptIndex;
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