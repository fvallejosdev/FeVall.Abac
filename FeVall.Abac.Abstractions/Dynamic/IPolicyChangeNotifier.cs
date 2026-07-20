// FeVall.Abac.Abstractions/Dynamic/IPolicyChangeNotifier.cs


namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Mecanismo de invalidación de caché entre instancias del servidor (pods, réplicas).
    /// Cuando la UI guarda un cambio en el servidor A, este contrato es lo que le avisa
    /// a los servidores B y C que deben recompilar esa política.
    /// DIP: implementable con Redis Pub/Sub, PostgreSQL LISTEN/NOTIFY, RabbitMQ, etc.
    /// sin que DynamicPolicyCache conozca el detalle.
    /// </summary>
    public interface IPolicyChangeNotifier
    {
        /// <summary>Publica que una política cambió. Llamado por la capa de administración/UI.</summary>
        Task PublishInvalidationAsync(string policyId, CancellationToken ct = default);

        /// <summary>
        /// Stream de IDs de políticas invalidadas por otras instancias.
        /// Cada instancia del motor se suscribe una vez al arrancar.
        /// </summary>
        IAsyncEnumerable<string> Subscribe(CancellationToken ct = default);
    }
}
