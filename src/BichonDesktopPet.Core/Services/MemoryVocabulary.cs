using System;
using System.Collections.Generic;
using System.Linq;

namespace BichonDesktopPet.Core.Services;

/// <summary>Conservative local lexical equivalences, not embedding-based semantic search.</summary>
public static class MemoryVocabulary
{
    private static readonly string[][] Groups =
    [
        ["看书", "阅读", "读书"],
        ["爵士乐", "爵士"],
        ["画画", "绘画"],
        ["骑行", "骑自行车"],
        ["摄影", "拍照"]
    ];

    public static string CanonicalTopic(string topic)
    {
        foreach (var group in Groups)
            if (group.Contains(topic, StringComparer.OrdinalIgnoreCase)) return group[0];
        return topic.ToLowerInvariant();
    }

    public static int Relevance(string topic, string category, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return 0;
        if (query.Contains(topic, StringComparison.OrdinalIgnoreCase))
            return 100 + Math.Min(topic.Length, 20);
        var canonical = CanonicalTopic(topic);
        foreach (var group in Groups)
            if (group[0] == canonical && group.Any(alias => query.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return 90;
        if (category != "未分类" && query.Contains(category, StringComparison.OrdinalIgnoreCase)) return 45;
        var hints = category switch
        {
            "饮食" => new[] { "吃什么", "喝什么", "吃点什么", "喝点什么", "饮料", "推荐食物", "推荐饮品" },
            "音乐" => new[] { "听什么", "推荐音乐", "听点什么", "歌单" },
            "兴趣" => new[] { "兴趣爱好", "平时喜欢做什么", "休闲活动", "有什么爱好" },
            "色彩" => new[] { "颜色", "配色", "色调" },
            _ => Array.Empty<string>()
        };
        return hints.Any(hint => query.Contains(hint, StringComparison.OrdinalIgnoreCase)) ? 30 : 0;
    }
}
