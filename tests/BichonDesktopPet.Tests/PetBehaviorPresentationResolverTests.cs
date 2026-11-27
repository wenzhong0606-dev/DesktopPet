using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PetBehaviorPresentationResolverTests
{
    private readonly PetBehaviorPresentationResolver _resolver = new();

    [TestMethod]
    public void DogHappy_UsesHappyBounceAndDogMessage()
    {
        var presentation = _resolver.Resolve(PetBehaviorState.Happy, "dog");

        Assert.AreEqual(PetVisualEffect.HappyBounce, presentation.VisualEffect);
        Assert.AreEqual("嘿嘿，摸摸我！", presentation.Message);
    }

    [TestMethod]
    public void CatIdle_UsesCatSpecificMessage()
    {
        var presentation = _resolver.Resolve(PetBehaviorState.Idle, "cat");

        Assert.AreEqual(PetVisualEffect.IdleBreathing, presentation.VisualEffect);
        Assert.AreEqual("喵～我在这里", presentation.Message);
    }

    [TestMethod]
    public void RabbitHappy_UsesRabbitSpecificMessage()
    {
        var presentation = _resolver.Resolve(PetBehaviorState.Happy, "rabbit");

        Assert.AreEqual("蹦蹦！好开心", presentation.Message);
    }

    [TestMethod]
    public void UnknownSpecies_UsesGenericMessage()
    {
        var presentation = _resolver.Resolve(PetBehaviorState.Greeting, "unlisted-species");

        Assert.AreEqual("欢迎回来！", presentation.Message);
        Assert.AreEqual(PetVisualEffect.GreetingBounce, presentation.VisualEffect);
    }

    [TestMethod]
    public void EveryState_ResolvesToAVisualEffect()
    {
        foreach (var state in Enum.GetValues<PetBehaviorState>())
        {
            var presentation = _resolver.Resolve(state, "custom");

            Assert.AreEqual(state, presentation.State);
            Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.Message));
        }
    }
}
