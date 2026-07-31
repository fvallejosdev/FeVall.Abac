// FeVall.Abac.Abstractions/Dynamic/IPolicyVersionStore.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Almacén append-only del historial de versiones de políticas.
    /// DIP: el motor no sabe si esto es una tabla SQL con columna Version,
    /// un event store, o documentos versionados en Mongo.
    /// Ninguna operación de esta interfaz permite editar o borrar una versión
    /// ya existente — solo agregar (AppendAsync) y leer.
    /// </summary>
    public interface IPolicyVersionStore
    {
        /// <summary>
        /// Agrega una nueva versión al historial. Nunca sobreescribe una existente.
        /// CONTRATO DE CONCURRENCIA: la implementación DEBE garantizar unicidad de
        /// (PolicyId, Version) — típicamente vía un constraint único a nivel de
        /// almacenamiento (SQL UNIQUE INDEX, condición de escritura en Mongo, etc.).
        /// Ante una colisión (dos llamadas concurrentes con el mismo PolicyId+Version),
        /// DEBE lanzar PolicyVersionConflictException. PolicyPublishingService depende
        /// de esta excepción específica para reintentar automáticamente — una
        /// implementación que trague la colisión o lance una excepción genérica
        /// rompe ese mecanismo de reintento.
        /// </summary>
        Task AppendAsync(PolicyVersion version, CancellationToken ct = default);

        /// <summary>Historial completo de una política, ordenado por Version descendente (más reciente primero).</summary>
        Task<IReadOnlyList<PolicyVersion>> GetHistoryAsync(string policyId, CancellationToken ct = default);

        /// <summary>La versión más reciente de una política, o null si nunca se publicó.</summary>
        Task<PolicyVersion?> GetLatestAsync(string policyId, CancellationToken ct = default);

        /// <summary>Una versión específica, o null si no existe.</summary>
        Task<PolicyVersion?> GetVersionAsync(string policyId, int version, CancellationToken ct = default);

        /// <summary>La versión más reciente de CADA política — usado para reconstruir el estado "actual" completo.</summary>
        Task<IReadOnlyList<PolicyVersion>> GetAllLatestAsync(CancellationToken ct = default);
    }
}
