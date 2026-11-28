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
        ["摄影", "拍照"],
        ["猫", "猫咪"], ["狗", "小狗"], ["冰美式", "冰美式咖啡"]
    ];

    public static string CanonicalTopic(string topic)
    {
        foreach (var group in Groups)
            if (group.Contains(topic, StringComparer.OrdinalIgnoreCase)) return group[0];
        return topic.ToLowerInvariant();
    }

    public static bool IsFollowUp(string query) =>
        query.Trim().TrimEnd('？','?','。','！','!') is
            "那还有呢" or "还有呢" or "还有什么" or "那怎么办" or "继续" or "继续说" or "那你推荐什么" or "还记得吗";

    public static int Relevance(string topic, string category, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return 0;
        if (query.Contains(topic, StringComparison.OrdinalIgnoreCase))
            return 100 + Math.Min(topic.Length, 20);
        var canonical = CanonicalTopic(topic);
        foreach (var group in Groups)
            if (group[0] == canonical && group.Any(alias => query.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return 90;
        if (category is "共同经历" or "用户经历" or "交流偏好")
        {
            // Structured topics have an optional recorded-day suffix, which is not
            // required in natural questions about a prior discussion or milestone.
            var parts = topic.Split('·');
            if (parts.Length >= 2 && query.Contains(parts[1], StringComparison.OrdinalIgnoreCase)) return 95;
            if (category == "用户经历" && parts.Length >= 2 &&
                query.Replace("了", "", StringComparison.Ordinal).Contains(parts[1], StringComparison.OrdinalIgnoreCase)) return 85;
            if (parts.Length >= 2 && parts[1] == "书籍推荐" &&
                (query.Contains("推荐书") || query.Contains("推荐过的书") || query.Contains("推荐过书"))) return 85;
            if (parts.Length >= 2 && parts[1] == "音乐推荐" && query.Contains("推荐过的音乐")) return 85;
            if (category == "交流偏好" && parts.Length >= 2)
            {
                if (parts[1] == "回复长度" && (query.Contains("回答简洁") || query.Contains("回答详细") || query.Contains("回复风格"))) return 85;
                if (parts[1] == "回答方式" && query.Contains("怎么回答")) return 85;
                if (parts[1] == "表达方式" && (query.Contains("说话方式") || query.Contains("表达风格"))) return 85;
            }
        }
        if (category is "临时状态" or "近期计划" or "关怀偏好" or "临时关怀反馈")
        {
            var parts = topic.Split('·');
            if (category == "近期计划" && parts.Length == 2 &&
                (query.Contains(parts[1]) || query.Contains("计划") || query.Contains("打算"))) return 85;
            if (category == "临时状态" && parts.Length == 2 &&
                ((parts[1] == "疲劳自述" && (query.Contains("累") || query.Contains("疲劳"))) ||
                 (parts[1] == "忙碌自述" && query.Contains("忙")))) return 85;
            if (category == "关怀偏好" && query.Contains("提醒")) return 70;
            if (category == "临时关怀反馈" && (query.Contains("打扰") || query.Contains("免打扰") || query.Contains("提醒"))) return 85;
        }
        if (category != "未分类" && query.Contains(category, StringComparison.OrdinalIgnoreCase)) return 45;
        var hints = category switch
        {
            "饮食" => new[] { "吃什么", "喝什么", "吃点什么", "喝点什么", "饮料", "推荐食物", "推荐饮品", "午餐吃", "晚餐吃", "喝杯什么", "想喝点", "下午茶" },
            "音乐" => new[] { "听什么", "推荐音乐", "听点什么", "歌单" },
            "兴趣" => new[] { "兴趣爱好", "平时喜欢做什么", "休闲活动", "有什么爱好", "闲暇时间", "周末做什么", "打发时间" },
            "色彩" => new[] { "颜色", "配色", "色调" },
            "共同经历" => new[] { "我们聊过", "一起聊过", "之前讨论过", "上次聊", "曾经讨论", "共同回忆" },
            "用户经历" => new[] { "我的经历", "我完成过", "我之前完成", "我的成就" },
            "交流偏好" => new[] { "交流习惯", "我希望你怎么", "我喜欢你怎么回答", "我的交流偏好" },
            _ => Array.Empty<string>()
        };
        return hints.Any(hint => query.Contains(hint, StringComparison.OrdinalIgnoreCase)) ? 30 : 0;
    }
}
