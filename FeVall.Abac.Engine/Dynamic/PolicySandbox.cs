using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

// FeVall.Abac.Engine/Dynamic/PolicySandbox.cs
namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Implementación de IPolicySandbox. Deliberadamente NO usa
    /// FaultTolerantPolicyDecorator: en el sandbox el usuario quiere ver el error
    /// real (stack, mensaje técnico), no un Deny genérico fail-closed — ese
    /// comportamiento fail-closed es correcto en producción, no aquí.
    /// Genuinamente async: TestAsync/EvaluateCaseAsync usan await en toda la
    /// cadena — nunca bloquean un hilo del thread pool con .GetAwaiter().GetResult(),
    /// evitando tanto el desperdicio de paralelismo bajo carga como el riesgo de
    /// deadlock si IPolicy.EvaluateAsync deja de ser síncrono en el futuro.
    /// internal sealed: se expone únicamente vía IPolicySandbox.
    /// </summary>
    internal sealed class PolicySandbox : IPolicySandbox
    {
        private readonly IPolicyCompiler _compiler;

        public PolicySandbox(IPolicyCompiler compiler)
        {
            ArgumentNullException.ThrowIfNull(compiler);
            _compiler = compiler;
        }

        public Task<PolicyTestReport> TestAsync(
            PolicyDefinition definition,
            IReadOnlyList<IEvaluationContext> testCases,
            CancellationToken ct = default)
        {
            // Delega a la versión async real — el retorno Task<T> se conserva
            // para no romper la firma de IPolicySandbox, pero el cuerpo ya no
            // bloquea ningún hilo del pool.
            return TestAsyncCore(definition, testCases, ct);
        }

        private async Task<PolicyTestReport> TestAsyncCore(
            PolicyDefinition definition,
            IReadOnlyList<IEvaluationContext> testCases,
            CancellationToken ct)
        {
            IPolicy compiled;
            try
            {
                // Se fuerza explainOnDeny: true — el usuario probando una política en la UI
                // necesita ver el árbol completo de diagnóstico sin importar si
                // AbacEngineOptions.EnableDetailedLogging está desactivado en producción.
                compiled = _compiler.Compile(definition, explainOnDeny: true);
            }
            catch (PolicyCompilationException ex)
            {
                return new PolicyTestReport
                {
                    CompiledSuccessfully = false,
                    CompilationError = ex.Message
                };
            }

            var results = new List<PolicyTestCaseResult>(testCases.Count);
            for (var i = 0; i < testCases.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(await EvaluateCaseAsync(compiled, testCases[i], i, ct));
            }

            return new PolicyTestReport
            {
                CompiledSuccessfully = true,
                CaseResults = results
            };
        }

        private static async Task<PolicyTestCaseResult> EvaluateCaseAsync(
            IPolicy compiled, IEvaluationContext context, int index, CancellationToken ct)
        {
            try
            {
                // CompiledPolicy ya expone Trace en la Decision — lo reutilizamos tal cual.
                var decision = await compiled.EvaluateAsync(context, ct);

                return new PolicyTestCaseResult
                {
                    CaseIndex = index,
                    DecisionEffect = decision.Effect.ToString(),
                    Reason = decision.Reason,
                    Trace = decision.Trace
                };
            }
            catch (Exception ex)
            {
                // A diferencia de producción (FaultTolerantPolicyDecorator), aquí
                // el error real SÍ se expone — es justo lo que el usuario probando
                // la política necesita ver para corregirla antes de publicar.
                return new PolicyTestCaseResult
                {
                    CaseIndex = index,
                    DecisionEffect = "Error",
                    RuntimeError = $"{ex.GetType().Name}: {ex.Message}"
                };
            }
        }
    }
}