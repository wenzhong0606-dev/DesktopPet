using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ChatWelcomeMessageBuilderTests
{
    [TestMethod]
    public void Build_UsesAvatarNameSelfReferenceAndUserAddress()
    {
        var companion = new CompanionIdentity { DisplayName = "备用名称" };
        var avatar = new AvatarProfile
        {
            Name = "豆包",
            Persona = new PersonaProfile
            {
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
        Assert.IsFalse(message.Contains("备用名称"));
    }

    [TestMethod]
    public void Build_ProducesDistinctVoiceForEverySupportedSpeechStyle()
    {
        var styles = new[] { "cute", "gentle", "cool", "tsundere", "playful", "natural" };
        var messages = styles.Select(style => ChatWelcomeMessageBuilder.Build(
            new CompanionIdentity { DisplayName = "伙伴" },
            new AvatarProfile
            {
                Name = "星星",
                Persona = new PersonaProfile
                {
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
        }
    }

    [TestMethod]
    public void Build_UsesNicknameTraitCatchphraseAndEmojiPreferences()
    {
        var avatar = new AvatarProfile
        {
            Name = "豆包",
            Persona = new PersonaProfile
            {
                Nicknames = ["团子"],
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

        StringAssert.Contains(message, "本汪很喜欢和你待在一起");
        StringAssert.Contains(message, "团子");
        StringAssert.Contains(message, "汪汪，出发！");
        StringAssert.Contains(message, "🐾");
    }

    [TestMethod]
    public void Build_UsesNaturalFallbackAndAvoidsDuplicatingGenericUserAddress()
    {
        var message = ChatWelcomeMessageBuilder.Build(
            new CompanionIdentity { DisplayName = "小伙伴" },
            new AvatarProfile
            {
                Persona = new PersonaProfile
                {
                    UserAddress = "你",
                    SelfReference = "我",
                    SpeechStyle = "natural",
                    EmojiLevel = "low"
                }
            });

        StringAssert.Contains(message, "小伙伴");
        Assert.IsFalse(message.Contains("你，你"));
        Assert.IsFalse(message.Contains("null"));
    }
}
