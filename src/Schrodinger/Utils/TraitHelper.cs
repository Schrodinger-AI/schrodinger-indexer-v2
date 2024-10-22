using Newtonsoft.Json;

namespace Schrodinger.Utils;


public class SpecialTrait
{
  public string Id { get; set; }
  public string Tag { get; set; }
  public Dictionary<string, Dictionary<string, string>> ReplaceTraits { get; set; } = new(); // {Type: { NewValue: OldValue } }
}


public static class TraitHelper
{ 
  private const string TraitOption = """
                                     [
                                       {
                                        "Id": "Wukong",
                                        "ReplaceTraits": {
                                          "Face": {"WUKONG Face Paint": "Monkey King Face Paint"}
                                        }
                                       },
                                       {
                                         "Id": "elon s1",
                                         "Tag": "elon s1",
                                         "ReplaceTraits": {
                                           "Background": {"Mars": "Misty Marshlands" },
                                           "Clothes": {"Devil's Champion Leather Armor Set": "Light Armor" },
                                           "Hat": {"The Boring Company Cap": "Baseball Cap" },
                                           "Pet": {"Floki": "Puppy" },
                                           "Belt": {"BJJ Belt": "Rodeo Belt" },
                                           "Ride": {"Cyber TRK": "Pickup Truck" },
                                           "Face": {"Weed": "Grass" },
                                           "Accessory": {"Surfboard": "Toilet Paper" },
                                           "Eyes": {"Aviator Sunglass": "Sunglass" },
                                           "Weapon": {"Flamethrower X": "Flamethrower" }
                                         }
                                       },
                                       {
                                       "Id": "Trump",
                                       "Tag": "Trump",
                                       "ReplaceTraits": {
                                         "Clothes": {"Red Tie": "Black Suit" },
                                         "Mouth": {"Shouting Fight": "Shouting" },
                                         "Accessory": {"Coke": "Pep Drink" },
                                         "Theme": {"Billionaire": "Russian Maslenitsa snowscape" },
                                         "Ride": {"Red Elephant": "Elephant" }
                                         }
                                      },
                                      {
                                       "Id": "Harris",
                                       "Tag": "Harris",
                                       "ReplaceTraits": {
                                         "Clothes": {"Purple Dress": "Dress" },
                                         "Mouth": {"HAHAHA": "Laughing" },
                                         "Accessory": {"Earrings": "Ring" },
                                         "Theme": {"Prosecutor": "Ukrainian sunflower vinok" },
                                         "Ride": {"Blue Donkey": "Horse-drawn" }
                                        }
                                      }
                                     ]
                                     """;
  private static List<SpecialTrait> traitConfig = JsonConvert.DeserializeObject<List<SpecialTrait>>(TraitOption);
  
  
  public static List<string> ReplaceTraitValues(List<string> traitTypes, List<string> traitValues)
  {
    var newValues = new List<string>();

    var cnt = traitTypes.Count;
    for (var i = 0; i < cnt; i++)
    {
      var traitType = traitTypes[i];
      var traitValue = traitValues[i];
      var newValue = traitValue;
            
      foreach (var specialTrait in traitConfig)
      {
        var replaceItems = specialTrait.ReplaceTraits;
        if (!replaceItems.ContainsKey(traitType))
        {
          continue;
        }

        var replaceValue = replaceItems[traitType];
        if (!replaceValue.ContainsKey(traitValue))
        {
          continue;
        }

        newValue = replaceValue[traitValue];
      }
            
      newValues.Add(newValue);
    }
        
    return newValues;
  }
}