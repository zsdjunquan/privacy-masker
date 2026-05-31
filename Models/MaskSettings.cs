namespace PrivacyMasker.Models;

public sealed class MaskSettings
{
    public string MaskKind { get; set; } = "preset";
    public string PresetId { get; set; } = "dark";
    public string Message { get; set; } = "不可以偷看哦";
    public string? AssetPath { get; set; }
    public double Opacity { get; set; } = 0.92;
}
