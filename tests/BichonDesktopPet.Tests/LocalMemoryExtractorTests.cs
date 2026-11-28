using System.Linq;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class LocalMemoryExtractorTests
{
    [DataTestMethod]
    [DataRow("我一直很喜欢猫", "猫", "用户喜欢猫")]
    [DataRow("我喜欢看书", "看书", "用户喜欢看书")]
    [DataRow("我不喜欢喝奶茶。", "奶茶", "用户不喜欢奶茶")]
    [DataRow("我爱听爵士乐", "爵士乐", "用户喜欢爵士乐")]
    [DataRow("我最喜欢的颜色是蓝色", "蓝色", "用户喜欢蓝色")]
    [DataRow("我现在更喜欢喝咖啡", "咖啡", "用户喜欢咖啡")]
    [DataRow("我现在不太喜欢奶茶了", "奶茶", "用户不太喜欢奶茶")]
    [DataRow("我对摄影很感兴趣", "摄影", "用户对摄影感兴趣")]
    [DataRow("我对摄影不太感兴趣", "摄影", "用户对摄影不太感兴趣")]
    [DataRow("我以前喜欢咖啡，现在不喜欢了", "咖啡", "用户不喜欢咖啡")]
    [DataRow("我以前喜欢咖啡，现在更喜欢茶", "茶", "用户喜欢茶")]
    public void Extract_ExplicitOrdinaryPreferences(string input, string topic, string content)
    {
        var candidate = LocalMemoryExtractor.Extract(input).Single();
        Assert.AreEqual(topic, candidate.Topic);
        Assert.AreEqual(content, candidate.Content);
    }

    [DataTestMethod]
    [DataRow("你喜欢喝咖啡吗？")]
    [DataRow("我喜欢咖啡吗？")]
    [DataRow("朋友喜欢火锅")]
    [DataRow("假设我喜欢咖啡")]
    [DataRow("我今天喜欢咖啡")]
    [DataRow("我以前喜欢咖啡")]
    [DataRow("我喜欢咖啡，我的密码是abc123")]
    [DataRow("我喜欢某某私人地址")]
    [DataRow("我喜欢咖啡，不喜欢咖啡")]
    [DataRow("请记住我银行卡密码123456")]
    [DataRow("我喜欢的颜色是我的密码")]
    [DataRow("我对摄影感兴趣吗")]
    [DataRow("我以前喜欢咖啡，朋友喜欢茶")]
    [DataRow("我喜欢咖啡和银行卡密码")]
    [DataRow("我以前喜欢咖啡，现在可能不喜欢了")]
    [DataRow("我喜欢看书。我的身份证号码是123456789")]
    public void Extract_DoesNotCaptureQuestionsHypotheticalsOrUnrecognizedSensitiveText(string input)
    {
        Assert.AreEqual(0, LocalMemoryExtractor.Extract(input).Count);
    }
    [TestMethod]
    public void Extract_CoordinatedPreferencesKeepTheirOwnPolarity()
    {
        var result = LocalMemoryExtractor.Extract("我喜欢喝咖啡和茶，但不喜欢奶茶");
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("用户喜欢咖啡", result[0].Content);
        Assert.AreEqual("用户喜欢茶", result[1].Content);
        Assert.AreEqual("用户不喜欢奶茶", result[2].Content);
    }

    [TestMethod]
    public void Extract_AdditionalCategoriesAreClassified()
    {
        Assert.AreEqual("色彩", LocalMemoryExtractor.Extract("我最喜欢的颜色是蓝色").Single().Category);
        Assert.AreEqual("兴趣", LocalMemoryExtractor.Extract("我对烘焙很感兴趣").Single().Category);
    }
}
