using AeFinder.Sdk.Attachments;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Processors.Provider;
using Schrodinger.Processors.Provider.dto;
using Schrodinger.Utils;

namespace Schrodinger.Processors;


public class SchrodingerAdoptProcessor : SchrodingerProcessorBase<Adopted>
{
    private readonly Dictionary<string, Dictionary<string, double>> _traitValueProbability;
    private readonly Dictionary<string, Dictionary<string, int>> _traitTypeProbability;
    
    private readonly IAppAttachmentValueProvider<ProbabilityRankMap> _probabilityRankMapAppAttachmentValueProvider;
    private readonly IAppAttachmentValueProvider<ProbabilityMap> _probabilityMapAppAttachmentValueProvider;
    
    public  SchrodingerAdoptProcessor(
        IAppAttachmentValueProvider<ProbabilityRankMap> probabilityRankMapAppAttachmentValueProvider, 
        IAppAttachmentValueProvider<ProbabilityMap> probabilityMapAppAttachmentValueProvider)
    {
        _probabilityRankMapAppAttachmentValueProvider = probabilityRankMapAppAttachmentValueProvider;
        _probabilityMapAppAttachmentValueProvider = probabilityMapAppAttachmentValueProvider;
        
        _traitValueProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(TraitProbabilityConstants.TraitValueProbability);
        _traitTypeProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(TraitProbabilityConstants.TraitTypeProbability);
    }
    
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
            
            if (adopt.Gen == 9)
            {
                var traitsGenOne = new List<List<string>>();
                var traitsGenTwoToNine = new List<List<string>>();
                GetTraitsInput(Mapper.Map<List<Entities.Attribute>, List<TraitInfo>>(adopt.Attributes), traitsGenOne, traitsGenTwoToNine);
                var rank = GetRank(traitsGenOne, traitsGenTwoToNine);
                // var rank = _rankProvider.GetRank(Mapper.Map<List<Entities.Attribute>, List<TraitInfo>>(adopt.Attributes));
                adopt = SetRankRarity(adopt, rank);
                Logger.LogDebug("[Adopted] get rank:{rank}, symbol:{symbol}", rank, symbol);
            }
            
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