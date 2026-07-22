// FeVall.Abac.Abstractions/AttributeBag.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Contenedor de atributos tipados para un participante de la evaluación.
/// Evita Dictionary&lt;string, object&gt; crudo — el compilador atrapa errores de tipo.
/// Anidamiento: para modelar estructura jerárquica (ej. Resource.Owner.Department),
/// almacena otro AttributeBag como valor bajo una clave (Set("Owner", ownerBag)).
/// AttributePathResolver camina estos niveles sin reflexión — cada segmento
/// intermedio DEBE ser un AttributeBag, o la ruta se trata como atributo ausente.
/// </summary>
public sealed class AttributeBag
{
    private readonly Dictionary<string, object> _attributes = [];
    public AttributeBag(int capacity = 0) => _attributes = new Dictionary<string, object>(capacity);
    /// <summary>
    /// Almacena un atributo. Sobreescribe si la clave ya existe.
    /// </summary>
    public AttributeBag Set<T>(string key, T value) where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributes[key] = value;
        return this; // fluent — permite encadenar Set() sin romper SRP
    }

    /// <summary>
    /// Retorna el valor si existe y es del tipo esperado. Nunca lanza.
    /// </summary>
    public T? Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _attributes.TryGetValue(key, out var value) && value is T typed
            ? typed
            : default;
    }

    /// <summary>
    /// Retorna el valor o lanza si no existe o el tipo no coincide.
    /// Usar cuando la ausencia del atributo es un error de programación.
    /// </summary>
    public T GetRequired<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_attributes.TryGetValue(key, out var value))
            throw new KeyNotFoundException(
                $"Attribute '{key}' not found in bag.");

        if (value is not T typed)
            throw new InvalidCastException(
                $"Attribute '{key}' exists but is '{value.GetType().Name}', expected '{typeof(T).Name}'.");

        return typed;
    }

    /// <summary>
    /// Indica si el atributo existe, independientemente del tipo.
    /// </summary>
    public bool Has(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _attributes.ContainsKey(key);
    }

    /// <summary>
    /// Claves actualmente almacenadas. Útil para logging y diagnóstico.
    /// </summary>
    public IReadOnlyCollection<string> Keys => _attributes.Keys;
}