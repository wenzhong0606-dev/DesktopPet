using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BichonDesktopPet.Core.Services;

public sealed record MemoryCandidate(string Topic, string Content, string Category);

/// <summary>Deliberately narrow local extraction: explicit first-person preferences, ordinary vocabulary only.
/// Not a general-purpose classifier or a sensitive-data detector for arbitrary text.</summary>
public static class LocalMemoryExtractor
{
    private static readonly Dictionary<string, string> KnownTopics = new(StringComparer.Ordinal)
    {
        ["冰美式"]="饮食", ["美式咖啡"]="饮食", ["咖啡"]="饮食", ["奶茶"]="饮食",
        ["茶"]="饮食", ["绿茶"]="饮食", ["红茶"]="饮食", ["拿铁"]="饮食",
        ["面条"]="饮食", ["米饭"]="饮食", ["火锅"]="饮食", ["饺子"]="饮食",
        ["披萨"]="饮食", ["巧克力"]="饮食", ["苹果"]="饮食", ["草莓"]="饮食",
        ["爵士乐"]="音乐", ["爵士"]="音乐", ["古典音乐"]="音乐", ["摇滚乐"]="音乐",
        ["流行音乐"]="音乐", ["轻音乐"]="音乐", ["钢琴曲"]="音乐",
        ["阅读"]="兴趣", ["看书"]="兴趣", ["画画"]="兴趣", ["摄影"]="兴趣",
        ["跑步"]="兴趣", ["散步"]="兴趣", ["游泳"]="兴趣", ["骑行"]="兴趣",
        ["打羽毛球"]="兴趣", ["打篮球"]="兴趣", ["游戏"]="兴趣", ["旅行"]="兴趣",
        ["猫"]="兴趣", ["狗"]="兴趣", ["猫咪"]="兴趣", ["小狗"]="兴趣",
        ["科幻电影"]="兴趣", ["喜剧电影"]="兴趣", ["动漫"]="兴趣", ["拼图"]="兴趣"
    };

    private static readonly Regex Clause = new(
        @"^(?<subject>我)?(?:一直|平时|比较|很|特别|最){0,2}(?<attitude>不喜欢|喜欢|爱好|爱)(?<item>[\p{IsCJKUnifiedIdeographs}]{1,12})$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<MemoryCandidate> Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 180) return [];
        var normalized = text.Trim().TrimEnd('。','！','!');
        var clauses = normalized.Split(['，', ',', '；', ';'], StringSplitOptions.TrimEntries);
        if (clauses.Length is < 1 or > 3) return [];
        var results = new List<MemoryCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < clauses.Length; i++)
        {
            var match = Clause.Match(clauses[i]);
            if (!match.Success || (i == 0 && !match.Groups["subject"].Success)) return [];
            var item = match.Groups["item"].Value;
            if (!KnownTopics.TryGetValue(item, out var category))
            {
                if (item.Length < 2 || !"喝吃听看".Contains(item[0])) return [];
                item = item[1..];
                if (!KnownTopics.TryGetValue(item, out category)) return [];
            }
            if (!seen.Add(item)) return [];
            var attitude = match.Groups["attitude"].Value == "不喜欢" ? "不喜欢" : "喜欢";
            results.Add(new MemoryCandidate(item, $"用户{attitude}{item}", category));
        }
        return results;
    }
}
