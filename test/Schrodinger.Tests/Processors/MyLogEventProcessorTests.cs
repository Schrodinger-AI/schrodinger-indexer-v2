using AeFinder.Sdk;
using AElf.Types;
using Forest;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Schrodinger.Entities;
using Schrodinger.GraphQL;
using Schrodinger.GraphQL.Dto;
using Schrodinger.Processors.Forest;
using Schrodinger.Processors.Provider;
using Schrodinger.Utils;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Schrodinger.Processors;

public class MyLogEventProcessorTests: SchrodingerTestBase
{
    private readonly SoldLogEventProcessor _soldLogEventProcessor;
    private readonly IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository;
    private readonly IObjectMapper _objectMapper;
    

    public MyLogEventProcessorTests()
    {
        _soldLogEventProcessor = GetRequiredService<SoldLogEventProcessor>();
        _nftActivityIndexRepository = GetRequiredService<IReadOnlyRepository<NFTActivityIndex>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }
    
    [Fact]
    public async Task TestSoldEvent()
    {
        var newValues = TraitHelper.ReplaceTraitValues(new List<string>() {"Pet", "Face"}, new List<string>() {"Flokis", "Weed"});
        const string symbol = "SGR-1";
        const string tokenSymbol = "ELF";
        const long amount = 10000000000;
        const long decimals = 8;
        const long quantity = 200000000;
        var address1 = Address.FromPublicKey("AAA".HexToByteArray());
        var address2 = Address.FromPublicKey("BBB".HexToByteArray());
        var sold = new Sold()
        {
            NftFrom = address1,
            NftTo = address2,
            NftQuantity = quantity,
            NftSymbol = symbol,
            PurchaseAmount = amount,
            PurchaseSymbol = tokenSymbol
        };
        
        var logEventContext = GenerateLogEventContext(sold);
        await _soldLogEventProcessor.ProcessAsync(sold, logEventContext);
        
        var result = await Query.GetSchrodingerSoldRecordAsync(_nftActivityIndexRepository,  new GetSchrodingerSoldRecordInput
        {
            Types = new List<int>(){3},
            SortType = "DESC",
            Address = "ELF_2YcGvyn7QPmhvrZ7aaymmb2MDYWhmAks356nV3kUwL8FkGSYeZ_tDVW",
            TimestampMin = 1620172800000
        }, _objectMapper);
        
        // Assert
        result.TotalRecordCount.ShouldBe(1);
        result.Data.Count.ShouldBe(1);
        
        // var entities = await Query.MyEntity(_repository, _objectMapper, new GetMyEntityInput
        // {
        //     ChainId = ChainId
        // });
        // entities.Count.ShouldBe(1);
    }
}