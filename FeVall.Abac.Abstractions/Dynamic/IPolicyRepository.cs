// FeVall.Abac.Abstractions/Dynamic/IPolicyRepository.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Fuente de verdad de las definiciones de política creadas por el usuario.
    /// DIP: el motor nunca sabe si esto es SQL Server, PostgreSQL, Mongo o un archivo.
    /// La UI de administración escribe aquí; el motor solo lee.
    /// </summary>
    public interface IPolicyRepository
    {
        Task<IReadOnlyList<PolicyDefinition>> GetAllAsync(CancellationToken ct = default);

        Task<PolicyDefinition?> GetByIdAsync(string policyId, CancellationToken ct = default);
    }
}
