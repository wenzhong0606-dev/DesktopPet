using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BichonDesktopPet.Core.Services;

/// <summary>Open vocabulary within explicit ordinary preference categories, not arbitrary personal facts.
/// Optional because unknown nouns cannot be guaranteed non-sensitive.</summary>
public static class OpenPreferenceExtractor
{
    private static readonly Regex Pattern = new(
        @"^我(?:现在|平时|一直)?(?:最)?(?<polarity>不喜欢|喜欢|偏爱)的(?<category>食物|饮料|音乐类型|运动|休闲活动|颜色)是(?<topic>[\p{IsCJKUnifiedIdeographs}]{2,12})$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly string[] Blocked =
    [
        "密码","账号","账户","证件","身份证","电话","住址","地址","姓名","名字","秘密","隐私",
        "银行卡","验证码","密钥","口令","公司","单位","学校","医院","疾病","药","病","诊断",
        "宗教","政治","党","性","暴力","毒品","自杀","自残","未成年","儿童","裸","色情",
        "假设","假如","如果","可能","也许","以前","曾经","不是","不再","朋友","别人","他说","她说",
        "但是","不过","或者","以及","和","与","然后","因为","所以","其实","了","吗","呢"
    ];

    public static IReadOnlyList<MemoryCandidate> Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 80) return [];
        var match = Pattern.Match(text.Trim().TrimEnd('。','！','!'));
        if (!match.Success) return [];
        var topic=match.Groups["topic"].Value;
        if (Blocked.Any(word=>topic.Contains(word,StringComparison.Ordinal))) return [];
        var category=match.Groups["category"].Value switch
        {
            "食物" or "饮料"=>"饮食", "音乐类型"=>"音乐", "颜色"=>"色彩", _=>"兴趣"
        };
        var polarity=match.Groups["polarity"].Value=="不喜欢"?"不喜欢":"喜欢";
        return [new MemoryCandidate(MemoryVocabulary.CanonicalTopic(topic),$"用户{polarity}{topic}",category)];
    }
}
