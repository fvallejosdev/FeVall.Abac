// FeVall.Abac.Engine/Extensions/AbacEngineExtensions.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Engine.Logging;
using FeVall.Abac.Engine.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace FeVall.Abac.Engine.Extensions;

/// <summary>
/// Punto de entrada para registrar el motor ABAC en el contenedor de DI.
/// SRP: su única responsabilidad es componer y registrar las dependencias del motor.
/// El consumidor llama AddAbacEngine() — no sabe nada de las clases internas.
/// </summary>
public static class AbacEngineExtensions
{
    /// <summary>
    /// Registra el motor ABAC y todas sus dependencias internas en el contenedor.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="configure">Configuración opcional del motor.</param>
    /// <returns>La misma colección para encadenar registros.</returns>
    public static IServiceCollection AddAbacEngine(
        this IServiceCollection services,
        Action<AbacEngineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = BuildOptions(configure);

        services.AddSingleton(options);

        RegisterLogger(services);
        RegisterStrategy(services, options);
        RegisterEvaluator(services);
        RegisterEngine(services);

        return services;
    }

    // Clean Code: cada registro en su propio método con nombre claro.
    // AddAbacEngine no mezcla niveles de abstracción — delega el detalle.

    private static AbacEngineOptions BuildOptions(Action<AbacEngineOptions>? configure)
    {
        var options = new AbacEngineOptions();
        configure?.Invoke(options);
        return options;
    }

    private static void RegisterLogger(IServiceCollection services) =>
        services.AddSingleton<IAbacLogger, AbacConsoleLogger>();

    private static void RegisterStrategy(
        IServiceCollection services,
        AbacEngineOptions options)
    {
        // Si el consumidor ya registró una ICombinationStrategy personalizada,
        // respetamos su elección y no sobreescribimos.
        if (services.Any(s => s.ServiceType == typeof(ICombinationStrategy)))
            return;

        var strategyType = ResolveStrategyType(options.CombinationStrategy);
        services.AddSingleton(typeof(ICombinationStrategy), strategyType);
    }

    private static Type ResolveStrategyType(CombinationStrategy strategy) =>
        strategy switch
        {
            CombinationStrategy.DenyOverrides => typeof(DenyOverridesStrategy),
            CombinationStrategy.PermitUnlessDeny => typeof(PermitUnlessDenyStrategy),
            _ => throw new ArgumentOutOfRangeException(
                     nameof(strategy),
                     $"Estrategia no reconocida: {strategy}")
        };

    private static void RegisterEvaluator(IServiceCollection services)
    {
        // PolicyEvaluator es el evaluador real — registrado como tipo concreto
        // para que NullGuardPolicyEvaluator pueda resolverlo explícitamente.
        services.AddScoped<PolicyEvaluator>();

        // NullGuardPolicyEvaluator decora a PolicyEvaluator.
        // Cuando alguien pida IPolicyEvaluator, recibe el decorator.
        services.AddScoped<IPolicyEvaluator>(sp =>
            new NullGuardPolicyEvaluator(
                sp.GetRequiredService<PolicyEvaluator>()));
    }

    private static void RegisterEngine(IServiceCollection services) =>
        services.AddScoped<IAbacEngine, AbacEngine>();
}