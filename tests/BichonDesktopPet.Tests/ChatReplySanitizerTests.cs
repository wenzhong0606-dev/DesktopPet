using BichonDesktopPet.Core.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ChatReplySanitizerTests
{
    [TestMethod]
    public void Sanitize_RemovesParenthesizedActionAndExpressionDescriptions()
    {
        var result = ChatReplySanitizer.Sanitize("（摇尾巴）你好！（眨眨眼）今天想聊什么？");

        Assert.AreEqual("你好！今天想聊什么？", result);
    }

    [TestMethod]
    public void Sanitize_RemovesAsteriskWrappedStageDirections()
    {
        var result = ChatReplySanitizer.Sanitize("*开心地跳起来*答案是 42。");

        Assert.AreEqual("答案是 42。", result);
    }

    [TestMethod]
    public void Sanitize_PreservesOrdinaryParentheticalExplanations()
    {
        const string answer = "HTTP（超文本传输协议）是一种应用层协议。";

        Assert.AreEqual(answer, ChatReplySanitizer.Sanitize(answer));
    }

    [TestMethod]
    public void Sanitize_ReturnsEmptyForBlankOrOnlyStageDirections()
    {
        Assert.AreEqual(string.Empty, ChatReplySanitizer.Sanitize("   "));
        Assert.AreEqual(string.Empty, ChatReplySanitizer.Sanitize("（摇尾巴）*眨眨眼*"));
    }
}
