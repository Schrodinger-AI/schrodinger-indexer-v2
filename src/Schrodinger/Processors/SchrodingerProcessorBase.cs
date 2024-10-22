using AeFinder.Sdk.Processor;
using AElf.CSharp.Core;
using AutoMapper;
// using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace Schrodinger.Processors;

public abstract class SchrodingerProcessorBase<TEvent> : LogEventProcessorBase<TEvent>
    where TEvent : IEvent<TEvent>,new()
{
    protected readonly IMapper Mapper;
    // IAbpLazyServiceProvider lazyServiceProvider { get; set; }
    // protected IObjectMapper ObjectMapper => LazyServiceProvider.LazyGetService<IObjectMapper>();
    
    protected SchrodingerProcessorBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SchrodingerProfile>();
        });
        Mapper = config.CreateMapper();
    }
    
    public override string GetContractAddress(string chainId)
    {
        return chainId switch
        {
            "AELF" => "Qx3QMZPstem3UHU6qjc1PsufaJoJcKj2kC2sCEnzsqCjAJ3At",
            "tDVV" => "24o1XG3ryAB7wnchtPGzar7GWw68mhD1UEW7KGKxyE3tQUb7TT",
            "tDVW" => "Ccc5pNs71BMbgDr2ZwpNqtegfkHkBsTJ57HBZ6gw3HNH6pb9S",
            _ => string.Empty
        };
    }
}