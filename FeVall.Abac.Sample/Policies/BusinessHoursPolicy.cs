// FeVall.Abac.Sample/Policies/BusinessHoursPolicy.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Sample.Policies;

/// <summary>
/// Permite el acceso solo dentro del horario laboral (8:00 - 18:00 UTC).
/// Implementa IPolicyApplicability — solo aplica a recursos de tipo "internal-report".
/// Demuestra ISP: la aplicabilidad es una interfaz separada de la evaluación.
/// </summary>
public sealed class BusinessHoursPolicy : IPolicy, IPolicyApplicability
{
    private const int StartHour = 8;
    private const int EndHour = 18;

    public string Name => nameof(BusinessHoursPolicy);

    public bool IsApplicableTo(IEvaluationContext context) =>
        context.Resource.Get<string>("type") == "internal-report";

    public Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default)
    {
        var hour = DateTime.UtcNow.Hour;

        return Task.FromResult(
            IsWithinBusinessHours(hour)
                ? Decision.PermitWith($"Acceso dentro de horario laboral (hora UTC: {hour}).")
                : Decision.DenyWith($"Acceso fuera de horario laboral (hora UTC: {hour}). Permitido entre {StartHour}:00 y {EndHour}:00."));
    }

    private static bool IsWithinBusinessHours(int hour) =>
        hour is >= StartHour and < EndHour;
}