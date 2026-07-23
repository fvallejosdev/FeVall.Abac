# FeVall.Abac — Estado del Proyecto y Mapeo de Trabajo

**Última actualización:** Julio 2026
**Alcance de este documento:** consolidar objetivo, arquitectura, estado actual, cambios realizados y pendientes de la extensión de políticas dinámicas de `FeVall.Abac`. No incluye código — solo el mapeo de decisiones y su justificación.

---

## 1. Objetivo del proyecto

Construir `FeVall.Abac` como una **librería base de control de acceso por atributos (ABAC)**, reutilizable en múltiples proyectos personales, siguiendo principios SOLID y Clean Code de forma estricta.

El objetivo específico de este ciclo de trabajo fue extender la arquitectura original (estática, políticas definidas en código) para soportar **políticas dinámicas creadas por usuarios finales desde una UI de administración**, sin comprometer:

- La separación de capas `Abstractions` / `Engine` (no negociable).
- Los principios SOLID ya aplicados en el diseño original.
- La seguridad del sistema (una política creada por un usuario no técnico no debe poder ejecutar código arbitrario ni tumbar el sistema).

La motivación de fondo: el motor de políticas ABAC era, según lo planteado al inicio, el **cuello de botella recurrente** en proyectos anteriores — se buscaba resolverlo de una vez de forma robusta y flexible.

---

## 2. Estado actual de la arquitectura

### 2.1 Estructura de capas (sin cambios en su filosofía)

```
FeVall.Abac.Abstractions   → contratos, DTOs, sin lógica de infraestructura
FeVall.Abac.Engine         → implementaciones concretas, internal por defecto
FeVall.Infrastructure      → a cargo del consumidor (persistencia, servicios)
FeVall.Api                 → a cargo del consumidor
```

### 2.2 Flujo de compilación y evaluación de una política dinámica

```
PolicyDefinition (JSON de la UI)
        │
        ▼
JsonPolicyCompiler.Compile()  ──valida──► IOperatorRegistry (lista blanca)
        │                                  AttributePathResolver (sanea rutas, soporta anidamiento)
        ▼
IConditionNode / IExplainableConditionNode (árbol Composite, con trazabilidad)
        │
        ▼
CompiledPolicy : IPolicy, IPolicyApplicability
        │
        ▼
FaultTolerantPolicyDecorator (fail-closed ante cualquier excepción)
        │
        ▼
DynamicPolicyCache : IPolicyProvider  ◄── IPolicyChangeNotifier (multi-pod)
        │
        ▼
ShortCircuitPolicyEvaluator : IPolicyEvaluator
        │
        ▼
AbacEngine
```

### 2.3 Camino paralelo: publicación y control de calidad (nuevo en este ciclo)

```
PolicyDefinition (borrador en la UI)
        │
        ▼
IPolicySandbox.TestAsync()  → compila + evalúa contra casos de prueba
        │                       (nunca toca IPolicyRepository ni el caché)
        ▼
   [usuario revisa ConditionTrace / errores antes de publicar]
        │
        ▼
PolicyPublishingService.PublishAsync()
        │
        ▼
IPolicyVersionStore.AppendAsync()  (historial append-only)
        │
        ▼
IPolicyChangeNotifier.PublishInvalidationAsync()  (multi-pod)
```

### 2.4 Principios SOLID aplicados — vigentes y reforzados

| Principio | Cómo se sostiene tras los cambios |
|---|---|
| **SRP** | Cada pieza nueva (sandbox, versionado, operadores, trace) es una clase con una sola razón de cambio. `PolicyPublishingService` coordina, no implementa. |
| **OCP** | Un operador nuevo sigue siendo una clase + una línea de registro. La trazabilidad y el versionado se agregaron sin modificar `JsonPolicyCompiler` en su forma, solo se extendió. |
| **LSP** | `IExplainableConditionNode` extiende `IConditionNode` sin romper sustituibilidad — un nodo que no la implementa sigue siendo válido (fallback en `CompositeConditionNode.ExplainChild`). |
| **ISP** | `IPolicyVersionStore` es una interfaz separada de `IPolicyRepository` — el consumidor no se ve forzado a implementar versionado si no lo necesita. `IExplainableConditionNode` es opcional sobre `IConditionNode`. |
| **DIP** | `VersionedPolicyRepository` adapta `IPolicyVersionStore` a `IPolicyRepository` sin que `DynamicPolicyCache` sepa que existe versionado. |

---

## 3. Estado actual del proyecto (compilación y calidad)

### 3.1 Errores de compilación resueltos

| # | Problema | Causa raíz | Estado |
|---|---|---|---|
| 1 | Múltiples errores `CS0103`/`CS1061` por métodos LINQ no reconocidos | Faltaba `using System.Linq;` en 10 archivos (namespaces anidados no heredan `using` de sus padres) | ✅ Resuelto |
| 2 | Tipos no reconocidos en `Dynamic/Conditions/` | Faltaba `using FeVall.Abac.Engine.Dynamic;` en `AttributeConditionNode.cs` y `CompositeConditionNode.cs` (sub-namespace no da visibilidad automática al padre) | ✅ Resuelto |
| 3 | `CS0051` — Incoherencia de accesibilidad en `AuditPersistenceWorker` | Clase `public` con parámetro de constructor `ChannelAuditSink` (`internal`) | ✅ Resuelto — `AuditPersistenceWorker` bajado a `internal` |
| 4 | Errores de patrón `not`/`or` con declaración de variable en `BetweenOperator`/`DateBetweenOperator` | C# no permite declarar variable de patrón dentro de una rama de `or` combinado | ✅ Resuelto — condiciones separadas explícitamente |
| 5 | `_obligations no existe en el contexto actual` en `CompiledPolicy` | Método `CollectObligations` obsoleto, remanente del diseño previo a la separación Permit/Deny precalculada | ✅ Resuelto — método eliminado |

### 3.2 Bug lógico silencioso corregido

**`GreaterThanOperator.ToDouble` / `LessThanOperator`** — `Convert.ToDouble(null)` no lanzaba excepción, devolvía `0.0` silenciosamente. Esto rompía el principio de **fail-closed** documentado: una política `LessThan` con atributo ausente daba `0 < X` → `true` → **Permit indebido**, el escenario más peligroso posible en un motor de control de acceso.

- **Corrección:** `ToDouble` ahora lanza `InvalidOperationException` ante `null`, permitiendo que `FaultTolerantPolicyDecorator` la traduzca correctamente en `Deny`.

### 3.3 Warnings de test resueltos

- Advertencia de analizador xUnit v3 (`xUnit1051`) por no usar `TestContext.Current.CancellationToken` en llamadas async dentro de tests. Resuelto pasando el token del framework de test en cada llamada, con recomendación de centralizarlo en una clase base (`AbacTestBase`).

---

## 4. Cambios realizados (mapeo completo por tema)

### 4.1 Trazabilidad de decisiones (¿por qué denegó/permitió una política?)

**Problema original:** `CompiledPolicy.EvaluateAsync` solo devolvía *"la regla no se cumplió"*, sin indicar cuál de las condiciones (potencialmente anidadas) falló. Inviable para que un usuario no técnico debuguee su propia política.

**Solución:**
- Nueva interfaz opcional `IExplainableConditionNode : IConditionNode` con método `Explain(context) → ConditionTrace`.
- `ConditionTrace`: DTO recursivo que refleja el árbol de condiciones con el resultado real de cada nodo (atributo, operador, valor esperado, valor real).
- `IsSatisfiedBy` (ruta rápida, con cortocircuito) queda intacto — el costo de `Explain` (sin cortocircuito, recorre todo el árbol) solo se paga en el camino de `Deny`, nunca en `Permit`.
- `CompiledPolicy.CollectFailingLeaves` aplana el árbol a un mensaje legible con las condiciones específicas que fallaron.
- Costo de rendimiento configurable vía `explainOnDeny` (pensado para conectarse a `AbacEngineOptions.EnableDetailedLogging`).

### 4.2 Sandbox / dry-run antes de publicar

**Problema original:** Ninguna forma de probar una política antes de que afectara tráfico real. El único gate era compilación sintáctica.

**Solución:**
- `IPolicySandbox.TestAsync(definition, testCases)` — compila y evalúa contra contextos de prueba **sin tocar** `IPolicyRepository` ni el caché.
- `PolicyTestReport` / `PolicyTestCaseResult`: exponen errores de compilación, `ConditionTrace` por caso, y errores de runtime **sin enmascarar** (a diferencia de producción, aquí el usuario necesita ver el error real para corregirlo).

### 4.3 Ampliación de operadores

**Problema original:** Set insuficiente (`Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `In`, `NotIn`, `ContainsAttribute`, `NotContainsAttribute`) para reglas de negocio típicas.

**Operadores agregados:**

| Categoría | Operadores nuevos |
|---|---|
| Numéricos | `GreaterThanOrEqual`, `LessThanOrEqual` |
| Texto | `StartsWith`, `EndsWith`, `ContainsText` |
| Rangos numéricos | `Between`, `NotBetween` (con validación min > max) |
| Fechas | `DateAfter`, `DateBefore`, `DateBetween` (nombres distintos a los numéricos para no forzar inferencia de tipo ambigua) |

Todos siguen el patrón OCP existente: clase nueva + una línea de registro en `RegisterOperators`, sin tocar `JsonPolicyCompiler` ni `OperatorRegistry`.

### 4.4 Validación de forma estricta (previene bugs silenciosos)

**Problemas cerrados:**

1. Nodo con `Attribute` y `Conditions` poblados simultáneamente — antes se descartaba `Conditions` en silencio; ahora se rechaza en compilación.
2. Operador lógico (`And`/`Or`/`Not`) usado en un nodo hoja — antes fallaba con mensaje genérico de `OperatorRegistry`; ahora mensaje específico y temprano.
3. `Value` ausente en un operador que lo requiere (ej. `Between`, `StartsWith`) — antes explotaba en el primer `EvaluateAsync` real; ahora se rechaza en `Compile()`.
4. `ObligationDefinition.FulfillOn` con valor fuera de `Permit`/`Deny` (ej. typo `"Grant"`) — antes la obligación simplemente nunca se disparaba, sin ningún aviso; ahora se rechaza en compilación.

### 4.5 Conexión de `Description` al árbol de trazabilidad

**Problema original:** `ConditionDefinition.Description` (el texto de negocio que el usuario escribe en la UI) existía en el DTO pero se perdía — nunca llegaba a `AttributeConditionNode` ni `CompositeConditionNode`.

**Solución:** `JsonPolicyCompiler.BuildLeaf`/`BuildComposite` propagan `def.Description` al construir los nodos. El `ConditionTrace` y, por lo tanto, el `Decision.Reason` en producción, ahora muestran el texto de negocio ("Criterio de No Conflicto:...") en vez de solo el path técnico (`Subject.ConflictsOfInterest`).

### 4.6 Rutas de atributo anidadas

**Problema original:** El regex de `AttributePathResolver` solo permitía un nivel (`Categoria.Atributo`), bloqueando estructuras como `Resource.Owner.Department`.

**Decisión de diseño:** se descartó reflexión abierta (rompería el principio de seguridad documentado — "no hay eval, no hay reflexión"). Se optó por **`AttributeBag` anidado**: un valor bajo una clave puede ser otro `AttributeBag`, y el resolver camina de bag en bag con el mismo mecanismo tipado (`Get<T>`) ya existente.

**Características de la solución:**
- 100% retrocompatible — rutas de un solo nivel funcionan igual.
- Sin nueva superficie de ataque — mismo saneamiento por regex, ahora aplicado a N segmentos.
- Fail-safe en la navegación intermedia (una cadena rota resuelve a `null`, no lanza — la excepción, si corresponde, la lanza el operador consumidor).
- Límite defensivo de profundidad (`MaxSegments = 10`) para evitar rutas absurdas generadas por error o abuso.
- **Fuera de alcance deliberado:** indexación dentro de colecciones (`Resource.Tags[0].Name`). Los casos reales de "¿está X en esta colección?" ya se resuelven con `ContainsAttribute`.

### 4.7 Parámetros de obligaciones

**Problema original:** `Decision.Obligations` era `IReadOnlyList<string>` — solo IDs. Los `Parameters` definidos en el JSON (ej. `NotifyVia: "SIEM"`) se calculaban y se descartaban sin llegar nunca al consumidor.

**Solución:**
- Nuevo DTO `Obligation` (`Id` + `Parameters` normalizados a tipos CLR, no `JsonElement` crudo).
- `Decision.Obligations` cambia de tipo a `IReadOnlyList<Obligation>` (cambio incompatible deliberado, asumido ahora antes de más adopción).
- **Optimización de rendimiento asociada:** la partición Permit/Deny de obligaciones, que antes se filtraba con `.Where(...)` en cada `EvaluateAsync`, ahora se precalcula una sola vez en `JsonPolicyCompiler.Compile()` — el hot path de evaluación queda en operación O(1) de selección de lista, sin filtrado por request.
- `AuditEntry.Obligations` se mantiene como `IReadOnlyList<string>` (solo IDs) — es el registro compacto e inmutable; el consumidor mapea `decision.Obligations.Select(o => o.Id)` al construir la entrada de auditoría.

### 4.8 Versionado y rollback de políticas

**Problema original:** `IPolicyRepository` no tenía historial. Recuperarse de una política mal publicada dependía de que alguien la reescribiera bien a mano bajo presión, sin registro de qué cambió ni quién lo hizo.

**Solución — filosofía append-only** (misma que ya aplicabas en `AuditEntry`): nunca se edita ni se borra una versión; revertir es publicar de nuevo el contenido de una versión anterior.

- `PolicyVersion`: entrada inmutable del historial (`PolicyId`, `Version`, `Definition`, `PublishedAtUtc`, `PublishedBy`, `ChangeNote`, `RevertedFromVersion`).
- `IPolicyVersionStore`: abstracción append-only (`AppendAsync`, `GetHistoryAsync`, `GetLatestAsync`, `GetVersionAsync`, `GetAllLatestAsync`). Interfaz separada de `IPolicyRepository` por ISP — no obliga a adoptar versionado.
- `VersionedPolicyRepository`: adapta `IPolicyVersionStore` a `IPolicyRepository` sin que `DynamicPolicyCache` se entere de que existe versionado — el consumidor implementa una sola interfaz y obtiene ambas capacidades.
- `PolicyPublishingService`: único punto de entrada de la UI para `PublishAsync` (nunca publica algo que no compile) y `RevertToAsync` (crea una versión nueva con el contenido de una vieja, revalidando compilación por si el whitelist de operadores cambió desde la publicación original). Ambos caminos disparan `IPolicyChangeNotifier` para invalidación multi-pod.
- Registro en DI condicional: si el consumidor no registra `IPolicyVersionStore`, el sistema sigue funcionando con un `IPolicyRepository` simple sin versionado — no es una adopción forzada.

---

## 5. Sugerencias de mejora (pendientes, no bloqueantes)

Trabajo identificado pero no abordado en este ciclo — quedan como backlog priorizable:

| # | Sugerencia | Justificación |
|---|---|---|
| 1 | **Precisión numérica**: migrar de `double` a `decimal` en `JsonValueNormalizer` para montos monetarios | `double` puede perder precisión en cifras grandes o cálculos financieros encadenados |
| 2 | **Límite de profundidad en el árbol de condiciones** (`CompositeConditionNode`/`BuildNode`) | Recursión sin límite ante anidamiento extremo desde la UI puede causar `StackOverflowException`, que en .NET no es capturable ni por `FaultTolerantPolicyDecorator` |
| 3 | **Indexación de colecciones en rutas de atributo** (`Resource.Tags[0].Name`) | Quedó fuera de alcance en #4.6 deliberadamente; evaluar solo si un proyecto real lo necesita |
| 4 | **Métricas de uso del sandbox vs. publicaciones directas** | Ayudaría a medir si la UI realmente está usándose como control de calidad antes de publicar |
| 5 | **Expiración / archivado de versiones muy antiguas** en `IPolicyVersionStore` | El historial es append-only por diseño; a largo plazo puede convertirse en una decisión de infraestructura (no de este motor) sobre cuánto conservar |
| 6 | **Tests de regresión para los operadores de fecha** | `DateTimeConverter.ToDateTimeOffset` acepta múltiples formatos de entrada (`DateTimeOffset`, `DateTime`, `string` ISO 8601) — vale la pena una batería de tests específica dado que es una fuente típica de bugs sutiles de zona horaria |
| 7 | **Documentar la convención de `AttributeBag` anidado** en el README del paquete de distribución, para que quien lo adopte en otro proyecto entienda cómo modelar jerarquías sin necesitar leer el código del resolver |

---

## 6. Checklist de archivos tocados en este ciclo (referencia rápida)

> No se incluye código — solo el listado de qué archivo cambió y por qué motivo, para que sirva de guía al aplicar los cambios manualmente.

**Abstractions**
- `Dynamic/IExplainableConditionNode.cs` — nuevo
- `Dynamic/ConditionTrace.cs` — nuevo
- `Dynamic/IPolicySandbox.cs` — nuevo
- `Dynamic/PolicyTestReport.cs` — nuevo
- `Dynamic/PolicyVersion.cs` — nuevo
- `Dynamic/IPolicyVersionStore.cs` — nuevo
- `Obligation.cs` — nuevo
- `Decision.cs` — modificado (`Obligations` cambia de tipo, se agrega `Trace`)

**Engine**
- `Dynamic/Conditions/AttributeConditionNode.cs` — modificado (implementa `IExplainableConditionNode`, recibe `description`)
- `Dynamic/Conditions/CompositeConditionNode.cs` — modificado (ídem, sin cortocircuito en `Explain`)
- `Dynamic/Operators/ComparisonOperators.cs` — modificado (fix fail-closed en `ToDouble`) + operadores `GreaterThanOrEqual`/`LessThanOrEqual`
- `Dynamic/Operators/TextOperators.cs` — nuevo
- `Dynamic/Operators/BetweenOperator.cs` — nuevo
- `Dynamic/Operators/DateTimeOperators.cs` — nuevo
- `Dynamic/AttributePathResolver.cs` — modificado (soporte de anidamiento)
- `Dynamic/JsonPolicyCompiler.cs` — modificado (validación de forma estricta, propagación de `Description`, normalización de obligaciones separadas por Permit/Deny)
- `Dynamic/CompiledPolicy.cs` — modificado (trace en Deny, obligaciones precalculadas, método obsoleto eliminado)
- `Dynamic/PolicySandbox.cs` — nuevo
- `Dynamic/VersionedPolicyRepository.cs` — nuevo
- `Dynamic/PolicyPublishingService.cs` — nuevo
- `Audit/AuditPersistenceWorker.cs` — modificado (accesibilidad `public` → `internal`)
- `Extensions/DynamicAbacEngineExtensions.cs` — modificado (registro de sandbox, versionado condicional, operadores nuevos)

**Tests**
- Clase base sugerida `AbacTestBase` con `TestContext.Current.CancellationToken` centralizado

---

## Estructura del proyecto.
```text
FeVall.Abac.sln
│
├── src
│   ├── FeVall.Abac.Abstractions
│   │   ├── Audit
│   │   │   ├── AuditEntry.cs
│   │   │   ├── IAuditSink.cs
│   │   ├── Dynamic
│   │   │   ├── IComparisonOperator.cs
│	│	│	├── IConditionNode.cs
│	│	│	├── IOperatorRegistry.cs
│	│	│	├── IPolicyChangeNotifier.cs
│	│	│	├── IPolicyCompiler.cs
│	│	│	├── IPolicyProvider.cs
│	│	│	├── IPolicyRepository.cs
│	│	│	├── IShortCircuitCombinationStrategy.cs
│	│	│	└── PolicyDefinition.cs
│   │   ├── AttributeBag.cs
│	│	├── Decision.cs
│	│	├── EvaluationContext.cs
│	│	├── EvaluationContextBuilder.cs
│	│	├── IAbacEngine.cs
│	│	├── IAbacLogger.cs
│	│	├── ICombinationStrategy.cs
│	│	├── IEvaluationContext.cs
│	│	├── IPolicy.cs
│	│	├── IPolicyApplicability.cs
│	│	├── IPolicyEvaluator.cs
│   │       
│   │
│   ├── FeVall.Abac.Engine
│   │   ├── Audit
│   │   │   ├── AuditPersistenceWorker.cs
│   │   │   └── ChannelAuditSink.cs
│	│	├── Dynamic
│	│	│	├── Conditions
│	│	│	│	├── AuditPersistenceWorker.cs
│	│	│	│	└── AuditPersistenceWorker.cs
│	│	│	├── Operators
│	│	│	│	├── AuditPersistenceWorker.cs
│	│	│	│	├── GreaterThanOperator.cs
│	│	│	│	├── AuditPersistenceWorker.cs
│	│	│	│	├── EqualsOperator.cs
│	│	│	│	└──InOperator.cs
│	│	│	├── AttributePathResolver.cs
│	│	│	├── CompiledPolicy.cs
│	│	│	├── FaultTolerantPolicyDecorator.cs
│	│	│	├── JsonPolicyCompiler.cs
│	│	│	├── JsonValueNormalizer.cs
│	│	│	├── PolicyCompilationException.cs
│	│	│	├── ShortCircuitPolicyEvaluator.cs
│	│	│	│	
│   │   ├── Extensions
│	│	│	├── AbacEngineExtensions.cs
│	│	│	├── AbacEngineOptions.cs
│	│	│	├── DynamicAbacEngineExtensions.cs
│	│	├── Logging
│	│	│	├── AbacConsoleLogger.cs
│	│	├── Strategies
│	│	│	├── DenyOverridesStrategy.cs
│	│	│	├── PermitUnlessDenyStrategy.cs
│	│	├── AbacEngine.cs
│	│	├── NullGuardPolicyEvaluator.cs
│   │   └── PolicyEvaluator.cs
│   │   │
│   ├── FeVall.Abac.Sample
│     
└── tests
    ├── FeVall.Domain.Tests
    └── FeVall.Application.Tests
```
---

## Codigo fuente completo (cada clase)