using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

public static class CareReminderMessageBuilder
{
    public static string BuildBreakReminder(AvatarProfile? avatar, PersonalityGrowthProfile? growth = null)
    {
        var persona = avatar?.Persona;
        var address = string.IsNullOrWhiteSpace(persona?.UserAddress) ? "你" : persona.UserAddress.Trim();

        if (growth?.PreferConciseCare == true)
            return persona?.SpeechStyle switch
            {
                "cute" => $"{address}，连续用电脑一小时啦，起来活动一下，休息眼睛吧！",
                "gentle" => $"{address}，已经连续用电脑一小时了，起来走走，休息眼睛吧。",
                "tsundere" => $"{address}，都一小时了，快起来活动一下，休息眼睛。",
                "playful" => $"{address}，电脑前一小时打卡！起来活动一下，休息眼睛吧。",
                _ => $"{address}，连续使用电脑约一小时，建议起身活动并休息眼睛。"
            };

        return persona?.SpeechStyle switch
        {
            "cute" => $"{address}，已经连续使用电脑一小时啦，先休息一下、活动活动吧！",
            "gentle" => $"{address}，你已经专注一段时间了。起来走走、看看远处，让眼睛休息一下吧。",
            "cool" => $"{address}，连续使用电脑已超过一小时。建议短暂起身活动并休息眼睛。",
            "tsundere" => $"{address}，都坐这么久了，快起来活动一下。别总顾着忙。",
            "playful" => $"{address}，电脑前连续打卡一小时！起来伸个懒腰，给自己充充电吧！",
            _ => $"{address}，已经连续使用电脑一小时了。建议起来活动一下，让眼睛休息一会儿。"
        };
    }

    public static string BuildWeatherReminder(AvatarProfile? avatar, CareLocation location, WeatherCareAlert alert,
        PersonalityGrowthProfile? growth = null)
    {
        var persona = avatar?.Persona;
        var address = string.IsNullOrWhiteSpace(persona?.UserAddress) ? "你" : persona.UserAddress.Trim();
        var suggestion = alert.Type switch
        {
            CareReminderType.ColdWeather => $"{location.DisplayName}气温偏低或即将明显下降，出门记得添件衣服。",
            CareReminderType.RainExpected => $"{location.DisplayName}未来几小时降雨概率较高，出门记得带伞。",
            CareReminderType.HeatWarning => $"{location.DisplayName}体感温度较高，记得补水并注意防暑。",
            _ => alert.Summary
        };

        var message = persona?.SpeechStyle switch
        {
            "cool" => $"{address}，天气提示：{suggestion}",
            "tsundere" => $"{address}，{suggestion}，别等不舒服了才想起来。",
            "playful" => $"{address}，天气小提醒！{suggestion}",
            "gentle" => $"{address}，提醒你一下：{suggestion}",
            _ => $"{address}，{suggestion}"
        };

        if (growth?.PreferConciseCare == true)
            message = persona?.SpeechStyle switch
            {
                "playful" => $"{address}，天气小提醒！{suggestion}",
                "gentle" => $"{address}，提醒一下：{suggestion}",
                _ => $"{address}，{suggestion}"
            };

        var locationSource = string.Equals(location.Source, "ip", System.StringComparison.Ordinal)
            ? "DB-IP https://db-ip.com/"
            : "GeoNames https://www.geonames.org/";
        return $"{message}（天气：Open-Meteo https://open-meteo.com/；位置：{locationSource}；数据许可：CC BY 4.0 https://creativecommons.org/licenses/by/4.0/）";
    }
}
