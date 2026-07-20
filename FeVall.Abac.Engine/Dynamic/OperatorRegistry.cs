// FeVall.Abac.Engine/Dynamic/OperatorRegistry.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Lista blanca real de operadores: se construye a partir de todos los
    /// IComparisonOperator registrados en DI. Un operador que el JSON pida pero
    /// que no esté aquí NUNCA se ejecuta — la política simplemente no compila.
    /// internal sealed: detalle de implementación del motor.
    /// </summary>
    internal sealed class OperatorRegistry : IOperatorRegistry
    {
        private readonly Dictionary<string, IComparisonOperator> _operators;

        public OperatorRegistry(IEnumerable<IComparisonOperator> operators)
        {
            ArgumentNullException.ThrowIfNull(operators);
            _operators = operators.ToDictionary(op => op.Name, StringComparer.Ordinal);
        }

        public IComparisonOperator Resolve(string operatorName)
        {
            if (_operators.TryGetValue(operatorName, out var resolved))
                return resolved;

            throw new PolicyCompilationException(
                $"Operador '{operatorName}' no está en la lista blanca. " +
                $"Operadores permitidos: {string.Join(", ", _operators.Keys)}.");
        }
    }
}