using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public sealed class AppRuleEngine
{
    private static readonly AppRule[] Rules =
    [
        new("微信", ["WeChat", "Weixin"], ["微信"]),
        new("企业微信", ["WXWork"], ["企业微信", "WeCom"]),
        new("QQ", ["QQ", "TIM"], ["QQ", "TIM"]),
        new("钉钉", ["DingTalk", "DingDing"], ["钉钉", "DingTalk"]),
        new("飞书", ["Feishu", "Lark"], ["飞书", "Lark"])
    ];

    public bool ShouldProtect(WindowSnapshot window)
    {
        if (string.IsNullOrWhiteSpace(window.Title))
        {
            return false;
        }

        return Rules.Any(rule =>
            rule.ProcessNames.Any(process => Matches(window.ProcessName, process)) ||
            rule.TitleKeywords.Any(keyword => window.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool Matches(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
