// FeVall.Abac.Abstractions/Dynamic/PolicyVersionConflictException.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Señala que IPolicyVersionStore.AppendAsync detectó una colisión de
    /// (PolicyId, Version) — dos publicaciones/reverts concurrentes intentaron
    /// escribir la misma versión. Toda implementación de IPolicyVersionStore
    /// DEBE lanzar esta excepción (o dejar que el constraint único subyacente
    /// la produzca, envuelta) ante ese escenario, nunca sobreescribir en
    /// silencio ni descartar la escritura sin avisar — el historial es
    /// append-only por diseño (ver PolicyVersion), así que una colisión nunca
    /// debe resolverse "el último que escribe gana" de forma silenciosa.
    /// </summary>
    public sealed class PolicyVersionConflictException : Exception
    {
        public string PolicyId { get; }
        public int Version { get; }

        public PolicyVersionConflictException(string policyId, int version)
            : base($"Ya existe una versión {version} para la política '{policyId}'. " +
                   "Probablemente otra publicación concurrente escribió esa versión primero.")
        {
            PolicyId = policyId;
            Version = version;
        }

        public PolicyVersionConflictException(string policyId, int version, Exception inner)
            : base($"Ya existe una versión {version} para la política '{policyId}'. " +
                   "Probablemente otra publicación concurrente escribió esa versión primero.", inner)
        {
            PolicyId = policyId;
            Version = version;
        }
    }
}
