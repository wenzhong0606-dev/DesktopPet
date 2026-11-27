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
    public void BuildMessages_UsesPrimaryNicknameAndPersona_NotManagementLabelOrBehaviorState()
    {
        var companion = new CompanionIdentity { Id = "private-stable-id", DisplayName = "身份管理名" };
        var avatar = new AvatarProfile
        {
            Id = "avatar-1",
            Name = "图片角色标签",
            Species = "dog",
            Breed = "比熊",
            Persona = new PersonaProfile
            {
                Nicknames = ["豆包", "小豆"],
                SelfReference = "本汪",
                UserAddress = "主人",
                PersonalityTraits = ["好奇", "温柔"],
                SpeechStyle = "cute",
                ResponseLength = "medium",
                EmojiLevel = "high",
                Catchphrases = ["汪汪！"]
            }
        };

        var messages = ChatContextBuilder.BuildMessages(companion, avatar, PetBehaviorState.Happy,
            [new AiChatMessage("user", "你好")]);

        Assert.AreEqual("system", messages[0].Role);
        StringAssert.Contains(messages[0].Content, "豆包");
        StringAssert.Contains(messages[0].Content, "本汪");
        StringAssert.Contains(messages[0].Content, "主人");
        StringAssert.Contains(messages[0].Content, "好奇、温柔");
        StringAssert.Contains(messages[0].Content, "事实准确");
        StringAssert.Contains(messages[0].Content, "禁止舞台指示");
        Assert.IsFalse(messages[0].Content.Contains("图片角色标签", StringComparison.Ordinal));
        Assert.IsFalse(messages[0].Content.Contains("身份管理名", StringComparison.Ordinal));
        Assert.IsFalse(messages[0].Content.Contains("private-stable-id", StringComparison.Ordinal));
        Assert.IsFalse(messages[0].Content.Contains("Happy", StringComparison.Ordinal));
        Assert.IsFalse(messages[0].Content.Contains("汪汪！", StringComparison.Ordinal));
        StringAssert.Contains(messages[0].Content, "其他昵称别名：小豆");
        Assert.AreEqual("user", messages[1].Role);
        Assert.AreEqual("你好", messages[1].Content);
    }

    [TestMethod]
    public void BuildMessages_KeepsOnlyRecentSupportedMessagesAndBoundsContent()
    {
        var history = Enumerable.Range(0, 15)
            .Select(index => new AiChatMessage("user", $"message-{index}"))
            .Concat([new AiChatMessage("tool", "must-not-pass"), new AiChatMessage("assistant", new string('x', 2500))])
            .ToArray();

        var messages = ChatContextBuilder.BuildMessages(
            new CompanionIdentity { DisplayName = "PRIVATE-COMPANION-DISPLAY" },
            new AvatarProfile { Name = "PRIVATE-AVATAR-MANAGEMENT" },
            PetBehaviorState.Idle, history, historyLimit: 3);

        Assert.AreEqual(4, messages.Count);
        Assert.AreEqual("message-13", messages[1].Content);
        Assert.AreEqual("message-14", messages[2].Content);
        Assert.AreEqual(2000, messages[3].Content.Length);
        Assert.IsTrue(messages.All(message => message.Role != "tool"));
        Assert.IsFalse(messages[0].Content.Contains("PRIVATE-COMPANION-DISPLAY", StringComparison.Ordinal));
        Assert.IsFalse(messages[0].Content.Contains("PRIVATE-AVATAR-MANAGEMENT", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildMessages_RejectsUnboundedHistoryLimit()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            ChatContextBuilder.BuildMessages(new CompanionIdentity(), new AvatarProfile(), PetBehaviorState.Idle, [], historyLimit: 51));
    }
}
