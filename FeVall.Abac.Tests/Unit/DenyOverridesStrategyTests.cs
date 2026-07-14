using FeVall.Abac.Abstractions;
using FeVall.Abac.Engine.Strategies;
using FluentAssertions;
using Xunit;

namespace FeVall.Abac.Tests.Unit
{
    public sealed class DenyOverridesStrategyTests
    {
        private readonly DenyOverridesStrategy _strategy = new();
        [Fact]
        public void Combine_AllPermit_ShouldReturnPermit()
        {
            var decisions = new List<Decision>
        {
            Decision.PermitWith("política 1"),
            Decision.PermitWith("política 2")
        };

            var result = _strategy.Combine(decisions);

            result.IsPermit.Should().BeTrue();
        }
        [Fact]
        public void Combine_OneDeny_ShouldReturnDeny()
        {
            var decisions = new List<Decision>
        {
            Decision.PermitWith("política 1"),
            Decision.DenyWith("fuera de horario"),
            Decision.PermitWith("política 3")
        };

            var result = _strategy.Combine(decisions);

            result.IsDeny.Should().BeTrue();
            result.Reason.Should().Contain("fuera de horario");
        }
        [Fact]
        public void Combine_AllDeny_ShouldReturnDenyWithFirstReason()
        {
            var decisions = new List<Decision>
        {
            Decision.DenyWith("primera razón"),
            Decision.DenyWith("segunda razón")
        };

            var result = _strategy.Combine(decisions);

            result.IsDeny.Should().BeTrue();
            result.Reason.Should().Contain("primera razón");
        }
        [Fact]
        public void Combine_WithNullList_ShouldThrow()
        {
            var act = () => _strategy.Combine(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Name_ShouldBeClassName()
        {
            _strategy.Name.Should().Be(nameof(DenyOverridesStrategy));
        }
    }
}
