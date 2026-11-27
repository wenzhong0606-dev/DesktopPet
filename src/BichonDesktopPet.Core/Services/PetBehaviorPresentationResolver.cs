using System;
using System.Collections.Generic;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Resolves behavior state into deterministic, persona-aware text and a host-renderable effect.
/// This is a local template system, not an AI language model.
/// </summary>
public sealed class PetBehaviorPresentationResolver
{
    private readonly Dictionary<string, int> _messageIndexes = new(StringComparer.Ordinal);

    /// <summary>
    /// Compatibility overload for callers that only have species metadata.
    /// </summary>
    public PetBehaviorPresentation Resolve(PetBehaviorState state, string? species)
    {
        var normalizedSpecies = string.IsNullOrWhiteSpace(species)
            ? "custom"
            : species.Trim().ToLowerInvariant();

        return new PetBehaviorPresentation(
            state,
            ResolveVisualEffect(state),
            ResolveLegacyMessage(state, normalizedSpecies));
    }

    /// <summary>
    /// Resolves a response using the selected avatar's independent identity and persona.
    /// Repeated calls rotate through a bounded template set for that avatar and state.
    /// </summary>
    public PetBehaviorPresentation Resolve(PetBehaviorState state, AvatarProfile? avatar)
    {
        var species = avatar?.Species?.Trim();
        species = string.IsNullOrWhiteSpace(species) ? "custom" : species.ToLowerInvariant();
        var persona = PersonaProfileValidator.NormalizeOrDefault(avatar?.Persona);
        var templates = BuildTemplates(state, species, persona);
        var key = $"{avatar?.Id ?? "unknown"}:{state}";
        var message = NextTemplate(key, templates)
            .Replace("{self}", persona.SelfReference, StringComparison.Ordinal)
            .Replace("{user}", persona.UserAddress, StringComparison.Ordinal);

        if (persona.ResponseLength == "medium")
        {
            message = $"{message} {GetFollowUp(state)}";
        }

        var emoji = GetEmoji(state);
        if (persona.EmojiLevel == "high" ||
            (persona.EmojiLevel == "medium" && state == PetBehaviorState.Excited))
        {
            message += " " + emoji;
        }

        return new PetBehaviorPresentation(state, ResolveVisualEffect(state), message);
    }

    private static PetVisualEffect ResolveVisualEffect(PetBehaviorState state) => state switch
    {
        PetBehaviorState.Greeting => PetVisualEffect.GreetingBounce,
        PetBehaviorState.Happy => PetVisualEffect.HappyBounce,
        PetBehaviorState.Excited => PetVisualEffect.ExcitedWiggle,
        PetBehaviorState.Sleepy => PetVisualEffect.SleepySway,
        _ => PetVisualEffect.IdleBreathing
    };

    private static string ResolveLegacyMessage(PetBehaviorState state, string species) =>
        (state, species) switch
        {
            (PetBehaviorState.Idle, "dog") => "汪～我在这里",
            (PetBehaviorState.Idle, "cat") => "喵～我在这里",
            (PetBehaviorState.Greeting, "dog") => "汪！欢迎回来！",
            (PetBehaviorState.Greeting, "cat") => "喵！你回来啦",
            (PetBehaviorState.Happy, "dog") => "嘿嘿，摸摸我！",
            (PetBehaviorState.Happy, "cat") => "呼噜呼噜～",
            (PetBehaviorState.Happy, "rabbit") => "蹦蹦！好开心",
            (PetBehaviorState.Excited, "dog") => "汪汪！好开心！",
            (PetBehaviorState.Excited, "cat") => "喵！好开心！",
            (PetBehaviorState.Excited, "rabbit") => "太开心啦！",
            (PetBehaviorState.Sleepy, "cat") => "我先打个盹...",
            (PetBehaviorState.Sleepy, _) => "我有点困啦...",
            (PetBehaviorState.Idle, _) => "我在这里陪着你",
            (PetBehaviorState.Greeting, _) => "欢迎回来！",
            (PetBehaviorState.Happy, _) => "嘿嘿，好开心！",
            (PetBehaviorState.Excited, _) => "耶！太棒啦！",
            _ => "我在这里陪着你"
        };

    private static List<string> BuildTemplates(
        PetBehaviorState state,
        string species,
        PersonaProfile persona)
    {
        var address = "{user}";
        var self = "{self}";
        var isAffectionate = HasTrait(persona, "黏人", "亲人", "爱撒娇", "affectionate", "clingy");
        var isShy = HasTrait(persona, "害羞", "胆小", "shy", "timid");
        var isIndependent = HasTrait(persona, "独立", "高冷", "independent", "aloof");
        var isPlayful = HasTrait(persona, "活泼", "调皮", "好奇", "playful", "lively", "curious");

        if (persona.SpeechStyle == "cool" || isIndependent)
        {
            List<string> styleTemplates = state switch
            {
                PetBehaviorState.Idle =>
                [
                    $"{self}在。{address}，有事叫我。",
                    "我先安静待着，不用一直管我。",
                    $"嗯，{address}，我在这里。"
                ],
                PetBehaviorState.Greeting =>
                [
                    $"{address}，回来了？",
                    "嗯，知道你回来了。",
                    "今天还顺利吗？"
                ],
                PetBehaviorState.Happy =>
                [
                    "好吧，再摸一下。",
                    "嗯，这样还不错。",
                    "今天表现不错，允许你继续摸摸。"
                ],
                PetBehaviorState.Excited =>
                [
                    "咳……其实我也挺开心的。",
                    "好吧，今天就陪你多玩一会儿。",
                    "这次互动还算有趣。"
                ],
                PetBehaviorState.Sleepy =>
                [
                    "我休息一会儿，别吵我。",
                    "有点困了，晚点再陪你。",
                    "先眯一会儿。"
                ],
                _ => ["我在。"]
            };
            AddSharedPersonaTemplates(styleTemplates, state, persona);
            return styleTemplates;
        }

        if (persona.SpeechStyle == "tsundere")
        {
            List<string> styleTemplates = state switch
            {
                PetBehaviorState.Idle =>
                [
                    "我才不是在等你呢。",
                    "哼，我只是刚好在这里。",
                    $"{address}，有事就说。"
                ],
                PetBehaviorState.Greeting =>
                [
                    "才不是特意等你呢……不过你回来就好。",
                    $"哼，{address}，今天怎么这么晚。",
                    "我才没有想你，只是打个招呼。"
                ],
                PetBehaviorState.Happy =>
                [
                    "才不是因为你摸我才开心的。",
                    "嗯……再摸一下也不是不行。",
                    "哼，今天就勉强让你摸摸。"
                ],
                PetBehaviorState.Excited =>
                [
                    "才、才没有兴奋过头呢！",
                    "今天就陪你玩久一点好了。",
                    "你可别误会，我只是心情不错。"
                ],
                PetBehaviorState.Sleepy =>
                [
                    "我才不是困了，只是闭眼休息。",
                    "先睡一小会儿，不许偷看。",
                    "哼，等我休息好了再理你。"
                ],
                _ => ["哼。"]
            };
            AddSharedPersonaTemplates(styleTemplates, state, persona);
            return styleTemplates;
        }

        if (persona.SpeechStyle == "gentle" || isShy)
        {
            List<string> styleTemplates = state switch
            {
                PetBehaviorState.Idle =>
                [
                    $"{self}会安静地陪着{address}。",
                    $"如果需要陪伴，{address}，随时叫我。",
                    "这里很安静，待在一起就很好。"
                ],
                PetBehaviorState.Greeting =>
                [
                    $"欢迎回来，{address}。{self}很高兴见到你。",
                    "你回来啦，我一直在这里。",
                    $"今天辛苦了，{address}。"
                ],
                PetBehaviorState.Happy =>
                [
                    "这样轻轻摸摸，好舒服呀。",
                    $"{self}现在觉得很幸福。",
                    $"谢谢{address}，我很喜欢这样的互动。"
                ],
                PetBehaviorState.Excited =>
                [
                    "呀，今天真的特别开心。",
                    $"{address}在身边，感觉真好。",
                    "我好像有一点点兴奋了。"
                ],
                PetBehaviorState.Sleepy =>
                [
                    "我有一点困了，先休息一下哦。",
                    "陪着你很安心，我想打个盹。",
                    "晚安一小会儿，等下再见。"
                ],
                _ => ["我在这里。"]
            };
            AddSharedPersonaTemplates(styleTemplates, state, persona);
            return styleTemplates;
        }

        var interjection = species switch
        {
            "dog" => "汪",
            "cat" => "喵",
            "rabbit" => "蹦蹦",
            _ => "嘿"
        };

        var templates = state switch
        {
            PetBehaviorState.Idle => new List<string>
            {
                $"{self}在这里陪着{address}。",
                $"{interjection}～{address}，需要{self}的时候记得叫我。",
                $"今天也一起待一会儿吧，{address}。"
            },
            PetBehaviorState.Greeting => new List<string>
            {
                $"{interjection}！{address}回来啦！",
                $"欢迎回来，{address}！{self}一直在这里。",
                $"{address}，今天过得怎么样？"
            },
            PetBehaviorState.Happy => new List<string>
            {
                $"{address}，再摸摸{self}嘛～",
                $"{self}好开心！",
                "嘿嘿，这样的互动最喜欢啦！"
            },
            PetBehaviorState.Excited => new List<string>
            {
                $"{interjection}{interjection}！{self}开心得停不下来啦！",
                $"耶！{address}最好啦！",
                "今天也太开心了吧！"
            },
            PetBehaviorState.Sleepy => new List<string>
            {
                $"呼……{address}，{self}先眯一会儿。",
                $"{self}有点困啦，等下再陪你。",
                "眼睛快要睁不开啦……"
            },
            _ => new List<string> { $"{self}在这里陪着{address}。" }
        };

        if (isAffectionate && state is PetBehaviorState.Idle or PetBehaviorState.Greeting)
        {
            templates.Add($"其实{self}一直都想黏着{address}。");
        }

        if (isPlayful && state is PetBehaviorState.Happy or PetBehaviorState.Excited)
        {
            templates.Add($"嘿！{address}，我们再玩一次！");
        }

        AddSharedPersonaTemplates(templates, state, persona);

        if (persona.SpeechStyle == "playful" && state is PetBehaviorState.Happy or PetBehaviorState.Excited)
        {
            templates.Add($"嘿嘿，抓到{address}啦！");
        }

        return templates;
    }

    private static void AddSharedPersonaTemplates(
        List<string> templates,
        PetBehaviorState state,
        PersonaProfile persona)
    {
        if (state == PetBehaviorState.Greeting)
        {
            foreach (var nickname in persona.Nicknames)
            {
                templates.Add($"你也可以叫我{nickname}，{{user}}！");
            }
        }

        if (state is PetBehaviorState.Happy or PetBehaviorState.Excited)
        {
            templates.AddRange(persona.Catchphrases);
        }
    }

    private string NextTemplate(string key, IReadOnlyList<string> templates)
    {
        if (templates.Count == 0)
        {
            return "我在这里陪着你。";
        }

        var index = _messageIndexes.TryGetValue(key, out var previousIndex)
            ? previousIndex % templates.Count
            : 0;
        _messageIndexes[key] = index + 1;
        return templates[index];
    }

    private static bool HasTrait(PersonaProfile persona, params string[] values)
    {
        foreach (var trait in persona.PersonalityTraits)
        {
            foreach (var value in values)
            {
                if (string.Equals(trait, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetFollowUp(PetBehaviorState state) => state switch
    {
        PetBehaviorState.Idle => "需要我时叫我就好。",
        PetBehaviorState.Greeting => "今天过得怎么样？",
        PetBehaviorState.Happy => "再陪我玩一会儿吧。",
        PetBehaviorState.Excited => "我们再玩一次吧！",
        PetBehaviorState.Sleepy => "我休息一下就好。",
        _ => string.Empty
    };

    private static string GetEmoji(PetBehaviorState state) => state switch
    {
        PetBehaviorState.Happy => "💗",
        PetBehaviorState.Excited => "✨",
        PetBehaviorState.Sleepy => "😴",
        PetBehaviorState.Greeting => "🐾",
        _ => "🌸"
    };
}
