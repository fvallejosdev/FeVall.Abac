// FeVall.Abac.Abstractions/Dynamic/PolicyVersion.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Una entrada inmutable del historial de una política. Nunca se edita tras
    /// crearse — publicar SIEMPRE agrega una versión nueva (append-only), igual
    /// filosofía que AuditEntry. Un revert no "restaura" esta instancia:
    /// crea una PolicyVersion NUEVA cuyo Definition es una copia de una anterior.
    /// </summary>
    public sealed record PolicyVersion
    {
        public required string PolicyId { get; init; }
        public required int Version { get; init; }
        public required PolicyDefinition Definition { get; init; }
        public required DateTimeOffset PublishedAtUtc { get; init; }
        public string? PublishedBy { get; init; }

        /// <summary>Nota libre del autor. En un revert, se autogenera si no se provee.</summary>
        public string? ChangeNote { get; init; }

        /// <summary>Si esta versión es resultado de un revert, el número de la versión revertida.</summary>
        public int? RevertedFromVersion { get; init; }
    }
}
