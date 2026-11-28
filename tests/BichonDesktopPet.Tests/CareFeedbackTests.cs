using System;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class CareFeedbackTests
{
    [DataTestMethod]
    [DataRow("以后少提醒我一点","关怀·提醒频率","用户希望减少主动提醒")]
    [DataRow("以后可以多提醒我一点","关怀·提醒频率","用户希望适当增加主动提醒")]
    [DataRow("以后提醒我时简短一点","关怀·提醒方式","用户希望提醒简短")]
    [DataRow("以后少提醒我休息","关怀·休息提醒","用户希望减少休息提醒")]
    [DataRow("以后可以提醒我休息","关怀·休息提醒","用户希望适当提醒休息")]
    public void ExplicitLongTermFeedback(string input,string topic,string content)
    {
        var result=CareFeedbackExtractor.Extract(input);
        Assert.IsNotNull(result);
        Assert.AreEqual(topic,result.Topic);
        Assert.AreEqual(content,result.Content);
        Assert.AreEqual("关怀偏好",result.Category);
        Assert.IsNull(CareFeedbackExtractor.Lifetime(result.Category));
    }

    [DataTestMethod]
    [DataRow("现在先别打扰我","用户要求暂时不要主动打扰")]
    [DataRow("现在可以提醒我了","用户明确允许恢复普通提醒")]
    public void TemporaryFeedbackSharesTopicAndExpires(string input,string content)
    {
        var result=CareFeedbackExtractor.Extract(input);
        Assert.IsNotNull(result);
        Assert.AreEqual("关怀·临时免打扰",result.Topic);
        Assert.AreEqual(content,result.Content);
        Assert.AreEqual<TimeSpan?>(TimeSpan.FromHours(2),CareFeedbackExtractor.Lifetime(result.Category));
    }

    [DataTestMethod]
    [DataRow("我朋友希望你少提醒他")]
    [DataRow("假如我希望你少提醒我")]
    [DataRow("以后少提醒我一点吗")]
    [DataRow("我可能希望你少提醒我")]
    [DataRow("我今天很忙")]
    [DataRow("以后少提醒我一点，密码是测试")]
    public void AmbiguousOrUnrelatedFeedbackIsRejected(string input)
        => Assert.IsNull(CareFeedbackExtractor.Extract(input));
}
