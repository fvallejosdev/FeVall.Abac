using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeVall.Abac.Engine.Audit
{
    /// <summary>
    /// Traduce un (IEvaluationContext, Decision) a un AuditEntry serializable.
    /// SRP: separado de AbacEngine — la responsabilidad de "cómo se resume un
    /// contexto para auditoría" no es la misma que "orquestar la evaluación".
    /// No conoce el dominio del consumidor: usa AttributeBag.Snapshot() de forma
    /// genérica, nunca asume claves específicas.
    /// internal static: detalle de implementación del motor.
    /// </summary>
    internal static class AuditEntryFactory
    {
        public static AuditEntry Build(IEvaluationContext context, Decision decision) => new()
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            DecisionEffect = decision.Effect.ToString(),
            Reason = decision.Reason,
            SubjectSummary = Summarize(context.Subject),
            ResourceSummary = Summarize(context.Resource),
            ActionSummary = Summarize(context.Action),
            Obligations = decision.Obligations.Select(o => o.Id).ToArray()
        };

        // Resumen best-effort: "Clave=Valor; Clave2=Valor2". Un AttributeBag anidado
        // (ver AttributePathResolver) se muestra como marcador "{...}" en vez de
        // recursar — esto es un RESUMEN de auditoría, no una serialización completa;
        // recursar sin límite arriesgaría entradas de auditoría enormes o cíclicas.
        private static string Summarize(AttributeBag bag)
        {
            var snapshot = bag.Snapshot();
            if (snapshot.Count == 0) return "(sin atributos)";

            return string.Join("; ", snapshot.Select(kv =>
                $"{kv.Key}={(kv.Value is AttributeBag ? "{...}" : kv.Value)}"));
        }

    }
}
