// FeVall.Abac.Engine/Extensions/DynamicAbacEngineExtensions.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Audit;
using FeVall.Abac.Abstractions.Dynamic;
using FeVall.Abac.Engine.Audit;
using FeVall.Abac.Engine; // NullGuardPolicyEvaluator (internal, mismo ensamblado)
using FeVall.Abac.Engine.Dynamic;
using FeVall.Abac.Engine.Dynamic.Operators;
using Microsoft.Extensions.DependencyInjection;

namespace FeVall.Abac.Engine.Extensions
{

    /// <summary>
    /// Punto de entrada único para habilitar políticas creadas por el usuario en la UI.
    /// Se llama DESPUÉS de AddAbacEngine(...) — complementa el motor existente,
    /// no lo reemplaza. El consumidor debe registrar además su propia implementación
    /// de IPolicyRepository, IPolicyChangeNotifier e IAuditBatchWriter (specíficas
    /// de su infraestructura: qué base de datos, qué bus de mensajería, qué almacén de logs).
    /// </summary>
    public static class DynamicAbacEngineExtensions
    {
        /// <summary>
        /// Registra el compilador, la lista blanca de operadores, la caché dinámica
        /// y el evaluador con cortocircuito. Requiere que IPolicyRepository e
        /// IPolicyChangeNotifier ya estén registrados por el consumidor.
        /// </summary>
        public static IServiceCollection AddDynamicAbacPolicies(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            RegisterOperators(services);
            services.AddSingleton<IOperatorRegistry, OperatorRegistry>();
            services.AddSingleton<IPolicyCompiler, JsonPolicyCompiler>();
            services.AddScoped<IPolicySandbox, PolicySandbox>();   // ← nuevo, scoped porque no cachea nada

            // Singleton: la caché y su suscripción de fondo viven durante toda la vida de la app.
            services.AddSingleton<DynamicPolicyCache>();
            services.AddSingleton<IPolicyProvider>(sp => sp.GetRequiredService<DynamicPolicyCache>());

            // Sustituye el registro de IPolicyEvaluator hecho por AddAbacEngine: en
            // Microsoft.Extensions.DependencyInjection, la última registración de una
            // interfaz es la que se resuelve por inyección de constructor, así que este
            // registro posterior reemplaza al PolicyEvaluator/NullGuardPolicyEvaluator
            // por defecto sin tener que tocar AbacEngineExtensions.
            services.AddScoped<ShortCircuitPolicyEvaluator>();
            services.AddScoped<IPolicyEvaluator>(sp =>
                new NullGuardPolicyEvaluator(sp.GetRequiredService<ShortCircuitPolicyEvaluator>()));

            return services;
        }

        /// <summary>Habilita la cola de auditoría no bloqueante. Requiere IAuditBatchWriter registrado.</summary>
        public static IServiceCollection AddAsyncAuditLog(this IServiceCollection services, int channelCapacity = 10_000)
        {
            services.AddSingleton(new ChannelAuditSink(channelCapacity));
            services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<ChannelAuditSink>());
            services.AddHostedService<AuditPersistenceWorker>();
            return services;
        }

        private static void RegisterOperators(IServiceCollection services)
        {
            // OCP en acción: agregar un operador nuevo mañana es una línea aquí,
            // nunca tocar JsonPolicyCompiler ni OperatorRegistry.
            services.AddSingleton<IComparisonOperator, EqualsOperator>();
            services.AddSingleton<IComparisonOperator, NotEqualsOperator>();
            services.AddSingleton<IComparisonOperator, GreaterThanOperator>();
            services.AddSingleton<IComparisonOperator, LessThanOperator>();
            services.AddSingleton<IComparisonOperator, InOperator>();
            services.AddSingleton<IComparisonOperator, NotInOperator>();
            services.AddSingleton<IComparisonOperator, ContainsAttributeOperator>();
            services.AddSingleton<IComparisonOperator, NotContainsAttributeOperator>();
        }
    }
}
