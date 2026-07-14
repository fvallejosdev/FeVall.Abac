using FeVall.Abac.Abstractions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FeVall.Abac.Tests.Unit
{
    public class AttributeBagTests
    {
        [Fact]
        public void Set_AndGet_ShouldReturnStoredValue()
        {
            var bag = new AttributeBag();
            bag.Set("userId", "user-1");

            bag.Get<string>("userId").Should().Be("user-1");
        }

        [Fact]
        public void Get_WhenKeyNotFound_ShouldReturnDefault()
        {
            var bag = new AttributeBag();

            bag.Get<string>("missing").Should().BeNull();
            bag.Get<int>("missing").Should().Be(0);
        }
        [Fact]
        public void Get_WhenTypeMismatch_ShouldReturnDefault()
        {
            var bag = new AttributeBag();
            bag.Set("value", 42);

            bag.Get<string>("value").Should().BeNull();
        }

        [Fact]
        public void GetRequired_WhenKeyNotFound_ShouldThrowKeyNotFoundException()
        {
            var bag = new AttributeBag();

            var act = () => bag.GetRequired<string>("missing");

            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*missing*");
        }
        [Fact]
        public void GetRequired_WhenTypeMismatch_ShouldThrowInvalidCastException()
        {
            var bag = new AttributeBag();
            bag.Set("value", 42);

            var act = () => bag.GetRequired<string>("value");

            act.Should().Throw<InvalidCastException>()
                .WithMessage("*value*");
        }

        [Fact]
        public void Has_WhenKeyExists_ShouldReturnTrue()
        {
            var bag = new AttributeBag();
            bag.Set("role", "admin");

            bag.Has("role").Should().BeTrue();
            bag.Has("missing").Should().BeFalse();
        }
        [Fact]
        public void Set_ShouldBeChainable()
        {
            var bag = new AttributeBag()
                .Set("userId", "user-1")
                .Set("role", "admin");

            bag.Keys.Should().HaveCount(2);
        }

        [Fact]
        public void Set_WithNullOrWhiteSpaceKey_ShouldThrow()
        {
            var bag = new AttributeBag();

            var act = () => bag.Set("", "value");

            act.Should().Throw<ArgumentException>();
        }
    }
}
