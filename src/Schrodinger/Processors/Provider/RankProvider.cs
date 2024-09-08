// using AeFinder.Sdk.Attachments;
// using AeFinder.Sdk.Logging;
// using Microsoft.Extensions.Logging;
// using Newtonsoft.Json;
// using Schrodinger.Entities;
// using Schrodinger.Processors.Provider.dto;
// using Schrodinger.Utils;
//
// namespace Schrodinger.Processors.Provider;
//
//
// public class ProbabilityRankMapAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityRankMap>
// {
//     public override string Key => "ProbabilityRankMapNew";
// }
//
// public class ProbabilityMapAppAttachmentValueProvider : AppAttachmentValueProviderBase<ProbabilityMap>
// {
//     public override string Key => "ProbabilityMapNew";
// }
//
//
// public class RankProvider
// {
//     private readonly Dictionary<string, Dictionary<string, double>> _traitValueProbability;
//     private readonly Dictionary<string, Dictionary<string, int>> _traitTypeProbability;
//     
//     private readonly IAppAttachmentValueProvider<ProbabilityRankMap> _probabilityRankMapAppAttachmentValueProvider;
//     private readonly IAppAttachmentValueProvider<ProbabilityMap> _probabilityMapAppAttachmentValueProvider;
//
//     private readonly IAeFinderLogger _logger;
//     
//     public RankProvider(
//         IAppAttachmentValueProvider<ProbabilityRankMap> probabilityRankMapAppAttachmentValueProvider, 
//         IAppAttachmentValueProvider<ProbabilityMap> probabilityMapAppAttachmentValueProvider, 
//         IAeFinderLogger logger) 
//     {
//         _probabilityRankMapAppAttachmentValueProvider = probabilityRankMapAppAttachmentValueProvider;
//         _probabilityMapAppAttachmentValueProvider = probabilityMapAppAttachmentValueProvider;
//         
//         _traitValueProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(TraitProbabilityConstants.TraitValueProbability);
//         _traitTypeProbability = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(TraitProbabilityConstants.TraitTypeProbability);
//         
//         _logger = logger;
//     }
//     
//     public int GetRank(List<TraitInfo> traitInfos)
//     {
//         try
//         {
//             var genOneTraitType = new List<string>();
//             var genOneTraitValue = new List<string>();
//             var genTwoTraitType = new List<string>();
//             var genTwoTraitValue = new List<string>();
//             var traitsGenOne = new List<List<string>>();
//             var traitsGenTwoToNine = new List<List<string>>();
//
//             for (int i=0; i<traitInfos.Count;i++)
//             {
//                 if (i < 3)
//                 {
//                     genOneTraitType.Add(traitInfos[i].TraitType);
//                     genOneTraitValue.Add(traitInfos[i].Value);
//                     continue;
//                 }
//                 genTwoTraitType.Add(traitInfos[i].TraitType);
//                 genTwoTraitValue.Add(traitInfos[i].Value);
//             }
//             traitsGenOne.Add(genOneTraitType);
//             traitsGenOne.Add(genOneTraitValue);
//             traitsGenTwoToNine.Add(genTwoTraitType);
//             traitsGenTwoToNine.Add(genTwoTraitValue);
//             
//             var rankOfGenOneProbabilityTypes = getRankOfGenOne(traitsGenOne);
//             var rankTwoToNineProbabilityTypes = getRankTwoToNine(traitsGenTwoToNine);
//             var rank = getRankOneToNine(rankOfGenOneProbabilityTypes, rankTwoToNineProbabilityTypes);
//             return rank;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError("GetRank Error: {err}", e.Message);
//             return 0;
//         }
//     }
//     
//     private int getRankOneToNine(List<string> rankOfGenOneProbabilityTypes, List<string> rankTwoToNineProbabilityTypes) 
//     {
//         
//         rankOfGenOneProbabilityTypes.AddRange(rankTwoToNineProbabilityTypes);
//         var wordType = string.Join("", rankOfGenOneProbabilityTypes);
//     
//         var probabilityMap = _probabilityMapAppAttachmentValueProvider.GetValue().Data;
//         string probability = "";
//         if (probabilityMap.TryGetValue(wordType, out var value))
//         {
//             probability = value;
//         }
//         else
//         {
//             // var probabilityPartB = _probabilityMapPartBAppAttachmentValueProvider.GetValue().Data;
//             // if (probabilityPartB.TryGetValue(wordType, out var value2))
//             // {
//             //     probability = value2;
//             // }
//             // else
//             // {
//             //     Logger.LogError("wordType not found in probability map:{wordType}", wordType);
//             // }
//             
//             _logger.LogError("wordType not found in probability map:{wordType}", wordType);
//         }
//     
//         int rankIndex = 0;
//         if (!probability.IsNullOrEmpty())
//         {
//             rankIndex = _probabilityRankMapAppAttachmentValueProvider.GetValue().Data.IndexOf(probability) + 1;
//         }
//         
//         return rankIndex;
//     }
//     
//     
//     private  string GetAGWord(string type)
//     {
//         return Constants.Mappings.TypeABCDEFGMap[type];
//     }
//     
//     
//     private int GetTraitLevel(string type, string trait)
//     {
//         var traitProbability = _traitValueProbability[type][trait];
//         return _traitTypeProbability[type][traitProbability.ToString()];
//     }
//     
//     
//     private List<string> getRankOfGenOne(List<List<string>> traits)
//     {
//         var probabilityTypes = traits[0].Zip(
//             traits[1],
//             (type, trait) =>
//             {
//                 var word = GetAGWord(type);
//                 var level = GetTraitLevel(type, trait);
//                 return word + level;
//             }).ToList();
//         
//         return probabilityTypes;
//     }
//     
//     
//     private List<string> getRankTwoToNine(List<List<string>> traits)
//     {
//         var probabilityTypes = traits[0].Zip(
//             traits[1],
//             (type, trait) =>
//             {
//                 var typeMapped = type;
//                 if (Constants.Mappings.TwoToNineTypeMap.TryGetValue(type, out var value))
//                 {
//                     typeMapped = value;
//                 }
//                 var typeMappedTrimmed = typeMapped.Trim();
//                 var word = GetAGWord(typeMappedTrimmed);
//                 var level = GetTraitLevel(typeMappedTrimmed, trait);
//                 return word + level;
//             }).ToList();
//         
//         var sortedProbabilityTypes = sortProbability(probabilityTypes);
//         return sortedProbabilityTypes;
//     }
//     
//     private List<string> sortProbability(List<string> probabilityTypes)
//     {
//         var list1 = new  List<string>();
//         var list2 = new  List<string>();
//         foreach (var item in probabilityTypes)
//         {
//             if (item.StartsWith("D") || item.StartsWith("G"))
//             {
//                 list1.Add(item);
//             }
//             else
//             {
//                 list2.Add(item);
//             }
//         }
//         
//         var sortedList1 = list1.OrderBy(x => x[0]).ThenBy(x => x[1]).ToList();
//         var sortedList2 = list2.OrderBy(x => x[0]).ThenBy(x => x[1]).ToList();
//         sortedList2.AddRange(sortedList1);
//         return sortedList2;
//     }
//     
// }