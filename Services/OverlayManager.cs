using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public sealed class OverlayManager
{
    private readonly Dictionary<IntPtr, OverlayWindow> _overlays = [];

    public void Sync(IReadOnlyCollection<WindowSnapshot> protectedWindows)
    {
        var liveHandles = protectedWindows.Select(window => window.Handle).ToHashSet();

        foreach (var staleHandle in _overlays.Keys.Where(handle => !liveHandles.Contains(handle)).ToList())
        {
            _overlays[staleHandle].Close();
            _overlays.Remove(staleHandle);
        }

        foreach (var window in protectedWindows)
        {
            if (!_overlays.TryGetValue(window.Handle, out var overlay))
            {
                overlay = new OverlayWindow();
                _overlays[window.Handle] = overlay;
                overlay.Show();
            }

            overlay.UpdateFor(window);
        }
    }

    public void Clear()
    {
        foreach (var overlay in _overlays.Values)
        {
            overlay.Close();
        }

        _overlays.Clear();
    }
}
