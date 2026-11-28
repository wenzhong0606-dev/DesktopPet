using System.Linq;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PreferenceFollowUpTests
{
    [DataTestMethod]
    [DataRow("我喜欢咖啡","现在不喜欢了","咖啡","用户不喜欢咖啡")]
    [DataRow("我不喜欢奶茶","但现在喜欢了","奶茶","用户喜欢奶茶")]
    [DataRow("我喜欢看书","如今不太喜欢了","阅读","用户不太喜欢阅读")]
    public void ExplicitCorrection_ResolvesUniquePreviousPreference(string previous,string current,string topic,string content)
    {
        var result=LocalMemoryExtractor.ExtractFollowUp(current,previous).Single();
        Assert.AreEqual(topic,result.Topic);
        Assert.AreEqual(content,result.Content);
    }

    [DataTestMethod]
    [DataRow("我喜欢咖啡和奶茶","现在不喜欢了")]
    [DataRow("今天天气怎么样","现在不喜欢了")]
    [DataRow("我以前喜欢咖啡","现在不喜欢了")]
    [DataRow("我喜欢咖啡","今天不想喝了")]
    [DataRow("我喜欢咖啡","现在可能不喜欢了")]
    [DataRow("我喜欢咖啡","现在不喜欢了吗")]
    [DataRow(null,"现在不喜欢了")]
    public void AmbiguousOrTemporaryCorrection_IsRejected(string? previous,string current)
        => Assert.AreEqual(0,LocalMemoryExtractor.ExtractFollowUp(current,previous).Count);
}
