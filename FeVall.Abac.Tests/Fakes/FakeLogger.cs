using FeVall.Abac.Abstractions;


namespace FeVall.Abac.Tests.Fakes
{
    /// <summary>
    /// Logger que no hace nada — elimina ruido en los tests.
    /// SRP: los tests no deben fallar por el logging.
    /// </summary>
    internal sealed class FakeLogger : IAbacLogger
    {
        public void LogEvaluationStarted(IEvaluationContext context) { }
        public void LogDecisionReached(IEvaluationContext context, Decision decision) { }
        public void LogPolicyEvaluated(IPolicy policy, Decision decision) { }
        public void LogPolicySkipped(IPolicy policy, IEvaluationContext context) { }
    }
}
