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
        ["阅读"]="兴趣", ["看书"]="兴趣", ["读书"]="兴趣", ["绘画"]="兴趣", ["骑自行车"]="兴趣", ["拍照"]="兴趣", ["画画"]="兴趣", ["摄影"]="兴趣",
        ["跑步"]="兴趣", ["散步"]="兴趣", ["游泳"]="兴趣", ["骑行"]="兴趣",
        ["打羽毛球"]="兴趣", ["打篮球"]="兴趣", ["游戏"]="兴趣", ["旅行"]="兴趣",
        ["猫"]="兴趣", ["狗"]="兴趣", ["猫咪"]="兴趣", ["小狗"]="兴趣",
        ["科幻电影"]="兴趣", ["喜剧电影"]="兴趣", ["动漫"]="兴趣", ["拼图"]="兴趣",
        ["纪录片"]="兴趣", ["悬疑电影"]="兴趣", ["爬山"]="兴趣", ["徒步"]="兴趣",
        ["烘焙"]="兴趣", ["园艺"]="兴趣", ["编程"]="兴趣", ["桌游"]="兴趣",
        ["红色"]="色彩", ["蓝色"]="色彩", ["绿色"]="色彩", ["紫色"]="色彩",
        ["黑色"]="色彩", ["白色"]="色彩", ["黄色"]="色彩", ["粉色"]="色彩",
        ["橙色"]="色彩", ["灰色"]="色彩", ["米色"]="色彩", ["浅蓝色"]="色彩"
    };

    private static readonly Regex Clause = Pattern(
        @"^(?<subject>我)?(?:现在|如今|平常|平时|一直|比较|很|特别|最|更|其实|还是){0,3}(?<attitude>不再喜欢|不太喜欢|不喜欢|喜欢|爱好|爱)(?:的是)?(?<item>[\p{IsCJKUnifiedIdeographs}、和及]{1,42}?)了?$");
    private static readonly Regex Favorite = Pattern(
        @"^我(?:现在|一直)?最?喜欢的(?:食物|饮料|音乐|颜色|运动|爱好)是(?<item>[\p{IsCJKUnifiedIdeographs}]{1,12})$");
    private static readonly Regex Interest = Pattern(
        @"^我(?:现在|一直)?对(?<item>[\p{IsCJKUnifiedIdeographs}]{1,12}?)(?<attitude>不太感兴趣|不感兴趣|很感兴趣|特别感兴趣|感兴趣)$");
    private static readonly Regex NaturalPreference = Pattern(
        @"^我(?:平时|一直|特别|很|比较|现在|更){0,3}(?<attitude>偏爱|钟爱|热衷于|不爱|不太爱|讨厌)(?<item>[\p{IsCJKUnifiedIdeographs}]{1,14})$");
    private static readonly Regex Enthusiasm = Pattern(
        @"^我(?:一直|现在)?对(?<item>[\p{IsCJKUnifiedIdeographs}]{1,12}?)(?<attitude>情有独钟|很着迷|特别着迷|不太感冒|不感冒)$");
    private static readonly Regex Past = Pattern(
        @"^我以前(?:很|比较)?喜欢(?<item>[\p{IsCJKUnifiedIdeographs}]{1,13})$");
    private static readonly Regex EllipticalCorrection = Pattern(
        @"^(?:但|不过)?(?:我)?(?:现在|如今)(?<attitude>不再喜欢|不太喜欢|不喜欢)了?$");

    private static Regex Pattern(string pattern) =>
        new(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<MemoryCandidate> Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 180) return [];
        var normalized = text.Trim().TrimEnd('。','！','!');
        // A complete declarative message is required; never mine quotes, questions,
        // mixed sensitive text, or arbitrary free-form clauses.
        var clauses = normalized.Split(['，', ',', '；', ';', '。'], StringSplitOptions.TrimEntries);
        if (clauses.Length is < 1 or > 3) return [];
        var results = new List<MemoryCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? pastItem = null;
        for (var i = 0; i < clauses.Length; i++)
        {
            var clause = clauses[i];
            if (i == 0 && clauses.Length == 2)
            {
                var past = Past.Match(clause);
                if (past.Success)
                {
                    if (!TryResolve(past.Groups["item"].Value, out pastItem, out _)) return [];
                    continue; // historical preference is not a current fact
                }
            }
            if (i > 0)
            {
                foreach (var connector in new[] { "但是", "不过", "而且", "并且", "但", "也" })
                    if (clause.StartsWith(connector, StringComparison.Ordinal))
                    { clause = clause[connector.Length..]; break; }
            }
            var match = Clause.Match(clause);
            var attitude = "";
            var itemList = "";
            if (pastItem is not null && EllipticalCorrection.Match(clause) is { Success: true } correction)
            {
                attitude = NormalizeAttitude(correction.Groups["attitude"].Value);
                itemList = pastItem;
            }
            else if (pastItem is null && Favorite.Match(clause) is { Success: true } favorite)
            {
                attitude = "喜欢";
                itemList = favorite.Groups["item"].Value;
            }
            else if (pastItem is null && Interest.Match(clause) is { Success: true } interest)
            {
                attitude = interest.Groups["attitude"].Value switch
                {
                    "不太感兴趣" => "不太感兴趣",
                    "不感兴趣" => "不感兴趣",
                    _ => "感兴趣"
                };
                itemList = interest.Groups["item"].Value;
            }
            else if (pastItem is null && NaturalPreference.Match(clause) is { Success: true } natural)
            {
                attitude = natural.Groups["attitude"].Value switch
                {
                    "不爱" or "讨厌" => "不喜欢", "不太爱" => "不太喜欢", _ => "喜欢"
                };
                itemList = natural.Groups["item"].Value;
            }
            else if (pastItem is null && Enthusiasm.Match(clause) is { Success: true } enthusiasm)
            {
                attitude = enthusiasm.Groups["attitude"].Value switch
                {
                    "不感冒" => "不喜欢", "不太感冒" => "不太喜欢", _ => "喜欢"
                };
                itemList = enthusiasm.Groups["item"].Value;
            }
            else if (match.Success && (i > 0 || match.Groups["subject"].Success))
            {
                attitude = NormalizeAttitude(match.Groups["attitude"].Value);
                itemList = match.Groups["item"].Value;
                if (pastItem is not null && !clause.StartsWith("现在", StringComparison.Ordinal) &&
                    !clause.StartsWith("如今", StringComparison.Ordinal) &&
                    !clause.StartsWith("我现在", StringComparison.Ordinal) &&
                    !clause.StartsWith("我如今", StringComparison.Ordinal)) return [];
            }
            else return [];

            var items = Regex.Split(itemList, "以及|、|和|及");
            foreach (var rawItem in items)
            {
                if (!TryResolve(rawItem, out var item, out var category) ||
                    !seen.Add(item) || results.Count >= 4) return [];
                var content = attitude.EndsWith("感兴趣", StringComparison.Ordinal)
                    ? $"用户对{item}{attitude}" : $"用户{attitude}{item}";
                results.Add(new MemoryCandidate(item, content, category));
            }
        }
        return results;
    }

    private static string NormalizeAttitude(string value) => value switch
    {
        "不再喜欢" or "不喜欢" => "不喜欢",
        "不太喜欢" => "不太喜欢",
        _ => "喜欢"
    };

    private static bool TryResolve(string input, out string item, out string category)
    {
        item = input;
        if (KnownTopics.TryGetValue(item, out var direct))
        { category = direct; item = MemoryVocabulary.CanonicalTopic(item); return true; }
        if (item.Length >= 2 && "喝吃听看".Contains(item[0]))
        {
            item = item[1..];
            if (KnownTopics.TryGetValue(item, out var stripped))
            { category = stripped; item = MemoryVocabulary.CanonicalTopic(item); return true; }
        }
        category = "";
        return false;
    }
}
