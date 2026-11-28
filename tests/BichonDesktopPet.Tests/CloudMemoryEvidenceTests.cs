using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class CloudMemoryEvidenceTests
{
    [DataTestMethod]
    [DataRow("我偏爱咖啡","咖啡","用户喜欢咖啡")]
    [DataRow("我对爵士乐情有独钟","爵士乐","用户喜欢爵士乐")]
    [DataRow("我对奶茶不太感冒","奶茶","用户不太喜欢奶茶")]
    public void LocalNaturalPreference(string input,string topic,string content)
    {
        var result=LocalMemoryExtractor.Extract(input).Single();
        Assert.AreEqual(topic,result.Topic);
        Assert.AreEqual(content,result.Content);
    }

    [DataTestMethod]
    [DataRow("我更喜欢简洁的回答","length",-1)]
    [DataRow("请你以后回答详细一些","length",1)]
    [DataRow("我希望你先说重点","direct",1)]
    public void FeedbackSharedByMemoryAndGrowth(string text,string axis,int direction)
    {
        var feedback=CommunicationFeedback.Recognize(text);
        Assert.IsNotNull(feedback);
        Assert.AreEqual(axis,feedback.Axis);
        Assert.AreEqual(direction,feedback.Direction);
        Assert.AreEqual(feedback.Content,ExperienceMemoryExtractor.ExtractUserStatement(text,DateTimeOffset.UtcNow).Single().Content);
    }

    [TestMethod]
    public void Validate_OnlyAcceptsOriginalFullLocallyRecognizedClauses()
    {
        const string input="对了，我偏爱咖啡";
        CollectionAssert.AreEqual(new[]{"我偏爱咖啡"},CloudMemoryEvidence.Validate(input,"""{"evidence":["我偏爱咖啡"]}""").ToArray());
        Assert.AreEqual(0,CloudMemoryEvidence.Validate(input,"""{"evidence":["我喜欢咖啡"]}""").Count);
        Assert.AreEqual(0,CloudMemoryEvidence.Validate(input,"""{"evidence":["偏爱咖啡"]}""").Count);
        Assert.AreEqual(0,CloudMemoryEvidence.Validate(input,"""{"evidence":["我偏爱咖啡"],"extra":"x"}""").Count);
        Assert.AreEqual(0,CloudMemoryEvidence.Validate(input,"not json").Count);
        Assert.AreEqual(0,CloudMemoryEvidence.Validate("我喜欢咖啡，我不喜欢咖啡","""{"evidence":["我喜欢咖啡"]}""").Count);
    }

    [DataTestMethod]
    [DataRow("对了，我偏爱咖啡，我的密码是123")]
    [DataRow("对了，我偏爱咖啡，我住在某某小区")]
    [DataRow("假设，我偏爱咖啡")]
    [DataRow("对了，我偏爱咖啡吗？")]
    [DataRow("对了，我喜欢某某私人信息")]
    [DataRow("我喜欢咖啡")]
    public void Gate_RejectsUnsupportedOrUnnecessaryMessages(string text)
    {
        Assert.IsFalse(CloudMemoryEvidence.CanRequest(text));
    }

    [TestMethod]
    public async Task SelectAsync_BoundedRequestAndErrorFallback()
    {
        var provider=new FakeProvider();
        var selected=await CloudMemoryEvidence.SelectAsync(provider,"对了，我偏爱咖啡");
        Assert.AreEqual(1,selected.Count);
        Assert.AreEqual(1,provider.Count);
        Assert.IsTrue(provider.Last!.JsonObjectResponse);
        Assert.AreEqual(180,provider.Last.MaxTokens);
        Assert.AreEqual(2,provider.Last.Messages.Count);
        Assert.AreEqual("对了，我偏爱咖啡",provider.Last.Messages[1].Content);
        provider.Fail=true;
        Assert.AreEqual(0,(await CloudMemoryEvidence.SelectAsync(provider,"对了，我偏爱咖啡")).Count);
        Assert.AreEqual(2,provider.Count);
        Assert.AreEqual(0,(await CloudMemoryEvidence.SelectAsync(provider,"对了，我的密码是123")).Count);
        Assert.AreEqual(2,provider.Count);
    }

    [TestMethod]
    public void Relevance_ExpandedAliasesAndIntent()
    {
        Assert.IsTrue(MemoryVocabulary.Relevance("猫","兴趣","猫咪")>0);
        Assert.IsTrue(MemoryVocabulary.Relevance("咖啡","饮食","下午茶推荐")>0);
        Assert.AreEqual(0,MemoryVocabulary.Relevance("咖啡","饮食","今天的天气"));
    }

    private sealed class FakeProvider : IAiChatProvider
    {
        public string ProviderName=>"Fake";
        public string Model=>"fake";
        public bool IsConfigured=>true;
        public int Count {get;private set;}
        public AiChatRequest? Last {get;private set;}
        public bool Fail {get;set;}
        public Task<AiChatResult> CompleteAsync(AiChatRequest request,CancellationToken cancellationToken=default)
        {
            Count++; Last=request;
            if(Fail) throw new AiProviderException("unavailable");
            return Task.FromResult(new AiChatResult{Content="""{"evidence":["我偏爱咖啡"]}"""});
        }
    }
}
