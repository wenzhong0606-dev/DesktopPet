using System;

namespace BichonDesktopPet.Core.Models;

/// <summary>Bounded learned expression tendencies, never the configured base persona.</summary>
public sealed record PersonalityGrowthProfile(bool Enabled = false, int Length = 0,
    int Directness = 0, int Naturalness = 0, int EmojiRestraint = 0)
{
    public bool PreferConciseCare => Enabled && (Length < 0 || Directness > 0);
    public string PromptHint()
    {
        if (!Enabled) return "";
        var hints = new System.Collections.Generic.List<string>();
        if (Length < 0) hints.Add("在不遗漏必要信息的前提下倾向稍简洁");
        if (Length > 0) hints.Add("复杂问题倾向提供稍充分的解释");
        if (Directness > 0) hints.Add("倾向先回答核心问题");
        if (Naturalness > 0) hints.Add("倾向自然表达，减少刻意卖萌");
        if (EmojiRestraint > 0) hints.Add("倾向少用表情符号");
        return string.Join("；", hints);
    }
}

public sealed record PersonalityGrowthChange(long Id, string Axis, int Before, int After,
    DateTimeOffset AtUtc, long SourceMessageId)
{
    public string Display => $"{AtUtc.ToLocalTime():yyyy-MM-dd HH:mm} · {Axis switch
    {
        "length" => "回复详略", "direct" => "直接表达", "natural" => "自然表达",
        "emoji" => "表情克制", _ => "未知维度"
    }} {Before:+0;-0;0} → {After:+0;-0;0} · 消息 #{SourceMessageId}";
}
