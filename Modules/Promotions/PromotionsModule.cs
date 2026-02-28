using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Promotions.Commands;
using StoreAssistantPro.Modules.Promotions.Services;

namespace StoreAssistantPro.Modules.Promotions;

public static class PromotionsModule
{
    public static IServiceCollection AddPromotionsModule(this IServiceCollection services)
    {
        services.AddSingleton<ICouponService, CouponService>();
        services.AddSingleton<IVoucherService, VoucherService>();
        services.AddTransient<ICommandRequestHandler<SaveCouponCommand, Unit>, SaveCouponHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteCouponCommand, Unit>, DeleteCouponHandler>();
        services.AddTransient<ICommandRequestHandler<ApplyCouponCommand, Unit>, ApplyCouponHandler>();
        services.AddTransient<ICommandRequestHandler<SaveVoucherCommand, Unit>, SaveVoucherHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteVoucherCommand, Unit>, DeleteVoucherHandler>();
        services.AddTransient<ICommandRequestHandler<RedeemVoucherCommand, Unit>, RedeemVoucherHandler>();

        return services;
    }
}
