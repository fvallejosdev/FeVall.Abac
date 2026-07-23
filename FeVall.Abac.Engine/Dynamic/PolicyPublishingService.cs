// FeVall.Abac.Engine/Dynamic/PolicyPublishingService.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Único punto de entrada para que la UI publique o revierta una política.
    /// SRP: coordina compilación de validación + versionado + invalidación de caché;
    /// no implementa ninguna de las tres cosas, delega a las abstracciones correspondientes.
    /// Invariante de diseño: NUNCA se publica una PolicyDefinition que no compile —
    /// esto es más estricto que el sandbox (que permite probar cosas rotas);
    /// aquí ya estamos en el camino de producción.
    /// </summary>
    public sealed class PolicyPublishingService
    {
        private readonly IPolicyVersionStore _versionStore;
        private readonly IPolicyCompiler _compiler;
        private readonly IPolicyChangeNotifier _notifier;

        public PolicyPublishingService(
            IPolicyVersionStore versionStore,
            IPolicyCompiler compiler,
            IPolicyChangeNotifier notifier)
        {
            _versionStore = versionStore;
            _compiler = compiler;
            _notifier = notifier;
        }

        /// <summary>
        /// Publica una nueva versión. Lanza PolicyCompilationException si la definición
        /// no compila — la UI debe haber pasado por IPolicySandbox antes de llegar aquí,
        /// pero esta es la última línea de defensa antes de tocar producción.
        /// </summary>
        /// 
        public async Task<PolicyVersion> PublishAsync(
           PolicyDefinition definition,
           string? publishedBy = null,
           string? changeNote = null,
           CancellationToken ct = default)
        {
            // Fail-fast: nunca se agrega al historial algo que ni siquiera compila.
            _compiler.Compile(definition);

            var nextVersion = await ComputeNextVersionAsync(definition.PolicyId, ct);

            var version = new PolicyVersion
            {
                PolicyId = definition.PolicyId,
                Version = nextVersion,
                Definition = definition,
                PublishedAtUtc = DateTimeOffset.UtcNow,
                PublishedBy = publishedBy,
                ChangeNote = changeNote
            };

            await _versionStore.AppendAsync(version, ct);
            await _notifier.PublishInvalidationAsync(definition.PolicyId, ct);

            return version;
        }

        /// <summary>
        /// Revierte a una versión anterior. NO borra ni modifica historial —
        /// crea una versión NUEVA cuyo contenido es una copia exacta de la versión
        /// objetivo. El historial completo (incluidos los intentos fallidos que
        /// llevaron al revert) queda intacto para auditoría.
        /// </summary>
        public async Task<PolicyVersion> RevertToAsync(
              string policyId,
              int targetVersion,
              string? revertedBy = null,
              CancellationToken ct = default)
        {
            var target = await _versionStore.GetVersionAsync(policyId, targetVersion, ct)
                ?? throw new InvalidOperationException(
                    $"La política '{policyId}' no tiene una versión {targetVersion} en su historial.");

            var nextVersion = await ComputeNextVersionAsync(policyId, ct);

            var reverted = new PolicyVersion
            {
                PolicyId = policyId,
                Version = nextVersion,
                Definition = target.Definition,
                PublishedAtUtc = DateTimeOffset.UtcNow,
                PublishedBy = revertedBy,
                ChangeNote = $"Revertido a la versión {targetVersion}.",
                RevertedFromVersion = targetVersion
            };
            // No reusamos PublishAsync tal cual porque ya validamos que target.Definition
            // compiló en su momento — pero igual la revalidamos: el whitelist de operadores
            // pudo haber cambiado (ej. un operador fue removido de RegisterOperators)
            // entre la publicación original y este revert.
            _compiler.Compile(reverted.Definition);

            await _versionStore.AppendAsync(reverted, ct);
            await _notifier.PublishInvalidationAsync(policyId, ct);

            return reverted;
        }

        public Task<IReadOnlyList<PolicyVersion>> GetHistoryAsync(string policyId, CancellationToken ct = default) =>
           _versionStore.GetHistoryAsync(policyId, ct);

        private async Task<int> ComputeNextVersionAsync(string policyId, CancellationToken ct)
        {
            var latest = await _versionStore.GetLatestAsync(policyId, ct);
            return (latest?.Version ?? 0) + 1;
        }
    }
}
