using System.Linq;
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
    public void PersonaHappy_UsesPetNameAndUserAddress()
    {
        var avatar = new AvatarProfile
        {
            Id = "bichon",
            Name = "豆包",
            Species = "dog",
            Persona = new PersonaProfile
            {
                SelfReference = "豆包",
                UserAddress = "主人",
                SpeechStyle = "cute"
            }
        };

        var presentation = _resolver.Resolve(PetBehaviorState.Happy, avatar);

        StringAssert.Contains(presentation.Message, "主人");
        StringAssert.Contains(presentation.Message, "豆包");
    }

    [TestMethod]
    public void PersonaCoolStyle_ChangesTheResponseTone()
    {
        var avatar = new AvatarProfile
        {
            Id = "cool-cat",
            Name = "煤球",
            Species = "cat",
            Persona = new PersonaProfile
            {
                SelfReference = "煤球",
                UserAddress = "铲屎官",
                PersonalityTraits = ["独立"],
                SpeechStyle = "cool"
            }
        };

        var presentation = _resolver.Resolve(PetBehaviorState.Greeting, avatar);

        StringAssert.Contains(presentation.Message, "铲屎官");
        Assert.IsTrue(
            presentation.Message.Contains("回来了", StringComparison.Ordinal) ||
            presentation.Message.Contains("知道", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RepeatedSameState_UsesDifferentTemplatesBeforeRepeating()
    {
        var avatar = new AvatarProfile
        {
            Id = "variety-dog",
            Name = "团子",
            Species = "dog",
            Persona = new PersonaProfile()
        };

        var first = _resolver.Resolve(PetBehaviorState.Happy, avatar);
        var second = _resolver.Resolve(PetBehaviorState.Happy, avatar);
        var third = _resolver.Resolve(PetBehaviorState.Happy, avatar);

        Assert.AreNotEqual(first.Message, second.Message);
        Assert.AreNotEqual(second.Message, third.Message);
        Assert.AreNotEqual(first.Message, third.Message);
    }

    [TestMethod]
    public void MediumResponseAndHighEmoji_ApplyPersonaPreferences()
    {
        var avatar = new AvatarProfile
        {
            Id = "expressive-pet",
            Name = "团子",
            Species = "rabbit",
            Persona = new PersonaProfile
            {
                ResponseLength = "medium",
                EmojiLevel = "high"
            }
        };

        var presentation = _resolver.Resolve(PetBehaviorState.Excited, avatar);

        StringAssert.Contains(presentation.Message, "我们再玩一次吧！");
        StringAssert.Contains(presentation.Message, "✨");
    }

    [TestMethod]
    public void Catchphrase_CanBeUsedForHappyOrExcitedResponse()
    {
        var avatar = new AvatarProfile
        {
            Id = "catchphrase-pet",
            Name = "豆包",
            Species = "dog",
            Persona = new PersonaProfile
            {
                Catchphrases = ["豆包能量满满！"]
            }
        };

        var messages = new[]
        {
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message
        };

        CollectionAssert.Contains(messages, "豆包能量满满！");
    }

    [TestMethod]
    public void NicknamesAndCatchphrases_AreAvailableForEverySpeechStyle()
    {
        var avatar = new AvatarProfile
        {
            Id = "cool-persona",
            Name = "煤球",
            Species = "cat",
            Persona = new PersonaProfile
            {
                Nicknames = ["小煤球"],
                SpeechStyle = "cool",
                Catchphrases = ["煤球今天心情不错。"]
            }
        };

        var greetings = new[]
        {
            _resolver.Resolve(PetBehaviorState.Greeting, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Greeting, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Greeting, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Greeting, avatar).Message
        };
        Assert.IsTrue(greetings.Any(message => message.Contains("小煤球", StringComparison.Ordinal)));

        var happyMessages = new[]
        {
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message,
            _resolver.Resolve(PetBehaviorState.Happy, avatar).Message
        };

        CollectionAssert.Contains(happyMessages, "煤球今天心情不错。");
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
