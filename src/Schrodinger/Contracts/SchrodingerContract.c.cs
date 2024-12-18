// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: schrodinger_contract.proto
// </auto-generated>
// Original file comments:
// the version of the language, use proto3 for contracts
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Schrodinger {

  #region Events
  public partial class Joined : aelf::IEvent<Joined>
  {
    public global::System.Collections.Generic.IEnumerable<Joined> GetIndexed()
    {
      return new List<Joined>
      {
      };
    }

    public Joined GetNonIndexed()
    {
      return new Joined
      {
        Domain = Domain,
        Registrant = Registrant,
      };
    }
  }

  public partial class Deployed : aelf::IEvent<Deployed>
  {
    public global::System.Collections.Generic.IEnumerable<Deployed> GetIndexed()
    {
      return new List<Deployed>
      {
      };
    }

    public Deployed GetNonIndexed()
    {
      return new Deployed
      {
        Tick = Tick,
        Ancestor = Ancestor,
        MaxGeneration = MaxGeneration,
        TotalSupply = TotalSupply,
        Decimals = Decimals,
        AttributeLists = AttributeLists,
        ImageCount = ImageCount,
        Issuer = Issuer,
        Owner = Owner,
        IssueChainId = IssueChainId,
        ExternalInfos = ExternalInfos,
        TokenName = TokenName,
        Deployer = Deployer,
        CrossGenerationConfig = CrossGenerationConfig,
        IsWeightEnabled = IsWeightEnabled,
        Admin = Admin,
        LossRate = LossRate,
        CommissionRate = CommissionRate,
        AttributesPerGen = AttributesPerGen,
        Signatory = Signatory,
        ImageUri = ImageUri,
        MaxGenLossRate = MaxGenLossRate,
      };
    }
  }

  public partial class Adopted : aelf::IEvent<Adopted>
  {
    public global::System.Collections.Generic.IEnumerable<Adopted> GetIndexed()
    {
      return new List<Adopted>
      {
      };
    }

    public Adopted GetNonIndexed()
    {
      return new Adopted
      {
        AdoptId = AdoptId,
        Parent = Parent,
        ParentGen = ParentGen,
        InputAmount = InputAmount,
        LossAmount = LossAmount,
        CommissionAmount = CommissionAmount,
        OutputAmount = OutputAmount,
        ImageCount = ImageCount,
        Adopter = Adopter,
        BlockHeight = BlockHeight,
        Attributes = Attributes,
        Gen = Gen,
        Ancestor = Ancestor,
        Symbol = Symbol,
        TokenName = TokenName,
      };
    }
  }

  public partial class Confirmed : aelf::IEvent<Confirmed>
  {
    public global::System.Collections.Generic.IEnumerable<Confirmed> GetIndexed()
    {
      return new List<Confirmed>
      {
      };
    }

    public Confirmed GetNonIndexed()
    {
      return new Confirmed
      {
        AdoptId = AdoptId,
        Parent = Parent,
        Symbol = Symbol,
        TotalSupply = TotalSupply,
        Decimals = Decimals,
        Gen = Gen,
        Attributes = Attributes,
        Issuer = Issuer,
        Owner = Owner,
        IssueChainId = IssueChainId,
        ExternalInfos = ExternalInfos,
        TokenName = TokenName,
        Deployer = Deployer,
        ImageUri = ImageUri,
      };
    }
  }

  public partial class Rerolled : aelf::IEvent<Rerolled>
  {
    public global::System.Collections.Generic.IEnumerable<Rerolled> GetIndexed()
    {
      return new List<Rerolled>
      {
      };
    }

    public Rerolled GetNonIndexed()
    {
      return new Rerolled
      {
        Symbol = Symbol,
        Ancestor = Ancestor,
        Amount = Amount,
        Recipient = Recipient,
      };
    }
  }

  public partial class FixedAttributeSet : aelf::IEvent<FixedAttributeSet>
  {
    public global::System.Collections.Generic.IEnumerable<FixedAttributeSet> GetIndexed()
    {
      return new List<FixedAttributeSet>
      {
      };
    }

    public FixedAttributeSet GetNonIndexed()
    {
      return new FixedAttributeSet
      {
        Tick = Tick,
        RemovedAttribute = RemovedAttribute,
        AddedAttribute = AddedAttribute,
      };
    }
  }

  public partial class RandomAttributeSet : aelf::IEvent<RandomAttributeSet>
  {
    public global::System.Collections.Generic.IEnumerable<RandomAttributeSet> GetIndexed()
    {
      return new List<RandomAttributeSet>
      {
      };
    }

    public RandomAttributeSet GetNonIndexed()
    {
      return new RandomAttributeSet
      {
        Tick = Tick,
        RemovedAttribute = RemovedAttribute,
        AddedAttribute = AddedAttribute,
      };
    }
  }

  public partial class AttributesPerGenerationSet : aelf::IEvent<AttributesPerGenerationSet>
  {
    public global::System.Collections.Generic.IEnumerable<AttributesPerGenerationSet> GetIndexed()
    {
      return new List<AttributesPerGenerationSet>
      {
      };
    }

    public AttributesPerGenerationSet GetNonIndexed()
    {
      return new AttributesPerGenerationSet
      {
        Tick = Tick,
        AttributesPerGen = AttributesPerGen,
      };
    }
  }

  public partial class ImageCountSet : aelf::IEvent<ImageCountSet>
  {
    public global::System.Collections.Generic.IEnumerable<ImageCountSet> GetIndexed()
    {
      return new List<ImageCountSet>
      {
      };
    }

    public ImageCountSet GetNonIndexed()
    {
      return new ImageCountSet
      {
        Tick = Tick,
        ImageCount = ImageCount,
      };
    }
  }

  public partial class MaxGenerationSet : aelf::IEvent<MaxGenerationSet>
  {
    public global::System.Collections.Generic.IEnumerable<MaxGenerationSet> GetIndexed()
    {
      return new List<MaxGenerationSet>
      {
      };
    }

    public MaxGenerationSet GetNonIndexed()
    {
      return new MaxGenerationSet
      {
        Tick = Tick,
        Gen = Gen,
      };
    }
  }

  public partial class RatesSet : aelf::IEvent<RatesSet>
  {
    public global::System.Collections.Generic.IEnumerable<RatesSet> GetIndexed()
    {
      return new List<RatesSet>
      {
      };
    }

    public RatesSet GetNonIndexed()
    {
      return new RatesSet
      {
        Tick = Tick,
        LossRate = LossRate,
        CommissionRate = CommissionRate,
        MaxGenLossRate = MaxGenLossRate,
      };
    }
  }

  public partial class RecipientSet : aelf::IEvent<RecipientSet>
  {
    public global::System.Collections.Generic.IEnumerable<RecipientSet> GetIndexed()
    {
      return new List<RecipientSet>
      {
      };
    }

    public RecipientSet GetNonIndexed()
    {
      return new RecipientSet
      {
        Tick = Tick,
        Recipient = Recipient,
      };
    }
  }

  public partial class InscriptionAdminSet : aelf::IEvent<InscriptionAdminSet>
  {
    public global::System.Collections.Generic.IEnumerable<InscriptionAdminSet> GetIndexed()
    {
      return new List<InscriptionAdminSet>
      {
      };
    }

    public InscriptionAdminSet GetNonIndexed()
    {
      return new InscriptionAdminSet
      {
        Tick = Tick,
        Admin = Admin,
      };
    }
  }

  public partial class CrossGenerationConfigSet : aelf::IEvent<CrossGenerationConfigSet>
  {
    public global::System.Collections.Generic.IEnumerable<CrossGenerationConfigSet> GetIndexed()
    {
      return new List<CrossGenerationConfigSet>
      {
      };
    }

    public CrossGenerationConfigSet GetNonIndexed()
    {
      return new CrossGenerationConfigSet
      {
        Tick = Tick,
        CrossGenerationConfig = CrossGenerationConfig,
      };
    }
  }

  public partial class ConfigSet : aelf::IEvent<ConfigSet>
  {
    public global::System.Collections.Generic.IEnumerable<ConfigSet> GetIndexed()
    {
      return new List<ConfigSet>
      {
      };
    }

    public ConfigSet GetNonIndexed()
    {
      return new ConfigSet
      {
        Config = Config,
      };
    }
  }

  public partial class MaxGenerationConfigSet : aelf::IEvent<MaxGenerationConfigSet>
  {
    public global::System.Collections.Generic.IEnumerable<MaxGenerationConfigSet> GetIndexed()
    {
      return new List<MaxGenerationConfigSet>
      {
      };
    }

    public MaxGenerationConfigSet GetNonIndexed()
    {
      return new MaxGenerationConfigSet
      {
        MaxGen = MaxGen,
      };
    }
  }

  public partial class ImageMaxSizeSet : aelf::IEvent<ImageMaxSizeSet>
  {
    public global::System.Collections.Generic.IEnumerable<ImageMaxSizeSet> GetIndexed()
    {
      return new List<ImageMaxSizeSet>
      {
      };
    }

    public ImageMaxSizeSet GetNonIndexed()
    {
      return new ImageMaxSizeSet
      {
        ImageMaxSize = ImageMaxSize,
      };
    }
  }

  public partial class ImageMaxCountSet : aelf::IEvent<ImageMaxCountSet>
  {
    public global::System.Collections.Generic.IEnumerable<ImageMaxCountSet> GetIndexed()
    {
      return new List<ImageMaxCountSet>
      {
      };
    }

    public ImageMaxCountSet GetNonIndexed()
    {
      return new ImageMaxCountSet
      {
        ImageMaxCount = ImageMaxCount,
      };
    }
  }

  public partial class AttributeConfigSet : aelf::IEvent<AttributeConfigSet>
  {
    public global::System.Collections.Generic.IEnumerable<AttributeConfigSet> GetIndexed()
    {
      return new List<AttributeConfigSet>
      {
      };
    }

    public AttributeConfigSet GetNonIndexed()
    {
      return new AttributeConfigSet
      {
        TraitTypeMaxCount = TraitTypeMaxCount,
        TraitValueMaxCount = TraitValueMaxCount,
        AttributeMaxLength = AttributeMaxLength,
        MaxAttributesPerGen = MaxAttributesPerGen,
        FixedTraitTypeMaxCount = FixedTraitTypeMaxCount,
      };
    }
  }

  public partial class AdminSet : aelf::IEvent<AdminSet>
  {
    public global::System.Collections.Generic.IEnumerable<AdminSet> GetIndexed()
    {
      return new List<AdminSet>
      {
      };
    }

    public AdminSet GetNonIndexed()
    {
      return new AdminSet
      {
        Admin = Admin,
      };
    }
  }

  public partial class SignatorySet : aelf::IEvent<SignatorySet>
  {
    public global::System.Collections.Generic.IEnumerable<SignatorySet> GetIndexed()
    {
      return new List<SignatorySet>
      {
      };
    }

    public SignatorySet GetNonIndexed()
    {
      return new SignatorySet
      {
        Tick = Tick,
        Signatory = Signatory,
      };
    }
  }

  public partial class ImageUriMaxSizeSet : aelf::IEvent<ImageUriMaxSizeSet>
  {
    public global::System.Collections.Generic.IEnumerable<ImageUriMaxSizeSet> GetIndexed()
    {
      return new List<ImageUriMaxSizeSet>
      {
      };
    }

    public ImageUriMaxSizeSet GetNonIndexed()
    {
      return new ImageUriMaxSizeSet
      {
        ImageUriMaxSize = ImageUriMaxSize,
      };
    }
  }

  public partial class ReferralAccepted : aelf::IEvent<ReferralAccepted>
  {
    public global::System.Collections.Generic.IEnumerable<ReferralAccepted> GetIndexed()
    {
      return new List<ReferralAccepted>
      {
      };
    }

    public ReferralAccepted GetNonIndexed()
    {
      return new ReferralAccepted
      {
        Referrer = Referrer,
        Invitee = Invitee,
      };
    }
  }

  public partial class OfficialDomainAliasSet : aelf::IEvent<OfficialDomainAliasSet>
  {
    public global::System.Collections.Generic.IEnumerable<OfficialDomainAliasSet> GetIndexed()
    {
      return new List<OfficialDomainAliasSet>
      {
      };
    }

    public OfficialDomainAliasSet GetNonIndexed()
    {
      return new OfficialDomainAliasSet
      {
        Alias = Alias,
      };
    }
  }

  public partial class AdoptionRerolled : aelf::IEvent<AdoptionRerolled>
  {
    public global::System.Collections.Generic.IEnumerable<AdoptionRerolled> GetIndexed()
    {
      return new List<AdoptionRerolled>
      {
      };
    }

    public AdoptionRerolled GetNonIndexed()
    {
      return new AdoptionRerolled
      {
        AdoptId = AdoptId,
        Symbol = Symbol,
        Amount = Amount,
        Account = Account,
      };
    }
  }

  public partial class AdoptionUpdated : aelf::IEvent<AdoptionUpdated>
  {
    public global::System.Collections.Generic.IEnumerable<AdoptionUpdated> GetIndexed()
    {
      return new List<AdoptionUpdated>
      {
      };
    }

    public AdoptionUpdated GetNonIndexed()
    {
      return new AdoptionUpdated
      {
        AdoptId = AdoptId,
        Parent = Parent,
        ParentGen = ParentGen,
        InputAmount = InputAmount,
        LossAmount = LossAmount,
        CommissionAmount = CommissionAmount,
        OutputAmount = OutputAmount,
        ImageCount = ImageCount,
        Adopter = Adopter,
        BlockHeight = BlockHeight,
        Attributes = Attributes,
        Gen = Gen,
        Ancestor = Ancestor,
        Symbol = Symbol,
        TokenName = TokenName,
      };
    }
  }

  #endregion
}
#endregion

