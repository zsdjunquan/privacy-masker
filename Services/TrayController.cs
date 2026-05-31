using System.Drawing;
using System.Windows.Forms;

namespace PrivacyMasker.Services;

public sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly Action _toggleProtection;

    public TrayController(Action toggleProtection, Action showWindow, Action exit)
    {
        _toggleProtection = toggleProtection;

        _toggleItem = new ToolStripMenuItem("开启保护", null, (_, _) => _toggleProtection());
        var showItem = new ToolStripMenuItem("打开面板", null, (_, _) => showWindow());
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => exit());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(showItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Text = "Privacy Masker",
            Icon = SystemIcons.Shield,
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => showWindow();
    }

    public void Update(bool isEnabled)
    {
        _toggleItem.Text = isEnabled ? "关闭保护" : "开启保护";
        _notifyIcon.Text = isEnabled ? "Privacy Masker - 保护中" : "Privacy Masker - 未开启";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
