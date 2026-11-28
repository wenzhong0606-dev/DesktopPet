using System.Linq;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class TemporalPreferenceTests
{
    [DataTestMethod]
    [DataRow("我过去不喜欢咖啡，但现在喜欢了","咖啡","用户喜欢咖啡")]
    [DataRow("我曾经喜欢奶茶，如今已经不喜欢了","奶茶","用户不喜欢奶茶")]
    [DataRow("我从前不太喜欢游泳，现在重新喜欢了","游泳","用户喜欢游泳")]
    [DataRow("我以前喜欢咖啡，现在还是喜欢","咖啡","用户喜欢咖啡")]
    [DataRow("我过去喜欢咖啡，现在喜欢奶茶","奶茶","用户喜欢奶茶")]
    public void ExplicitCurrentPreference_OnlyStoresCurrentFact(string input,string topic,string content)
    {
        var result=LocalMemoryExtractor.Extract(input).Single();
        Assert.AreEqual(topic,result.Topic);
        Assert.AreEqual(content,result.Content);
    }

    [DataTestMethod]
    [DataRow("我过去不喜欢咖啡")]
    [DataRow("我曾经喜欢奶茶")]
    [DataRow("我从前喜欢游泳")]
    [DataRow("我以前喜欢咖啡，但今天想喝奶茶")]
    [DataRow("我过去喜欢咖啡，现在可能不喜欢了")]
    [DataRow("我过去喜欢咖啡，以后可能喜欢奶茶")]
    [DataRow("我今天特别想喝咖啡")]
    [DataRow("我最近偶尔想喝奶茶")]
    [DataRow("我过去喜欢咖啡，现在喜欢了么")]
    public void HistoricalTemporaryOrUncertainStatement_DoesNotCreateCurrentFact(string input)
        => Assert.AreEqual(0,LocalMemoryExtractor.Extract(input).Count);
}
