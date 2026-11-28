using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class AdvancedMemoryQualityTests
{
    [TestMethod]
    public void OpenVocabulary_FixedEvaluationCorpusMeasuresPrecisionRecallAndFalsePositiveRate()
    {
        (string Text,bool Expected)[] corpus=[
            ("我最喜欢的食物是番茄炒蛋",true),
            ("我喜欢的饮料是桂花乌龙",true),
            ("我偏爱的音乐类型是交响乐",true),
            ("我喜欢的运动是壁球",true),
            ("我喜欢的颜色是雾霾蓝",true),
            ("我喜欢的休闲活动是陶艺",true),
            ("假设我喜欢的食物是番茄炒蛋",false),
            ("我喜欢的食物是番茄炒蛋吗",false),
            ("我喜欢的饮料是银行卡密码",false),
            ("我喜欢的颜色是红色或者蓝色",false),
            ("我喜欢的食物是123456",false),
            ("我喜欢的运动是我朋友说的",false),
            ("我今天很累",false),
            ("我喜欢的颜色是我的姓名",false)
        ];
        int tp=0,fp=0,fn=0,tn=0;
        foreach(var item in corpus)
        {
            var actual=OpenPreferenceExtractor.Extract(item.Text).Count>0;
            if(actual&&item.Expected)tp++;
            else if(actual)fp++;
            else if(item.Expected)fn++;
            else tn++;
        }
        var precision=(double)tp/(tp+fp);
        var recall=(double)tp/(tp+fn);
        var falsePositiveRate=(double)fp/(fp+tn);
        Assert.AreEqual(1.0,precision);
        Assert.AreEqual(1.0,recall);
        Assert.AreEqual(0.0,falsePositiveRate);
    }

    [TestMethod]
    public void SemanticQuery_RejectsUnboundedOrUnknownOutput()
    {
        Assert.AreEqual("饮食 咖啡",SemanticMemoryQuery.Validate("""{"category":"饮食","terms":["咖啡"]}"""));
        Assert.AreEqual("",SemanticMemoryQuery.Validate("""{"category":"身份","terms":["咖啡"]}"""));
        Assert.AreEqual("",SemanticMemoryQuery.Validate("""{"category":"饮食","terms":["密码"]}"""));
        Assert.AreEqual("",SemanticMemoryQuery.Validate("""{"category":"饮食","terms":["咖啡"],"extra":1}"""));
        Assert.AreEqual("",SemanticMemoryQuery.Validate("not json"));
        Assert.IsFalse(SemanticMemoryQuery.CanRequest("推荐我的银行卡密码"));
        Assert.IsFalse(SemanticMemoryQuery.CanRequest("今天天气如何"));
    }

    [TestMethod]
    public async Task SemanticQuery_SendsOnlyQuestionAndFallsBackOnFailure()
    {
        var provider=new FakeProvider();
        const string question="推荐适合我的午后饮品";
        Assert.AreEqual("饮食 咖啡",await SemanticMemoryQuery.ExpandAsync(provider,question));
        Assert.AreEqual(2,provider.Last!.Messages.Count);
        Assert.AreEqual(question,provider.Last.Messages[1].Content);
        Assert.AreEqual(100,provider.Last.MaxTokens);
        Assert.IsTrue(provider.Last.JsonObjectResponse);
        provider.Fail=true;
        Assert.AreEqual("",await SemanticMemoryQuery.ExpandAsync(provider,question));
    }

    private sealed class FakeProvider:IAiChatProvider
    {
        public string ProviderName=>"fake";
        public string Model=>"fake";
        public bool IsConfigured=>true;
        public AiChatRequest? Last{get;private set;}
        public bool Fail{get;set;}
        public Task<AiChatResult> CompleteAsync(AiChatRequest request,CancellationToken cancellationToken=default)
        {
            Last=request;
            if(Fail)throw new AiProviderException("offline");
            return Task.FromResult(new AiChatResult{Content="""{"category":"饮食","terms":["咖啡"]}"""});
        }
    }
}
