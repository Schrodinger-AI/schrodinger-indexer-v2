using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;
using SchrodingerMain;


namespace Schrodinger.Processors;

public class CollectionDeployedProcessor: SchrodingerProcessorBase<CollectionDeployed>
{
    public override async Task ProcessAsync(CollectionDeployed eventValue, LogEventContext context)
    {
        var chainId = context.ChainId;
        var symbol = eventValue.Symbol;
        Logger.LogDebug("[CollectionDeployed] start chainId:{chainId} symbol:{symbol}", chainId, symbol);
        try
        {
            var tick = TokenSymbolHelper.GetTickBySymbol(symbol);
            var schrodingerIndex = await GetEntityAsync<SchrodingerIndex>(IdGenerateHelper.GetId(chainId, tick));
            if (schrodingerIndex != null)
            {
                Logger.LogDebug("[CollectionDeployed] schrodingerIndex alreadyExisted chainId:{chainId} symbol:{symbol}", chainId, symbol);
                return;
            }

            schrodingerIndex = Mapper.Map<CollectionDeployed, SchrodingerIndex>(eventValue);
            schrodingerIndex.Id = IdGenerateHelper.GetId(chainId, TokenSymbolHelper.GetTickBySymbol(symbol));
            
            await SaveEntityAsync(schrodingerIndex);
            // Logger.LogDebug("[CollectionDeployed] end chainId:{chainId} symbol:{symbol}", chainId, symbol);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[CollectionDeployed] Exception chainId:{chainId} symbol:{symbol}", chainId, symbol);
            throw;
        }
    }
    
}