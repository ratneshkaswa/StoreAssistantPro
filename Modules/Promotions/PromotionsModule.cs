using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Promotions.Commands;
using StoreAssistantPro.Modules.Promotions.Services;
using StoreAssistantPro.Modules.Promotions.ViewModels;

namespace StoreAssistantPro.Modules.Promotions;

public static class PromotionsModule
{
    public const string PromotionsPage = "Promotions";

    public static IServiceCollection AddPromotionsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<PromotionsViewModel>(PromotionsPage);

        services.AddSingleton<ICouponService, CouponService>();
        services.AddSingleton<IVoucherService, VoucherService>();
        services.AddTransient<ICommandRequestHandler<SaveCouponCommand, Unit>, SaveCouponHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteCouponCommand, Unit>, DeleteCouponHandler>();
        services.AddTransient<ICommandRequestHandler<ApplyCouponCommand, Unit>, ApplyCouponHandler>();
        services.AddTransient<ICommandRequestHandler<SaveVoucherCommand, Unit>, SaveVoucherHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteVoucherCommand, Unit>, DeleteVoucherHandler>();
        services.AddTransient<ICommandRequestHandler<RedeemVoucherCommand, Unit>, RedeemVoucherHandler>();
        services.AddTransient<PromotionsViewModel>();

        return services;
    }
}
