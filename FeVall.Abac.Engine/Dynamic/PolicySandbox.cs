using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;
using System;
using System.Collections.Generic;
using System.Text;
// FeVall.Abac.Engine/Dynamic/PolicySandbox.cs
namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Implementación de IPolicySandbox. Deliberadamente NO usa
    /// FaultTolerantPolicyDecorator: en el sandbox el usuario quiere ver el error
    /// real (stack, mensaje técnico), no un Deny genérico fail-closed — ese
    /// comportamiento fail-closed es correcto en producción, no aquí.
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
            IPolicy compiled;
            try
            {
                compiled = _compiler.Compile(definition);
            }
            catch (PolicyCompilationException ex)
            {
                return Task.FromResult(new PolicyTestReport
                {
                    CompiledSuccessfully = false,
                    CompilationError = ex.Message
                });
            }

            var results = new List<PolicyTestCaseResult>(testCases.Count);
            for (var i = 0; i < testCases.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(EvaluateCase(compiled, testCases[i], i, ct));
            }

            return Task.FromResult(new PolicyTestReport
            {
                CompiledSuccessfully = true,
                CaseResults = results
            });
        }

        private static PolicyTestCaseResult EvaluateCase(
           IPolicy compiled, IEvaluationContext context, int index, CancellationToken ct)
        {
            try
            {
                // CompiledPolicy ya expone Trace en la Decision (ver #1) — lo reutilizamos tal cual.
                var decision = compiled.EvaluateAsync(context, ct).GetAwaiter().GetResult();

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
