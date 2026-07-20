// FeVall.Abac.Abstractions/Dynamic/IPolicyProvider.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Fuente unificada de políticas para PolicyEvaluator.
    /// Este es el ÚNICO cambio requerido sobre el motor existente: PolicyEvaluator
    /// pasa de recibir "IEnumerable&lt;IPolicy&gt; policies" (fijo, resuelto una vez por DI)
    /// a recibir "IPolicyProvider" (puede variar en cada llamada — políticas dinámicas
    /// recién publicadas por la UI aparecen sin reiniciar la aplicación).
    /// SRP: la única responsabilidad es "dame las políticas vigentes ahora mismo".
    /// </summary>
    public interface IPolicyProvider
    {
        Task<IReadOnlyList<IPolicy>> GetPoliciesAsync(CancellationToken ct = default);
    }
}
