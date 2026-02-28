using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Promotions.Services;

namespace StoreAssistantPro.Modules.Promotions.Commands;

public class SaveCouponHandler(ICouponService couponService)
    : BaseCommandHandler<SaveCouponCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveCouponCommand command)
    {
        var coupon = command.Coupon;
        if (coupon.Id == 0)
            await couponService.CreateAsync(coupon);
        else
            await couponService.UpdateAsync(coupon);
        return CommandResult.Success();
    }
}

public class DeleteCouponHandler(ICouponService couponService)
    : BaseCommandHandler<DeleteCouponCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteCouponCommand command)
    {
        await couponService.DeleteAsync(command.CouponId);
        return CommandResult.Success();
    }
}

public class ApplyCouponHandler(ICouponService couponService)
    : BaseCommandHandler<ApplyCouponCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ApplyCouponCommand command)
    {
        var coupon = await couponService.ValidateAndGetAsync(command.CouponCode, command.BillAmount);
        if (coupon is null)
            return CommandResult.Failure("Invalid or expired coupon code.");
        await couponService.IncrementUsageAsync(coupon.Id);
        return CommandResult.Success();
    }
}

public class SaveVoucherHandler(IVoucherService voucherService)
    : BaseCommandHandler<SaveVoucherCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveVoucherCommand command)
    {
        var voucher = command.Voucher;
        if (voucher.Id == 0)
            await voucherService.CreateAsync(voucher);
        else
            await voucherService.UpdateAsync(voucher);
        return CommandResult.Success();
    }
}

public class DeleteVoucherHandler(IVoucherService voucherService)
    : BaseCommandHandler<DeleteVoucherCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteVoucherCommand command)
    {
        await voucherService.DeleteAsync(command.VoucherId);
        return CommandResult.Success();
    }
}

public class RedeemVoucherHandler(IVoucherService voucherService)
    : BaseCommandHandler<RedeemVoucherCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(RedeemVoucherCommand command)
    {
        var voucher = await voucherService.ValidateAndGetAsync(command.VoucherCode);
        if (voucher is null)
            return CommandResult.Failure("Invalid or expired voucher code.");
        if (voucher.Balance < command.Amount)
            return CommandResult.Failure($"Insufficient voucher balance. Available: {voucher.Balance:C}");
        await voucherService.RedeemAsync(voucher.Id, command.Amount);
        return CommandResult.Success();
    }
}
