using FeVall.Abac.Abstractions;
using FeVall.Abac.Engine.Extensions;
using FeVall.Abac.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FeVall.Abac.Tests.Integration
{
    public sealed class AbacEngineIntegrationTests
    {
        private static IAbacEngine BuildEngine(params IPolicy[] policies)
        {
            var services = new ServiceCollection();

            services.AddLogging(l => l.SetMinimumLevel(LogLevel.None));
            services.AddSingleton<IAbacLogger, FakeLogger>();

            services.AddAbacEngine(options =>
                options.CombinationStrategy = CombinationStrategy.DenyOverrides);

            foreach (var policy in policies)
                services.AddSingleton<IPolicy>(_ => policy);

            return services
                .BuildServiceProvider()
                .GetRequiredService<IAbacEngine>();
        }
        private static IEvaluationContext BuildContext(
        string userId = "user-1",
        string ownerId = "user-1",
        string resource = "document") =>
        EvaluationContextBuilder
            .Create()
            .WithSubject(s => s.Set("userId", userId))
            .WithResource(r => r.Set("ownerId", ownerId).Set("type", resource))
            .WithAction(a => a.Set("type", "read"))
            .Build();

        [Fact]
        public async Task EvaluateAsync_WithPermitPolicy_ShouldReturnPermit()
        {
            var engine = BuildEngine(new FakePolicy("AllowAll", Decision.Permit));
            var context = BuildContext();

            var decision = await engine.EvaluateAsync(context, CancellationToken.None);

            decision.IsPermit.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_WithDenyPolicy_ShouldReturnDeny()
        {
            var engine = BuildEngine(new FakePolicy("DenyAll", Decision.DenyWith("denegado")));
            var context = BuildContext();

            var decision = await engine.EvaluateAsync(context);

            decision.IsDeny.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_WithNoPolicies_ShouldReturnDeny()
        {
            var engine = BuildEngine();
            var context = BuildContext();

            var decision = await engine.EvaluateAsync(context);

            decision.IsDeny.Should().BeTrue();
            decision.Reason.Should().Contain("No hay políticas");
        }
        [Fact]
        public async Task EvaluateAsync_WithNullContext_ShouldThrow()
        {
            var engine = BuildEngine(new FakePolicy("Any", Decision.Permit));

            var act = async () => await engine.EvaluateAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
        [Fact]
        public async Task EvaluateAsync_OneDenyAmongPermits_ShouldReturnDeny()
        {
            var engine = BuildEngine(
                new FakePolicy("Allow1", Decision.PermitWith("ok")),
                new FakePolicy("Deny1", Decision.DenyWith("bloqueado")),
                new FakePolicy("Allow2", Decision.PermitWith("ok")));

            var context = BuildContext();
            var decision = await engine.EvaluateAsync(context);

            decision.IsDeny.Should().BeTrue();
            decision.Reason.Should().Contain("bloqueado");
        }
    }
}
