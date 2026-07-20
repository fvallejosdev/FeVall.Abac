
// FeVall.Abac.Abstractions/Audit/AuditEntry.cs
namespace FeVall.Abac.Abstractions.Audit
{
    /// <summary>
    /// Registro de auditoría de una decisión ABAC. Inmutable — se escribe una vez
    /// y se persiste tal cual, nunca se modifica (requisito típico de HIPAA/PCI-DSS).
    /// </summary>
    public sealed record AuditEntry
    {
        public required DateTimeOffset OccurredAtUtc { get; init; }
        public required string DecisionEffect { get; init; }
        public string? Reason { get; init; }
        public required string SubjectSummary { get; init; }
        public required string ResourceSummary { get; init; }
        public required string ActionSummary { get; init; }
        public IReadOnlyList<string> Obligations { get; init; } = [];
    }

}
