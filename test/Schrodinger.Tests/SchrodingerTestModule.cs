using AeFinder.App.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Schrodinger.Processors;
using Schrodinger.Processors.Forest;
using Volo.Abp.Modularity;

namespace Schrodinger;

[DependsOn(
    typeof(AeFinderAppTestBaseModule),
    typeof(SchrodingerModule))]
public class SchrodingerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AeFinderAppEntityOptions>(options => { options.AddTypes<SchrodingerModule>(); });
        
        // Add your Processors.
        context.Services.AddSingleton<SoldLogEventProcessor>();
    }
}