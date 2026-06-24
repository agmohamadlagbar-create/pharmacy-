using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OnlineStore.Api.Controllers;
using OnlineStore.Api.Hubs;
using OnlineStore.Api.Services;

namespace OnlineStore.Backend.Tests;

public class ApiSurfaceTests
{
    [Fact]
    public void Controllers_AreApiControllersWithRoutes()
    {
        var controllerTypes = new[]
        {
            typeof(AdminController),
            typeof(AdsController),
            typeof(AiController),
            typeof(AuthController),
            typeof(CartController),
            typeof(CategoriesController),
            typeof(DeliveryController),
            typeof(MeController),
            typeof(NotificationsController),
            typeof(OrdersController),
            typeof(PaymentController),
            typeof(ProductsController),
            typeof(ShopsController)
        };

        foreach (var controllerType in controllerTypes)
        {
            Assert.True(typeof(ControllerBase).IsAssignableFrom(controllerType));
            Assert.NotNull(Attribute.GetCustomAttribute(controllerType, typeof(ApiControllerAttribute)));
            Assert.NotNull(Attribute.GetCustomAttribute(controllerType, typeof(RouteAttribute)));
        }
    }

    [Fact]
    public void SignalRHubs_InheritFromHub()
    {
        var hubTypes = new[]
        {
            typeof(AiChatHub),
            typeof(NotificationHub),
            typeof(TrackingHub)
        };

        foreach (var hubType in hubTypes)
        {
            Assert.True(typeof(Hub).IsAssignableFrom(hubType));
        }
    }

    [Fact]
    public void CoreServices_ImplementTheirPublicContracts()
    {
        Assert.True(typeof(IAiOrchestratorService).IsAssignableFrom(typeof(AiOrchestratorService)));
        Assert.True(typeof(ICacheService).IsAssignableFrom(typeof(LocalCacheService)));
        Assert.True(typeof(ICartService).IsAssignableFrom(typeof(CartService)));
        Assert.True(typeof(IChatHistoryService).IsAssignableFrom(typeof(ChatHistoryService)));
        Assert.True(typeof(ICustomerSupportService).IsAssignableFrom(typeof(CustomerSupportService)));
        Assert.True(typeof(IEmailService).IsAssignableFrom(typeof(SmtpEmailService)));
        Assert.True(typeof(IFileStorageService).IsAssignableFrom(typeof(FileStorageService)));
        Assert.True(typeof(IGroqAiService).IsAssignableFrom(typeof(GroqAiService)));
        Assert.True(typeof(IImageProcessingQueue).IsAssignableFrom(typeof(ImageProcessingQueue)));
        Assert.True(typeof(IJwtTokenService).IsAssignableFrom(typeof(JwtTokenService)));
        Assert.True(typeof(INotificationService).IsAssignableFrom(typeof(NotificationService)));
        Assert.True(typeof(IProductSearchService).IsAssignableFrom(typeof(ProductSearchService)));
        Assert.True(typeof(IRecommendationService).IsAssignableFrom(typeof(RecommendationService)));
        Assert.True(typeof(IStoreCacheService).IsAssignableFrom(typeof(StoreCacheService)));
        Assert.True(typeof(IStoreSelectionService).IsAssignableFrom(typeof(StoreSelectionService)));
        Assert.True(typeof(IVerificationCodeStore).IsAssignableFrom(typeof(InMemoryVerificationCodeStore)));
    }
}
