using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FeVall.Abac.Analyzers;

/// <summary>
/// Detecta bloqueo síncrono sobre Task/ValueTask: '.Result' y
/// '.GetAwaiter().GetResult()'. Usa la API de IOperation (semántica, no solo
/// sintáctica) para evitar falsos positivos sobre tipos que casualmente tengan
/// una propiedad o método con el mismo nombre pero no sean Task-like.
/// Alcance deliberado: solo Task/Task&lt;T&gt;/ValueTask/ValueTask&lt;T&gt; del BCL —
/// no intenta cubrir Task-likes custom (ej. un IAsyncEnumerable envuelto a mano),
/// que no existen hoy en este codebase.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlockingAsyncCallAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.BlockingAsyncCall);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    // Caso 1: task.Result
    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        var propertyRef = (IPropertyReferenceOperation)context.Operation;

        if (propertyRef.Property.Name != nameof(Task<int>.Result))
            return;

        if (!IsTaskLike(propertyRef.Instance?.Type))
            return;

        Report(context, propertyRef.Syntax.GetLocation(), ".Result");
    }

    // Caso 2: task.GetAwaiter().GetResult()
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != nameof(TaskAwaiter.GetResult))
            return;

        // El receptor de GetResult() debe ser, a su vez, una llamada a GetAwaiter()
        // sobre un Task-like — así se evita marcar un GetResult() de otro tipo
        // que no tenga nada que ver con async/await.
        if (invocation.Instance is not IInvocationOperation awaiterCall)
            return;

        if (awaiterCall.TargetMethod.Name != nameof(Task.GetAwaiter))
            return;

        if (!IsTaskLike(awaiterCall.Instance?.Type))
            return;

        Report(context, invocation.Syntax.GetLocation(), ".GetAwaiter().GetResult()");
    }

    private static void Report(OperationAnalysisContext context, Location location, string offendingMember) =>
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.BlockingAsyncCall,
            location,
            offendingMember));

    private static bool IsTaskLike(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        // OriginalDefinition colapsa Task<int>, Task<string>, etc. a Task<TResult> —
        // así no hace falta enumerar cada instanciación genérica posible.
        var fullName = type.OriginalDefinition.ToDisplayString();

        return fullName is "System.Threading.Tasks.Task"
            or "System.Threading.Tasks.Task<TResult>"
            or "System.Threading.Tasks.ValueTask"
            or "System.Threading.Tasks.ValueTask<TResult>";
    }
}