using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.Utils;


namespace Schrodinger.Processors;

public class DeployedLogEventProcessor: SchrodingerProcessorBase<Deployed>
{

    public override async Task ProcessAsync(Deployed eventValue, LogEventContext context)
    {
        Logger.LogDebug("[Deployed] begin");
        
        try
        {
            if (eventValue == null || context == null) return;

            var schrodingerId = IdGenerateHelper.GetId(context.ChainId, eventValue.Tick);
            var fixedAttributeList = new List<Entities.AttributeSet>();
            var randomAttributeList = new List<Entities.AttributeSet>();
            if (eventValue.AttributeLists != null)
            {
                if (eventValue.AttributeLists.FixedAttributes != null)
                {
                    fixedAttributeList =
                        Mapper.Map<List<AttributeSet>, List<Entities.AttributeSet>>(eventValue
                            .AttributeLists
                            .FixedAttributes.ToList());
                }

                if (eventValue.AttributeLists.RandomAttributes != null)
                {
                    randomAttributeList = Mapper.Map<List<AttributeSet>, List<Entities.AttributeSet>>(
                        eventValue
                            .AttributeLists.RandomAttributes.ToList());
                }
            }

            var externalInfo = new Dictionary<string, string>();
            if (eventValue.ExternalInfos != null && eventValue.ExternalInfos.Value != null)
            {
                externalInfo = eventValue.ExternalInfos.Value.ToDictionary(item => item.Key, item => item.Value);
            }

            var schrodingerIndex = new SchrodingerIndex()
            {
                Id = schrodingerId,
                Tick = eventValue.Tick,
                Issuer = eventValue.Issuer.ToBase58(),
                Owner = eventValue.Owner.ToBase58(),
                Deployer = eventValue.Deployer.ToBase58(),
                TransactionId = context.Transaction.TransactionId,
                Ancestor = eventValue.Ancestor,
                TokenName = eventValue.TokenName,
                Signatory = eventValue.Signatory?.ToBase58() ?? string.Empty,
                // CollectionExternalInfo = new Dictionary<string, string>(),
                ExternalInfo = externalInfo,
                // Rule = eventValue.coll,
                AttributeSets = new Entities.AttributeSets()
                {
                    FixedAttributes = fixedAttributeList,
                    RandomAttributes = randomAttributeList,
                },
                CrossGenerationConfig =
                    Mapper.Map<CrossGenerationConfig, Entities.CrossGenerationConfig>(eventValue.CrossGenerationConfig),
                TotalSupply = eventValue.TotalSupply,
                IssueChainId = eventValue.IssueChainId,
                MaxGeneration = eventValue.MaxGeneration,
                Decimals = eventValue.Decimals,
                IsWeightEnabled = eventValue.IsWeightEnabled,
                LossRate = Convert.ToDouble(eventValue.LossRate) / 10000
            };

            await SaveEntityAsync(schrodingerIndex);
            Logger.LogDebug("[Deployed] end, id: {id}", schrodingerId);
        }
        catch (Exception e)
        {
            Logger.LogError("[Deployed] error: {err}", e.Message);
        }
    }
}