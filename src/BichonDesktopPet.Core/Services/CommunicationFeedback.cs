using System;
using System.Collections.Generic;

namespace BichonDesktopPet.Core.Services;

/// <summary>Shared explicit feedback vocabulary for memory and growth; no inferred sentiment.</summary>
public static class CommunicationFeedback
{
    public sealed record Feedback(string Axis, int Direction, string Topic, string Content);
    private static readonly Dictionary<string, Feedback> Phrases = new(StringComparer.Ordinal);
    static CommunicationFeedback()
    {
        Add(new("length",-1,"回复长度","用户希望回复简洁一些"),
            "我希望你回答简洁一点","我希望你回答简短一点","我更喜欢简洁的回答","我喜欢你简明扼要地回答","请你以后回答简洁一些");
        Add(new("length",1,"回复长度","用户希望回复详细一些"),
            "我希望你回答详细一点","我希望你多解释一些","我更喜欢详细的回答","请你以后回答详细一些");
        Add(new("direct",1,"回答方式","用户希望直接回答问题"),
            "我希望你回答直接一点","我希望你先说重点","我喜欢你直接回答问题","请你以后先说重点");
        Add(new("natural",1,"表达方式","用户希望表达自然"),
            "我希望你说话自然一点","我喜欢自然一点的表达","请你以后自然一点");
        Add(new("natural",1,"表达方式","用户希望不要刻意卖萌"),
            "我希望你不要卖萌","我不喜欢你刻意卖萌","请你以后不要刻意卖萌");
        Add(new("emoji",1,"表情符号","用户希望少用或不用表情符号"),
            "我不喜欢你使用表情符号","我希望你少用表情符号","我希望你少发 emoji","我希望你少发emoji","请你以后少用表情符号");
        Add(new("emoji",1,"表情符号","用户希望不用表情符号"),
            "我希望你不要使用表情符号","我希望你不要发emoji","请你以后不要使用表情符号");
    }
    private static void Add(Feedback feedback, params string[] phrases)
    {
        foreach (var phrase in phrases) Phrases.Add(phrase, feedback);
    }
    public static Feedback? Recognize(string? text) =>
        Phrases.GetValueOrDefault((text ?? "").Trim().TrimEnd('。','！','!'));
}
