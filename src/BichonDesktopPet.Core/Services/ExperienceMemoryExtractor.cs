using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BichonDesktopPet.Core.Services;

/// <summary>Finite local patterns only. Historical claims remain user-reported,
/// and verified exchanges establish conversation activity, not real-world outcomes.</summary>
public static class ExperienceMemoryExtractor
{
    private static readonly Dictionary<string, string> Milestones = new(StringComparer.Ordinal)
    {
        ["读完了一本书"]="读完一本书", ["看完了一本书"]="读完一本书",
        ["完成了学习计划"]="完成学习计划", ["完成了训练计划"]="完成训练计划",
        ["完成了跑步目标"]="完成跑步目标", ["完成了拼图"]="完成拼图",
        ["完成了绘画作品"]="完成绘画作品", ["学会了游泳"]="学会游泳",
        ["学会了骑自行车"]="学会骑自行车"
    };
    private static readonly Dictionary<string, (string Topic, string Content)> Interaction = new(StringComparer.Ordinal)
    {
        ["希望你回答简洁一点"]=("回复长度","用户希望回复简洁一些"),
        ["希望你回答详细一点"]=("回复长度","用户希望回复详细一些"),
        ["希望你回答直接一点"]=("回答方式","用户希望直接回答问题"),
        ["希望你说话自然一点"]=("表达方式","用户希望表达自然"),
        ["希望你不要卖萌"]=("表达方式","用户希望不要刻意卖萌"),
        ["不喜欢你使用表情符号"]=("表情符号","用户希望少用或不用表情符号"),
        ["希望你少用表情符号"]=("表情符号","用户希望少用或不用表情符号"),
        ["希望你不要使用表情符号"]=("表情符号","用户希望不用表情符号")
    };
    private static readonly Dictionary<string, string> ExchangeTopics = new(StringComparer.Ordinal)
    {
        ["帮我制定学习计划"]="学习计划",
        ["帮我制定阅读计划"]="阅读计划",
        ["帮我制定跑步计划"]="跑步计划",
        ["帮我制定旅行计划"]="旅行计划",
        ["给我推荐几本书"]="书籍推荐",
        ["推荐几本书"]="书籍推荐",
        ["给我推荐一些音乐"]="音乐推荐",
        ["推荐一些音乐"]="音乐推荐",
        ["帮我整理学习思路"]="学习思路"
    };
    private static readonly Regex MilestonePattern = new(
        @"^我(?<when>今天|昨天)?(?<action>[\p{IsCJKUnifiedIdeographs}]{2,20})$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<MemoryCandidate> ExtractUserStatement(string text, DateTimeOffset recordedAt)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0 || normalized.Length > 80) return [];
        if (normalized.StartsWith("我", StringComparison.Ordinal) &&
            Interaction.TryGetValue(normalized[1..], out var interaction))
            return [new MemoryCandidate("交流偏好·" + interaction.Topic, interaction.Content, "交流偏好")];
        var match = MilestonePattern.Match(normalized);
        if (!match.Success || !Milestones.TryGetValue(match.Groups["action"].Value, out var action)) return [];
        var day = recordedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var relative = match.Groups["when"].Value;
        var content = $"用户在 {day}（UTC记录日期）的消息中表示{(relative.Length > 0 ? relative : "已")}{action}。";
        return [new MemoryCandidate($"经历·{action}·{day}", content, "用户经历")];
    }

    public static MemoryCandidate? ExtractExchange(string userText, DateTimeOffset recordedAt)
    {
        if (!ExchangeTopics.TryGetValue(Normalize(userText), out var topic)) return null;
        var day = recordedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new MemoryCandidate($"对话·{topic}·{day}",
            $"在 {day}（UTC记录日期），用户曾就{topic}提出请求，桌宠已返回文字回复。未验证实际任务是否完成。",
            "共同经历");
    }

    private static string Normalize(string? text) => (text ?? "").Trim().TrimEnd('。','！','!');
}
