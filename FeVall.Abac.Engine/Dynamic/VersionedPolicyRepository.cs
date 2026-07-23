// FeVall.Abac.Engine/Dynamic/VersionedPolicyRepository.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Adapta IPolicyVersionStore (historial completo) a IPolicyRepository
    /// (solo "la versión vigente ahora"), que es lo único que DynamicPolicyCache
    /// necesita consumir. El consumidor de la librería implementa ÚNICAMENTE
    /// IPolicyVersionStore — esta clase le ahorra escribir una segunda
    /// implementación de IPolicyRepository a mano.
    /// internal sealed: se registra como IPolicyRepository vía AddDynamicAbacPolicies.
    /// </summary>
    internal sealed class VersionedPolicyRepository : IPolicyRepository
    {
        private readonly IPolicyVersionStore _versionStore;

        public VersionedPolicyRepository(IPolicyVersionStore versionStore)
        {
            ArgumentNullException.ThrowIfNull(versionStore);
            _versionStore = versionStore;
        }

        public async Task<IReadOnlyList<PolicyDefinition>> GetAllAsync(CancellationToken ct = default)
        {
            var latest = await _versionStore.GetAllLatestAsync(ct);
            return latest.Select(v => v.Definition).ToArray();
        }

        public async Task<PolicyDefinition?> GetByIdAsync(string policyId, CancellationToken ct = default)
        {
            var latest = await _versionStore.GetLatestAsync(policyId, ct);
            return latest?.Definition;
        }
    }
}
