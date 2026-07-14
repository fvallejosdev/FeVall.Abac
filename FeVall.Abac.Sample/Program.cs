// FeVall.Abac.Sample/Program.cs

using FeVall.Abac.Abstractions;
using FeVall.Abac.Engine.Extensions;
using FeVall.Abac.Sample.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ─── Composition Root ────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;
var services = new ServiceCollection();

services.AddLogging(logging =>
{
    logging.AddConsole(options =>
    {
        // Escribe sincrónico — sin buffer separado
        options.LogToStandardErrorThreshold = LogLevel.None;
    });
    logging.SetMinimumLevel(LogLevel.Debug);
});

services.AddAbacEngine(options =>
{
    options.CombinationStrategy = CombinationStrategy.DenyOverrides;
    options.DenyOnPolicyException = true;
    options.EnableDetailedLogging = true;
});

services.AddScoped<IPolicy, DocumentOwnerPolicy>();
services.AddScoped<IPolicy, BusinessHoursPolicy>();

await using var provider = services.BuildServiceProvider();
var engine = provider.GetRequiredService<IAbacEngine>();

await RunScenarioAsync(engine, "Dueño del documento accede",
    subject: s => s.Set("userId", "user-1"),
    resource: r => r.Set("ownerId", "user-1").Set("type", "internal-report"),
    action: a => a.Set("type", "read"));

await RunScenarioAsync(engine, "Usuario ajeno intenta acceder",
    subject: s => s.Set("userId", "user-2"),
    resource: r => r.Set("ownerId", "user-1").Set("type", "internal-report"),
    action: a => a.Set("type", "read"));

await RunScenarioAsync(engine, "Sin userId en el contexto",
    subject: s => s.Set("role", "viewer"),
    resource: r => r.Set("ownerId", "user-1").Set("type", "document"),
    action: a => a.Set("type", "read"));

static async Task RunScenarioAsync(
    IAbacEngine engine,
    string scenarioName,
    Action<AttributeBag> subject,
    Action<AttributeBag> resource,
    Action<AttributeBag> action)
{
    Console.WriteLine($"┌─ Escenario: {scenarioName}");
    Console.WriteLine($"│");

    var context = EvaluationContextBuilder
        .Create()
        .WithSubject(subject)
        .WithResource(resource)
        .WithAction(action)
        .WithEnvironment(e => e.Set("hour", DateTime.UtcNow.Hour))
        .Build();

    var decision = await engine.EvaluateAsync(context);

    Console.WriteLine($"│");
    var icon = decision.IsPermit ? "✅" : "❌";
    Console.WriteLine($"└─ Resultado: {icon} {decision.Effect} — {decision.Reason}");
    Console.WriteLine();
}