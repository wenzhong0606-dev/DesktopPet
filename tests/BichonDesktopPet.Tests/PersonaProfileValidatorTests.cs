using System;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PersonaProfileValidatorTests
{
    [TestMethod]
    public void TryNormalize_NullProfile_ReturnsUsableDefaults()
    {
        Assert.IsTrue(PersonaProfileValidator.TryNormalize(null, out var persona, out var error));

        Assert.AreEqual(string.Empty, error);
        Assert.AreEqual("我", persona.SelfReference);
        Assert.AreEqual("你", persona.UserAddress);
        Assert.AreEqual("cute", persona.SpeechStyle);
        Assert.AreEqual("short", persona.ResponseLength);
    }

    [TestMethod]
    public void TryNormalize_TrimsAndDeduplicatesListValues()
    {
        var source = new PersonaProfile
        {
            Nicknames = [" 小豆 ", "小豆", "豆豆"],
            PersonalityTraits = [" 活泼 ", "黏人"],
            Catchphrases = [" 汪！ ", "汪！"]
        };

        Assert.IsTrue(PersonaProfileValidator.TryNormalize(source, out var normalized, out _));

        CollectionAssert.AreEqual(new[] { "小豆", "豆豆" }, normalized.Nicknames);
        CollectionAssert.AreEqual(new[] { "活泼", "黏人" }, normalized.PersonalityTraits);
        CollectionAssert.AreEqual(new[] { "汪！" }, normalized.Catchphrases);
    }

    [TestMethod]
    public void TryNormalize_RejectsMoreThanThreePersonalityTraits()
    {
        var source = new PersonaProfile
        {
            PersonalityTraits = ["活泼", "黏人", "好奇", "调皮"]
        };

        Assert.IsFalse(PersonaProfileValidator.TryNormalize(source, out _, out var error));
        StringAssert.Contains(error, "最多填写 3 项");
    }

    [TestMethod]
    public void TryNormalize_RejectsTooManyNicknamesOrCatchphrases()
    {
        var tooManyNicknames = new PersonaProfile
        {
            Nicknames = ["一", "二", "三", "四", "五", "六"]
        };
        var tooManyCatchphrases = new PersonaProfile
        {
            Catchphrases = ["一", "二", "三", "四", "五", "六"]
        };

        Assert.IsFalse(PersonaProfileValidator.TryNormalize(tooManyNicknames, out _, out _));
        Assert.IsFalse(PersonaProfileValidator.TryNormalize(tooManyCatchphrases, out _, out _));
    }

    [TestMethod]
    public void TryNormalize_RejectsMissingSelfReferenceOrUserAddress()
    {
        var missingSelf = new PersonaProfile { SelfReference = " " };
        var missingAddress = new PersonaProfile { UserAddress = string.Empty };

        Assert.IsFalse(PersonaProfileValidator.TryNormalize(missingSelf, out _, out _));
        Assert.IsFalse(PersonaProfileValidator.TryNormalize(missingAddress, out _, out _));
    }

    [TestMethod]
    public void NormalizeOrDefault_UsesSafeDefaultsForInvalidPersistedData()
    {
        var invalid = new PersonaProfile
        {
            SelfReference = new string('x', 30),
            SpeechStyle = "unsupported"
        };

        var normalized = PersonaProfileValidator.NormalizeOrDefault(invalid);

        Assert.AreEqual("我", normalized.SelfReference);
        Assert.AreEqual("cute", normalized.SpeechStyle);
    }
}
