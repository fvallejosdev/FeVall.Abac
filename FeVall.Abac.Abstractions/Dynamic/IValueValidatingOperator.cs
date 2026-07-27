using System;
using System.Collections.Generic;
using System.Text;
// FeVall.Abac.Abstractions/Dynamic/IValueValidatingOperator.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Capacidad opcional de un IComparisonOperator para validar la FORMA de su
    /// Value ya normalizado (conteo de elementos, rango coherente, parseabilidad)
    /// en tiempo de COMPILACIÓN, no en cada Evaluate(). ISP: separada de
    /// IComparisonOperator porque la mayoría de los operadores (Equals, GreaterThan,
    /// In...) no tienen forma estructural que validar más allá de "Value no es null"
    /// (ya cubierto por ValueRequiredOperators en JsonPolicyCompiler). Solo los
    /// operadores de rango (Between, NotBetween, DateBetween) la implementan.
    /// </summary>
    public interface IValueValidatingOperator : IComparisonOperator
    {
        /// <summary>
        /// Valida la forma del Value ya normalizado (tipos CLR, no JsonElement).
        /// Lanza PolicyCompilationException-compatible (cualquier Exception —
        /// JsonPolicyCompiler la envuelve) si la forma es inválida. No debe
        /// lanzar por atributos de contexto ausentes: eso sigue siendo
        /// responsabilidad exclusiva de Evaluate() en tiempo de evaluación.
        /// </summary>
        void ValidateValueShape(object? normalizedValue);
    }
}
