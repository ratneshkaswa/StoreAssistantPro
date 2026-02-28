using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Promotions.Commands;

public sealed record SaveCouponCommand(Coupon Coupon) : ICommandRequest<Unit>;
public sealed record DeleteCouponCommand(int CouponId) : ICommandRequest<Unit>;
public sealed record ApplyCouponCommand(string CouponCode, decimal BillAmount) : ICommandRequest<Unit>;
public sealed record SaveVoucherCommand(Voucher Voucher) : ICommandRequest<Unit>;
public sealed record DeleteVoucherCommand(int VoucherId) : ICommandRequest<Unit>;
public sealed record RedeemVoucherCommand(string VoucherCode, decimal Amount) : ICommandRequest<Unit>;
