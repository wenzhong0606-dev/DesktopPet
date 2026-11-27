using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ChatWelcomeMessageBuilderTests
{
    [TestMethod]
    public void Build_UsesPrimaryPetNicknameAndPersona_NotManagementLabel()
    {
        var companion = new CompanionIdentity { DisplayName = "身份管理名" };
        var avatar = new AvatarProfile
        {
            Name = "图片角色标签",
            Persona = new PersonaProfile
            {
                Nicknames = ["豆包", "小豆"],
                SelfReference = "本汪",
                UserAddress = "主人",
                SpeechStyle = "cute",
                EmojiLevel = "low"
            }
        };

        var message = ChatWelcomeMessageBuilder.Build(companion, avatar);

        StringAssert.Contains(message, "豆包");
        StringAssert.Contains(message, "主人");
        StringAssert.Contains(message, "本汪");
        Assert.IsFalse(message.Contains("图片角色标签"));
        Assert.IsFalse(message.Contains("身份管理名"));
        Assert.IsFalse(message.Contains("小豆"));
    }

    [TestMethod]
    public void Build_ProducesDistinctVoiceForEverySupportedSpeechStyle()
    {
        var styles = new[] { "cute", "gentle", "cool", "tsundere", "playful", "natural" };
        var messages = styles.Select(style => ChatWelcomeMessageBuilder.Build(
            new CompanionIdentity { DisplayName = "管理用名称" },
            new AvatarProfile
            {
                Name = "角色标签",
                Persona = new PersonaProfile
                {
                    Nicknames = ["星星"],
                    SelfReference = "本狐",
                    UserAddress = "队长",
                    SpeechStyle = style,
                    EmojiLevel = "low"
                }
            })).ToArray();

        Assert.AreEqual(styles.Length, messages.Distinct().Count());
        foreach (var message in messages)
        {
            StringAssert.Contains(message, "星星");
            StringAssert.Contains(message, "队长");
            StringAssert.Contains(message, "本狐");
            Assert.IsFalse(message.Contains("角色标签"));
        }
    }

    [TestMethod]
    public void Build_UsesPrimaryNicknameAndPersonalityWithoutForcingCatchphrases()
    {
        var avatar = new AvatarProfile
        {
            Name = "管理标签",
            Persona = new PersonaProfile
            {
                Nicknames = ["团子", "小团"],
                SelfReference = "本汪",
                UserAddress = "主人",
                PersonalityTraits = ["黏人"],
                SpeechStyle = "cute",
                ResponseLength = "medium",
                EmojiLevel = "high",
                Catchphrases = ["汪汪，出发！"]
            }
        };

        var message = ChatWelcomeMessageBuilder.Build(null, avatar);

        StringAssert.Contains(message, "团子");
        StringAssert.Contains(message, "本汪很喜欢和你待在一起");
        StringAssert.Contains(message, "🐾");
        Assert.IsFalse(message.Contains("管理标签"));
        Assert.IsFalse(message.Contains("小团"));
        Assert.IsFalse(message.Contains("汪汪，出发！"));
    }

    [TestMethod]
    public void Build_WithoutNicknameNeverFallsBackToManagementName()
    {
        var message = ChatWelcomeMessageBuilder.Build(
            new CompanionIdentity { DisplayName = "身份管理名" },
            new AvatarProfile
            {
                Name = "图片管理标签",
                Persona = new PersonaProfile
                {
                    UserAddress = "你",
                    SelfReference = "我",
                    SpeechStyle = "natural",
                    EmojiLevel = "low"
                }
            });

        Assert.IsFalse(message.Contains("身份管理名"));
        Assert.IsFalse(message.Contains("图片管理标签"));
        Assert.IsFalse(message.Contains("null"));
        StringAssert.Contains(message, "今天想聊些什么");
    }

    [TestMethod]
    public void GetPrimaryNickname_ReturnsTheFirstNicknameOnly()
    {
        var avatar = new AvatarProfile
        {
            Name = "管理名称",
            Persona = new PersonaProfile { Nicknames = ["豆包", "小豆", "豆豆"] }
        };

        Assert.AreEqual("豆包", ChatWelcomeMessageBuilder.GetPrimaryNickname(avatar));
    }
}
