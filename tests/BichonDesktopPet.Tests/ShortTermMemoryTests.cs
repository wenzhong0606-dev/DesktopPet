using System;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ShortTermMemoryTests
{
    [DataTestMethod]
    [DataRow("我今天有点累","临时状态","状态·疲劳自述",8)]
    [DataRow("我今天不累","临时状态","状态·疲劳自述",8)]
    [DataRow("我今天很忙","临时状态","状态·忙碌自述",8)]
    [DataRow("我明天准备去跑步","近期计划","计划·跑步",36)]
    [DataRow("我明天打算看书","近期计划","计划·看书",36)]
    public void ExplicitStatement_HasBoundedLifetime(string input,string category,string topic,int hours)
    {
        var candidate=ShortTermMemoryExtractor.Extract(input);
        Assert.IsNotNull(candidate);
        Assert.AreEqual(category,candidate.Category);
        Assert.AreEqual(topic,candidate.Topic);
        Assert.AreEqual<TimeSpan?>(TimeSpan.FromHours(hours),ShortTermMemoryExtractor.Lifetime(category));
        Assert.AreEqual(0,LocalMemoryExtractor.Extract(input).Count);
    }

    [DataTestMethod]
    [DataRow("我喜欢跑步")]
    [DataRow("我朋友今天很累")]
    [DataRow("我明天可能去跑步")]
    [DataRow("我明天准备去跑步吗")]
    [DataRow("我明天打算看病")]
    [DataRow("我今天很忙，密码是测试")]
    public void UnrelatedOrUncertainStatement_IsRejected(string input)
        => Assert.IsNull(ShortTermMemoryExtractor.Extract(input));
}
