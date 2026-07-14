// FeVall.Abac.Tests/Fakes/FakePolicy.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Tests.Fakes
{
    /// <summary>
    /// Política controlable para tests.
    /// Retorna la decisión configurada al construirla — sin lógica de dominio.
    /// </summary>
    internal sealed class FakePolicy : IPolicy
    {
        private readonly Decision _decision;

        public string Name { get; }

        public FakePolicy(string name, Decision decision)
        {
            Name = name;
            _decision = decision;
        }

        public Task<Decision> EvaluateAsync(
            IEvaluationContext context,
            CancellationToken ct = default) =>
            Task.FromResult(_decision);
    }
}
