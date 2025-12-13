using API.Services.Interfaces;
using API.Services.Realisations;

namespace API.Services;

public class DependencyRegistration
{
    public static IServiceCollection RegisterDependency(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IGenreService, GenreService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IBookSwipeService, BookSwipeService>();
        services.AddScoped<IAiService, OpenAiService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<ISupportService, SupportService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
