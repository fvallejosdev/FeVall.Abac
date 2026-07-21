
// FeVall.Abac.Abstractions/Dynamic/PolicyTestReport.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Resultado de probar una política en el sandbox. DTO puro (SRP).
    /// </summary>
    ///
    public sealed record PolicyTestReport
    {
        public required bool CompiledSuccessfully { get; init; }
        public string? CompilationError { get; init; }
        public IReadOnlyList<PolicyTestCaseResult> CaseResults { get; init; } = [];
    }
    public sealed record PolicyTestCaseResult
    {
        public required int CaseIndex { get; init; }
        public required string DecisionEffect { get; init; }
        public string? Reason { get; init; }
        public ConditionTrace? Trace { get; init; }

        /// <summary>
        /// Excepción de runtime capturada durante ESTE caso puntual (ej. GreaterThan
        /// contra un atributo ausente). A diferencia de producción, aquí SÍ se expone
        /// el detalle técnico completo — el usuario que prueba necesita saber
        /// exactamente qué reventó, no un mensaje genérico de fail-closed.
        /// </summary>
        public string? RuntimeError { get; init; }
    }
}
