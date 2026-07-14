// FeVall.Abac.Sample/Policies/DocumentOwnerPolicy.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Sample.Policies;

/// <summary>
/// Permite el acceso solo si el usuario es el dueño del documento.
/// Demuestra una política simple basada en atributos de Subject y Resource.
/// </summary>
public sealed class DocumentOwnerPolicy : IPolicy
{
    public string Name => nameof(DocumentOwnerPolicy);

    public Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default)
    {
        var userId = context.Subject.Get<string>("userId");
        var ownerId = context.Resource.Get<string>("ownerId");

        if (userId is null || ownerId is null)
            return Task.FromResult(
                Decision.DenyWith("userId u ownerId no están presentes en el contexto."));

        return Task.FromResult(
            userId == ownerId
                ? Decision.PermitWith($"Usuario '{userId}' es dueño del documento.")
                : Decision.DenyWith($"Usuario '{userId}' no es dueño del documento."));
    }
}