using System;
using System.Linq;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ExperienceMemoryExtractorTests
{
    private static readonly DateTimeOffset Recorded = new(2026, 11, 28, 10, 0, 0, TimeSpan.Zero);

    [DataTestMethod]
    [DataRow("我今天完成了学习计划","用户经历")]
    [DataRow("我昨天读完了一本书","用户经历")]
    [DataRow("我希望你回答简洁一点","交流偏好")]
    [DataRow("我希望你不要使用表情符号","交流偏好")]
    public void ExtractUserStatement_RecognizesFiniteExplicitStatements(string text, string category)
    {
        Assert.AreEqual(category, ExperienceMemoryExtractor.ExtractUserStatement(text, Recorded).Single().Category);
    }

    [DataTestMethod]
    [DataRow("我今天完成了学习计划吗？")]
    [DataRow("假如我今天完成了学习计划")]
    [DataRow("我们一起去了巴黎")]
    [DataRow("我希望你回答简洁一点，我的密码是123")]
    [DataRow("我今天完成了某某私人地址的调查")]
    [DataRow("我明天完成了学习计划")]
    public void ExtractUserStatement_RejectsUnverifiedOrUnsupportedClaims(string text)
    {
        Assert.AreEqual(0, ExperienceMemoryExtractor.ExtractUserStatement(text, Recorded).Count);
    }

    [TestMethod]
    public void ExtractExchange_ProducesOnlyFixedTopicDescription()
    {
        var item = ExperienceMemoryExtractor.ExtractExchange("帮我制定阅读计划。", Recorded);
        Assert.IsNotNull(item);
        Assert.AreEqual("对话·阅读计划·2026-11-28", item.Topic);
        StringAssert.Contains(item.Content, "未验证实际任务是否完成");
        Assert.IsNull(ExperienceMemoryExtractor.ExtractExchange("假设帮我制定阅读计划", Recorded));
    }
}
