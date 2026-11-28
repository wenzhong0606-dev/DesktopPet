using System;
using System.Collections.Generic;

namespace BichonDesktopPet.Core.Services;

/// <summary>Explicit user feedback only; not a scheduler or inferred routine.</summary>
public static class CareFeedbackExtractor
{
    private static readonly Dictionary<string, MemoryCandidate> Phrases = new(StringComparer.Ordinal);
    static CareFeedbackExtractor()
    {
        Add("关怀·提醒频率","用户希望减少主动提醒","关怀偏好",
            "以后少提醒我一点","我希望你少提醒我","不要太频繁地提醒我","以后不要频繁提醒我");
        Add("关怀·提醒频率","用户希望适当增加主动提醒","关怀偏好",
            "以后可以多提醒我一点","我希望你多提醒我","以后可以多提醒我");
        Add("关怀·提醒方式","用户希望提醒简短","关怀偏好",
            "以后提醒我时简短一点","我希望你的提醒简短一点");
        Add("关怀·休息提醒","用户希望减少休息提醒","关怀偏好",
            "以后少提醒我休息","我希望你少提醒我休息");
        Add("关怀·休息提醒","用户希望适当提醒休息","关怀偏好",
            "以后可以提醒我休息","我希望你提醒我休息");
        Add("关怀·临时免打扰","用户要求暂时不要主动打扰","临时关怀反馈",
            "现在先别打扰我","暂时不要提醒我","我现在很忙先别打扰我");
        Add("关怀·临时免打扰","用户明确允许恢复普通提醒","临时关怀反馈",
            "现在可以提醒我了","现在可以打扰我了");
    }

    private static void Add(string topic,string content,string category,params string[] phrases)
    {
        foreach(var phrase in phrases) Phrases.Add(phrase,new MemoryCandidate(topic,content,category));
    }

    public static MemoryCandidate? Extract(string? text) =>
        Phrases.GetValueOrDefault((text??"").Trim().TrimEnd('。','！','!'));

    public static TimeSpan? Lifetime(string category) =>
        category=="临时关怀反馈" ? TimeSpan.FromHours(2) : null;
}
