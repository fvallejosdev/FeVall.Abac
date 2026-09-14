using Microsoft.CodeAnalysis;

namespace FeVall.Abac.Analyzers;

/// <summary>
/// Definiciones de diagnóstico del analyzer. Separado del analyzer mismo por SRP —
/// mismo criterio de un-solo-lugar-por-responsabilidad que el resto del codebase.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// ABAC0001: bloqueo síncrono sobre una operación asíncrona (.Result o
    /// .GetAwaiter().GetResult()). Ver CHANGELOG_3.md #4 — el bug concreto que
    /// este analyzer existe para que no se reintroduzca en FeVall.Abac.Engine.
    /// </summary>
    public static readonly DiagnosticDescriptor BlockingAsyncCall = new(
        id: "ABAC0001",
        title: "Uso bloqueante de código asíncrono",
        messageFormat: "'{0}' bloquea el hilo llamante sobre una operación asíncrona; usa 'await' en su lugar",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Bloquear sincrónicamente sobre un Task/ValueTask vía '.Result' o " +
            "'.GetAwaiter().GetResult()' desperdicia hilos del thread pool y puede " +
            "producir deadlock bajo un SynchronizationContext (WPF, algunos hosts " +
            "de test) si la operación subyacente deja de ser puramente síncrona. " +
            "Es el mismo patrón corregido en PolicySandbox.EvaluateCase (CHANGELOG_3.md #4).");
}