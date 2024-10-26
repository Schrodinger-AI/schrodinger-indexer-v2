using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Schrodinger.Entities;
using Schrodinger.Utils;

namespace Schrodinger.Processors;

public class ConfirmVoucherProcessor : SchrodingerProcessorBase<VoucherConfirmed>
{
    public override async Task ProcessAsync(VoucherConfirmed eventValue, LogEventContext context)
    {
        Logger.LogDebug("[VoucherConfirmed] begin, id:{symbol}", eventValue.VoucherInfo.VoucherId.ToHex());
        var voucherIndexId = eventValue.VoucherInfo.VoucherId.ToHex();
        
        var voucherIndex = await GetEntityAsync<AdoptWithVoucherIndex>(voucherIndexId);
        if (voucherIndex != null)
        {
            voucherIndex.AdoptId = eventValue.VoucherInfo.AdoptId.ToHex();
            voucherIndex.UpdatedTime = DateTimeHelper.GetCurrentTimestamp();
        }
        else
        {
            voucherIndex = Mapper.Map<VoucherConfirmed, AdoptWithVoucherIndex>(eventValue);
            voucherIndex.AdoptId = eventValue.VoucherInfo.AdoptId.ToHex();
            voucherIndex.VoucherId = voucherIndexId;
            voucherIndex.Tick = eventValue.VoucherInfo.Tick;
            voucherIndex.CreatedTime = DateTimeHelper.GetCurrentTimestamp();
            voucherIndex.UpdatedTime = DateTimeHelper.GetCurrentTimestamp();
        }
        
        await SaveEntityAsync(voucherIndex);
        Logger.LogDebug("[VoucherConfirmed] finished, id:{symbol}", eventValue.VoucherInfo.VoucherId.ToHex());
    }
}