using AeFinder.Sdk.Processor;
using Schrodinger.GraphQL;
using Schrodinger.Processors;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Schrodinger.Indexer.Plugin.Processors;
using Schrodinger.Processors.Forest;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Schrodinger;

public class SchrodingerModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SchrodingerModule>(); });
        context.Services.AddSingleton<ISchema, AppSchema>();
        
        // Add your LogEventProcessor implementation.
        context.Services.AddTransient<ILogEventProcessor, AdoptionRerolledProcessor>();
        context.Services.AddTransient<ILogEventProcessor, BurnedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, CollectionDeployedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, ConfirmedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, CrossChainReceivedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, DeployedLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, FixedAttributesSetLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, IssuedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, MaxGenerationSetLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, RandomAttributesSetLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, RerolledProcessor>();
        context.Services.AddTransient<ILogEventProcessor, SchrodingerAdoptProcessor>();
        context.Services.AddTransient<ILogEventProcessor, TokenCreatedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, TransferredProcessor>();
        context.Services.AddTransient<ILogEventProcessor, SoldLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, AdoptionUpdatedProcessor>();
        context.Services.AddTransient<ILogEventProcessor, SpinProcessor>();
        context.Services.AddTransient<ILogEventProcessor, SpinRewardConfigProcessor>();
        context.Services.AddTransient<ILogEventProcessor, AdoptedWithVoucherProcessor>();
        context.Services.AddTransient<ILogEventProcessor, ConfirmVoucherProcessor>();
        context.Services.AddTransient<ILogEventProcessor, BredProcessor>();
    }
}