using System;
using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class MemoryPrivacyAndCorpusTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void LocalPreferenceCorpus_ReportsExactCandidateMetrics()
    {
        (string Input, string[] Expected)[] corpus =
        [
            ("我钟爱古典音乐", ["古典音乐:用户喜欢古典音乐"]),
            ("我对游泳不太感兴趣", ["游泳:用户对游泳不太感兴趣"]),
            ("我最喜欢的颜色是紫色", ["紫色:用户喜欢紫色"]),
            ("我喜欢火锅，也喜欢饺子", ["火锅:用户喜欢火锅","饺子:用户喜欢饺子"]),
            ("我讨厌奶茶", ["奶茶:用户不喜欢奶茶"]),
            ("我以前喜欢咖啡，如今不喜欢了", ["咖啡:用户不喜欢咖啡"]),
            ("我今天想喝奶茶", []),
            ("我妈妈喜欢紫色", []),
            ("我可能喜欢游泳", []),
            ("我喜欢火锅吗", []),
            ("假如我喜欢饺子", []),
            ("我喜欢咖啡，密码是测试", []),
            ("我以前喜欢奶茶", []),
            ("我喜欢紫色，但不喜欢紫色", []),
            ("我喜欢火锅或者饺子", []),
            ("我对咖啡没有特别的偏好", [])
        ];
        int tp=0,fp=0,fn=0,negative=0,falsePositiveMessages=0;
        foreach(var row in corpus)
        {
            var actual=LocalMemoryExtractor.Extract(row.Input)
                .Select(x=>$"{x.Topic}:{x.Content}").ToArray();
            tp+=actual.Intersect(row.Expected).Count();
            fp+=actual.Except(row.Expected).Count();
            fn+=row.Expected.Except(actual).Count();
            if(row.Expected.Length==0)
            {
                negative++;
                if(actual.Length>0)falsePositiveMessages++;
            }
        }
        TestContext.WriteLine($"TP={tp} FP={fp} FN={fn}; precision={(double)tp/Math.Max(1,tp+fp):P2}; recall={(double)tp/Math.Max(1,tp+fn):P2}; negative-message FPR={(double)falsePositiveMessages/negative:P2}");
        Assert.AreEqual(0,fp);
        Assert.AreEqual(0,fn);
    }

    [DataTestMethod]
    [DataRow("推荐适合我的银行卡")]
    [DataRow("推荐适合我的账户")]
    [DataRow("推荐适合我的邮箱")]
    [DataRow("推荐适合我的手机号")]
    [DataRow("推荐适合我的验证码")]
    [DataRow("推荐适合我的密钥")]
    [DataRow("推荐适合我的口令")]
    public void SemanticRequest_RejectsCredentialAndContactTopics(string query)
        => Assert.IsFalse(SemanticMemoryQuery.CanRequest(query));

    [DataTestMethod]
    [DataRow("银行卡")]
    [DataRow("账户")]
    [DataRow("邮箱")]
    [DataRow("手机号")]
    [DataRow("验证码")]
    [DataRow("密钥")]
    [DataRow("口令")]
    public void SemanticOutput_RejectsCredentialAndContactTerms(string term)
        => Assert.AreEqual("",SemanticMemoryQuery.Validate(
            System.Text.Json.JsonSerializer.Serialize(new { category="兴趣",terms=new[]{term} })));
}
