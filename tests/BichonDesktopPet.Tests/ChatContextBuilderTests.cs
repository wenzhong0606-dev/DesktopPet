using System;
using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ChatContextBuilderTests
{
    [TestMethod]
    public void BuildMessages_IncludesCurrentPersonaAndBehaviorWithoutIdentitySecrets()
    {
        var companion = new CompanionIdentity
        {
            Id = "private-stable-id",
            DisplayName = "豆包"
        };
        var avatar = new AvatarProfile
        {
            Id = "avatar-1",
            Name = "豆包",
            Species = "dog",
            Breed = "比熊",
            Persona = new PersonaProfile
            {
                SelfReference = "本汪",
                UserAddress = "主人",
                PersonalityTraits = ["好奇", "温柔"],
                SpeechStyle = "cute",
                ResponseLength = "medium",
                EmojiLevel = "high",
                Catchphrases = ["汪汪！"]
            }
        };

        var messages = ChatContextBuilder.BuildMessages(
            companion,
            avatar,
            PetBehaviorState.Happy,
            [new AiChatMessage("user", "你好")]);

        Assert.AreEqual("system", messages[0].Role);
        StringAssert.Contains(messages[0].Content, "豆包");
        StringAssert.Contains(messages[0].Content, "本汪");
        StringAssert.Contains(messages[0].Content, "主人");
        StringAssert.Contains(messages[0].Content, "好奇、温柔");
        StringAssert.Contains(messages[0].Content, "Happy");
        Assert.IsFalse(messages[0].Content.Contains("private-stable-id", StringComparison.Ordinal));
        Assert.AreEqual("user", messages[1].Role);
        Assert.AreEqual("你好", messages[1].Content);
    }

    [TestMethod]
    public void BuildMessages_KeepsOnlyRecentSupportedMessagesAndBoundsContent()
    {
        var history = Enumerable.Range(0, 15)
            .Select(index => new AiChatMessage("user", $"message-{index}"))
            .Concat([
                new AiChatMessage("tool", "must-not-pass"),
                new AiChatMessage("assistant", new string('x', 2500))
            ])
            .ToArray();

        var messages = ChatContextBuilder.BuildMessages(
            new CompanionIdentity { DisplayName = "伙伴" },
            new AvatarProfile { Name = "伙伴" },
            PetBehaviorState.Idle,
            history,
            historyLimit: 3);

        Assert.AreEqual(4, messages.Count);
        Assert.AreEqual("message-14", messages[1].Content);
        Assert.AreEqual(2000, messages[3].Content.Length);
        Assert.IsTrue(messages.All(message => message.Role != "tool"));
    }

    [TestMethod]
    public void BuildMessages_RejectsUnboundedHistoryLimit()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            ChatContextBuilder.BuildMessages(
                new CompanionIdentity(),
                new AvatarProfile(),
                PetBehaviorState.Idle,
                [],
                historyLimit: 51));
    }
}
