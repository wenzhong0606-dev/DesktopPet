using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Produces short, deterministic care messages. No AI request is made for local reminders.
/// </summary>
public static class CareReminderMessageBuilder
{
    public static string BuildBreakReminder(AvatarProfile? avatar)
    {
        var persona = avatar?.Persona;
        var address = string.IsNullOrWhiteSpace(persona?.UserAddress) ? "你" : persona.UserAddress.Trim();
        var style = persona?.SpeechStyle ?? "natural";

        return style switch
        {
            "cute" => $"{address}，已经连续使用电脑一小时啦，先休息一下、活动活动吧！",
            "gentle" => $"{address}，你已经专注一段时间了。起来走走、看看远处，让眼睛休息一下吧。",
            "cool" => $"{address}，连续使用电脑已超过一小时。建议短暂起身活动并休息眼睛。",
            "tsundere" => $"{address}，都坐这么久了，快起来活动一下。别总顾着忙。",
            "playful" => $"{address}，电脑前连续打卡一小时！起来伸个懒腰，给自己充充电吧！",
            _ => $"{address}，已经连续使用电脑一小时了。建议起来活动一下，让眼睛休息一会儿。"
        };
    }
}
