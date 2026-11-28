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
    public void Extract_DoesNotCaptureQuestionsHypotheticalsOrUnrecognizedSensitiveText(string input)
    {
        Assert.AreEqual(0, LocalMemoryExtractor.Extract(input).Count);
    }
}
