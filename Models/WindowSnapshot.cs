using System.Windows;

namespace PrivacyMasker.Models;

public sealed record WindowSnapshot(
    IntPtr Handle,
    string Title,
    string ProcessName,
    Rect Bounds);
