using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingList.Application.Parsing;
using ShoppingList.Infrastructure.Ocr;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddOptions<TesseractOptions>()
            .Bind(configuration.GetSection(TesseractOptions.SectionName));
        services.AddSingleton<IIngredientParserService, TesseractIngredientParserService>();

        return services;
    }
}
