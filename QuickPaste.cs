// QuickPaste — 全局快速粘贴工具 / Global quick paste tool
// Copyright (c) 2026 ghjkllasdf-fake
// Licensed under the MIT License. See LICENSE file for details.
//
// 编译 / Build: csc /target:winexe /out:QuickPaste.exe /r:System.Web.Extensions.dll QuickPaste.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

// ═══════════════════════════════════════════════════════════════════
// Entry point
// ═══════════════════════════════════════════════════════════════════
static class Program
{
    [STAThread]
    static void Main()
    {
        // Single instance guard
        bool created;
        using (var mutex = new System.Threading.Mutex(true, "QuickPaste_SingleInstance_7F3A", out created))
        {
            if (!created) { return; }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool restart;
            do
            {
                var config = ConfigManager.Load();
                var app    = new QuickPasteApp(config);
                Application.Run(app);
                restart = app.ShouldRestart;
            } while (restart);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
// Config
// ═══════════════════════════════════════════════════════════════════
class SnippetItem
{
    public string Name;
    public string Text;
    public bool   IsSeparator;
}

class AppConfig
{
    public bool              AutoPaste = true;
    public uint              HotkeyMod = 0x0002 | 0x0004; // Ctrl+Shift
    public uint              HotkeyVk  = 0x51;            // Q
    public List<SnippetItem> Snippets  = new List<SnippetItem>();
}

static class ConfigManager
{
    public static string ConfigPath
    {
        get { return Path.Combine(AppDir, "snippets.json"); }
    }

    public static string AppDir
    {
        get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
    }

    public static AppConfig Load()
    {
        var cfg = new AppConfig();
        string path = ConfigPath;
        if (!File.Exists(path))
        {
            CreateDefault();
        }
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var ser  = new JavaScriptSerializer();
            var dict = ser.Deserialize<Dictionary<string, object>>(json);
            if (dict == null) return cfg;

            if (dict.ContainsKey("auto_paste"))
                cfg.AutoPaste = Convert.ToBoolean(dict["auto_paste"]);

            if (dict.ContainsKey("hotkey_mod"))
                cfg.HotkeyMod = Convert.ToUInt32(dict["hotkey_mod"]);
            if (dict.ContainsKey("hotkey_vk"))
                cfg.HotkeyVk = Convert.ToUInt32(dict["hotkey_vk"]);

            if (dict.ContainsKey("snippets"))
            {
                var arr = dict["snippets"] as ArrayList;
                if (arr != null)
                {
                    foreach (Dictionary<string, object> item in arr)
                    {
                        if (item.ContainsKey("separator") && Convert.ToBoolean(item["separator"]))
                        {
                            cfg.Snippets.Add(new SnippetItem { IsSeparator = true });
                        }
                        else if (item.ContainsKey("name"))
                        {
                            cfg.Snippets.Add(new SnippetItem
                            {
                                Name = item["name"].ToString(),
                                Text = item.ContainsKey("text") ? item["text"].ToString() : ""
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            MessageBox.Show("snippets.json 格式错误，已恢复默认配置。", "QuickPaste",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            CreateDefault();
            return Load();
        }
        return cfg;
    }

    public static void Save(AppConfig cfg)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"auto_paste\": " + (cfg.AutoPaste ? "true" : "false") + ",");
        sb.AppendLine("  \"hotkey_mod\": " + cfg.HotkeyMod + ",");
        sb.AppendLine("  \"hotkey_vk\": "  + cfg.HotkeyVk + ",");
        sb.AppendLine("  \"snippets\": [");
        for (int i = 0; i < cfg.Snippets.Count; i++)
        {
            var s    = cfg.Snippets[i];
            bool last = (i == cfg.Snippets.Count - 1);
            if (s.IsSeparator)
                sb.AppendLine("    { \"separator\": true }" + (last ? "" : ","));
            else
                sb.AppendLine("    { \"name\": " + JsonEsc(s.Name) + ", \"text\": " + JsonEsc(s.Text) + " }" + (last ? "" : ","));
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
    }

    private static void CreateDefault()
    {
        var cfg = new AppConfig();
        cfg.Snippets.AddRange(new[]
        {
            new SnippetItem { Name = "\U0001f4e7 工作邮箱", Text = "your.email@microsoft.com" },
            new SnippetItem { Name = "\U0001f4e7 个人邮箱", Text = "your.email@gmail.com" },
            new SnippetItem { IsSeparator = true },
            new SnippetItem { Name = "\U0001f517 GitHub",    Text = "https://github.com/yourname" },
            new SnippetItem { Name = "\U0001f4de 电话",      Text = "+86-138-xxxx-xxxx" },
            new SnippetItem { IsSeparator = true },
            new SnippetItem { Name = "\U0001f3e0 地址",      Text = "Your address here" },
        });
        Save(cfg);
    }

    private static string JsonEsc(string s)
    {
        if (s == null) s = "";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                       .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
    }
}

// ═══════════════════════════════════════════════════════════════════
// HotkeyHost — dedicated hidden window for global hotkey
// ═══════════════════════════════════════════════════════════════════
class HotkeyHost : NativeWindow, IDisposable
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    const int WM_HOTKEY  = 0x0312;
    const int HOTKEY_ID  = 9001;

    public event Action HotkeyPressed;
    public uint CurrentMod { get; private set; }
    public uint CurrentVk  { get; private set; }

    public HotkeyHost(uint mod, uint vk)
    {
        CurrentMod = mod;
        CurrentVk  = vk;
        CreateHandle(new CreateParams());
    }

    public bool Register()
    {
        return RegisterHotKey(Handle, HOTKEY_ID, CurrentMod, CurrentVk);
    }

    public bool Reregister(uint mod, uint vk)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        CurrentMod = mod;
        CurrentVk  = vk;
        return RegisterHotKey(Handle, HOTKEY_ID, mod, vk);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            if (HotkeyPressed != null) HotkeyPressed();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        DestroyHandle();
    }
}

// ═══════════════════════════════════════════════════════════════════
// QuickPasteApp — main form (tray-only)
// ═══════════════════════════════════════════════════════════════════
class QuickPasteApp : Form
{
    [DllImport("user32.dll")] static extern bool   GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool   SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern void   keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    const byte VK_CONTROL      = 0x11;
    const byte VK_V            = 0x56;
    const uint KEYEVENTF_KEYUP = 0x0002;

    NotifyIcon       trayIcon;
    ContextMenuStrip snippetMenu;
    HotkeyHost       hotkeyHost;
    AppConfig        config;
    IntPtr           prevWindow;

    public bool ShouldRestart { get; private set; }

    public QuickPasteApp(AppConfig cfg)
    {
        config          = cfg;
        ShowInTaskbar   = false;
        WindowState     = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Size            = new Size(0, 0);
        Visible         = false;

        // Hotkey host
        hotkeyHost = new HotkeyHost(cfg.HotkeyMod, cfg.HotkeyVk);
        hotkeyHost.HotkeyPressed += OnHotkey;

        // Snippet popup
        snippetMenu = new ContextMenuStrip { ShowImageMargin = false, AutoClose = true };
        snippetMenu.Font = new Font("Segoe UI", 10F);
        RebuildSnippetMenu();

        // Tray icon
        Icon appIcon = LoadAppIcon();
        trayIcon = new NotifyIcon
        {
            Icon    = appIcon ?? SystemIcons.Application,
            Visible = true,
            Text    = "QuickPaste"
        };
        if (appIcon != null) this.Icon = appIcon;
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("⚙️ 设置",       null, (s, e) => ShowSettings());
        trayMenu.Items.Add("🔄 重新加载配置", null, (s, e) => { ShouldRestart = true;  Close(); });
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("❌ 退出",         null, (s, e) => { ShouldRestart = false; Close(); });
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += (s, e) => ShowSettings();

        if (!hotkeyHost.Register())
        {
            MessageBox.Show("热键注册失败，可能已被其他程序占用。", "QuickPaste",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Hide();
        trayIcon.ShowBalloonTip(2000, "QuickPaste 已启动",
            "按 " + HotkeyText() + " 弹出快速粘贴菜单", ToolTipIcon.Info);
    }

    string HotkeyText()
    {
        var p = new List<string>();
        if ((hotkeyHost.CurrentMod & 0x0004) != 0) p.Add("Ctrl");
        if ((hotkeyHost.CurrentMod & 0x0001) != 0) p.Add("Alt");
        if ((hotkeyHost.CurrentMod & 0x0002) != 0) p.Add("Shift");
        if ((hotkeyHost.CurrentMod & 0x0008) != 0) p.Add("Win");
        p.Add(((Keys)hotkeyHost.CurrentVk).ToString());
        return string.Join("+", p.ToArray());
    }

    public void RebuildSnippetMenu()
    {
        snippetMenu.Items.Clear();
        foreach (var s in config.Snippets)
        {
            if (s.IsSeparator) { snippetMenu.Items.Add(new ToolStripSeparator()); continue; }
            var item = new ToolStripMenuItem(s.Name) { Tag = s.Text };
            item.ToolTipText = s.Text != null && s.Text.Length > 100 ? s.Text.Substring(0, 100) + "…" : s.Text;
            item.Click += OnSnippetClick;
            snippetMenu.Items.Add(item);
        }
    }

    static Icon LoadAppIcon()
    {
        try
        {
            // Try external file first, then fall back to embedded exe icon
            string dir  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dir, "app.ico");
            if (File.Exists(path)) return new Icon(path);
            return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        }
        catch { }
        return null;
    }

    void OnHotkey()
    {
        prevWindow = GetForegroundWindow();
        POINT pt;
        GetCursorPos(out pt);
        // Must bring our process to foreground so the menu
        // can detect click-outside and auto-close properly
        SetForegroundWindow(Handle);
        snippetMenu.Show(pt.X, pt.Y);
    }

    void OnSnippetClick(object sender, EventArgs e)
    {
        var mi = sender as ToolStripMenuItem;
        if (mi == null || mi.Tag == null) return;
        string txt = mi.Tag.ToString();
        Clipboard.SetText(txt);

        if (config.AutoPaste && prevWindow != IntPtr.Zero)
        {
            SetForegroundWindow(prevWindow);
            Thread.Sleep(120);
            keybd_event(VK_CONTROL, 0, 0,               UIntPtr.Zero);
            keybd_event(VK_V,       0, 0,               UIntPtr.Zero);
            keybd_event(VK_V,       0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        else
        {
            trayIcon.ShowBalloonTip(2000, "已复制", txt.Length > 60 ? txt.Substring(0, 60) + "…" : txt, ToolTipIcon.Info);
        }
    }

    // ── Settings ──────────────────────────────────────────────
    SettingsForm settingsForm;

    void ShowSettings()
    {
        if (settingsForm != null && !settingsForm.IsDisposed) { settingsForm.Activate(); return; }
        settingsForm = new SettingsForm(this, config, hotkeyHost);
        settingsForm.Show();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        hotkeyHost.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        base.OnFormClosing(e);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SettingsForm — Fluent dark settings UI
// ═══════════════════════════════════════════════════════════════════
class SettingsForm : Form
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);
    const int DWMWA_USE_IMMERSIVE_DARK_MODE   = 20;
    const int DWMWA_WINDOW_CORNER_PREFERENCE  = 33;
    const int DWMWCP_ROUND = 2;

    static readonly Color BgColor     = Color.FromArgb(32, 32, 32);
    static readonly Color CardColor   = Color.FromArgb(44, 44, 44);
    static readonly Color TextColor   = Color.FromArgb(230, 230, 230);
    static readonly Color DimColor    = Color.FromArgb(160, 160, 160);
    static readonly Color AccentColor = Color.FromArgb(96, 165, 250);
    static readonly Color BorderColor = Color.FromArgb(60, 60, 60);
    static readonly Color HoverColor  = Color.FromArgb(55, 55, 55);

    QuickPasteApp app;
    AppConfig     config;
    HotkeyHost    hotkey;

    ListView  snippetList;
    CheckBox  chkAutoStart;
    TextBox   txtHotkey;
    uint      pendingMod, pendingVk;
    bool      capturing;

    public SettingsForm(QuickPasteApp owner, AppConfig cfg, HotkeyHost hk)
    {
        app    = owner;
        config = cfg;
        hotkey = hk;

        Text            = "QuickPaste 设置";
        Size            = new Size(540, 620);
        MinimumSize     = new Size(440, 500);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = BgColor;
        ForeColor       = TextColor;
        Font            = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        KeyPreview      = true;

        ApplyDwm(Handle);
        BuildUI();
        LoadValues();
    }

    static void ApplyDwm(IntPtr hwnd)
    {
        try
        {
            int v = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, 4);
            int r = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref r, 4);
        }
        catch { }
    }

    void BuildUI()
    {
        int y = 20;

        // ── Hotkey ────────────────────────────────────────────
        Controls.Add(Lbl("快捷键", 20, y, true)); y += 30;

        var pnlHk = Card(20, y, ClientSize.Width - 40, 48);
        txtHotkey = new TextBox
        {
            Location    = new Point(12, 12),
            Width       = pnlHk.Width - 108,
            ReadOnly    = true,
            BackColor   = CardColor,
            ForeColor   = TextColor,
            BorderStyle = BorderStyle.None,
            Font        = new Font("Segoe UI", 10F),
            Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        txtHotkey.GotFocus  += (s, e) => { capturing = true;  txtHotkey.Text = "请按下新快捷键…"; };
        txtHotkey.LostFocus += (s, e) => { capturing = false; ShowHotkey(); };

        var btnApply = Btn("应用", pnlHk.Width - 84, 9, 72, 30);
        btnApply.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        btnApply.Click += (s, e) =>
        {
            if (pendingVk != 0)
            {
                if (!hotkey.Reregister(pendingMod, pendingVk))
                    MessageBox.Show("热键注册失败", "QuickPaste");
                else
                {
                    config.HotkeyMod = pendingMod;
                    config.HotkeyVk  = pendingVk;
                    ConfigManager.Save(config);
                    ShowHotkey();
                }
            }
            capturing = false;
        };

        pnlHk.Controls.Add(txtHotkey);
        pnlHk.Controls.Add(btnApply);
        Controls.Add(pnlHk);
        y += 60;

        // ── Auto-start ────────────────────────────────────────
        var pnlAs = Card(20, y, ClientSize.Width - 40, 44);
        chkAutoStart = new CheckBox
        {
            Text      = "开机自启动",
            Location  = new Point(12, 10),
            AutoSize  = true,
            ForeColor = TextColor,
            FlatStyle = FlatStyle.Flat
        };
        chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);
        pnlAs.Controls.Add(chkAutoStart);
        Controls.Add(pnlAs);
        y += 58;

        // ── Snippets ──────────────────────────────────────────
        Controls.Add(Lbl("预设短语", 20, y, true)); y += 30;

        snippetList = new ListView
        {
            Location    = new Point(20, y),
            Size        = new Size(ClientSize.Width - 40, ClientSize.Height - y - 72),
            View        = View.Details,
            FullRowSelect = true,
            BackColor   = CardColor,
            ForeColor   = TextColor,
            BorderStyle = BorderStyle.None,
            Font        = new Font("Segoe UI", 10F),
            Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        snippetList.Columns.Add("名称", 160);
        snippetList.Columns.Add("内容", 320);
        Controls.Add(snippetList);

        // Buttons
        int by = ClientSize.Height - 44;
        var btnAdd  = Btn("➕ 添加",   20,  by, 84, 34); btnAdd.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
        var btnEdit = Btn("✏️ 编辑",  112, by, 84, 34); btnEdit.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        var btnDel  = Btn("🗑️ 删除", 204, by, 84, 34); btnDel.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
        var btnSep  = Btn("─ 分隔线",  296, by, 90, 34); btnSep.Anchor  = AnchorStyles.Left | AnchorStyles.Bottom;
        var btnUp   = Btn("▲",         396, by, 40, 34); btnUp.Anchor   = AnchorStyles.Left | AnchorStyles.Bottom;
        var btnDown = Btn("▼",         444, by, 40, 34); btnDown.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        btnAdd.Click  += (s, e) => EditDialog(-1);
        btnEdit.Click += (s, e) => { if (snippetList.SelectedIndices.Count > 0) EditDialog(snippetList.SelectedIndices[0]); };
        btnDel.Click  += (s, e) =>
        {
            if (snippetList.SelectedIndices.Count > 0)
            {
                int idx = snippetList.SelectedIndices[0];
                snippetList.Items.RemoveAt(idx);
                SaveFromList();
            }
        };
        btnSep.Click += (s, e) =>
        {
            var li = new ListViewItem(new[] { "───", "(分隔线)" }) { Tag = "sep" };
            snippetList.Items.Add(li);
            SaveFromList();
        };
        btnUp.Click += (s, e) => MoveItem(-1);
        btnDown.Click += (s, e) => MoveItem(1);

        Controls.Add(btnAdd); Controls.Add(btnEdit); Controls.Add(btnDel);
        Controls.Add(btnSep); Controls.Add(btnUp); Controls.Add(btnDown);
    }

    void LoadValues()
    {
        ShowHotkey();
        chkAutoStart.Checked = IsAutoStart();
        snippetList.Items.Clear();
        foreach (var s in config.Snippets)
        {
            if (s.IsSeparator)
            {
                var li = new ListViewItem(new[] { "───", "(分隔线)" }) { Tag = "sep" };
                snippetList.Items.Add(li);
            }
            else
                snippetList.Items.Add(new ListViewItem(new[] { s.Name ?? "", s.Text ?? "" }));
        }
    }

    void ShowHotkey()
    {
        var p = new List<string>();
        if ((hotkey.CurrentMod & 0x0004) != 0) p.Add("Ctrl");
        if ((hotkey.CurrentMod & 0x0001) != 0) p.Add("Alt");
        if ((hotkey.CurrentMod & 0x0002) != 0) p.Add("Shift");
        if ((hotkey.CurrentMod & 0x0008) != 0) p.Add("Win");
        p.Add(((Keys)hotkey.CurrentVk).ToString());
        txtHotkey.Text = string.Join(" + ", p.ToArray());
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (capturing)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                key == Keys.LWin || key == Keys.RWin)
                return true;

            Keys mod = keyData & Keys.Modifiers;
            uint fm = 0;
            if ((mod & Keys.Control) != 0) fm |= 0x0004;
            if ((mod & Keys.Alt)     != 0) fm |= 0x0001;
            if ((mod & Keys.Shift)   != 0) fm |= 0x0002;

            pendingMod = fm;
            pendingVk  = (uint)key;

            var p = new List<string>();
            if ((fm & 0x0004) != 0) p.Add("Ctrl");
            if ((fm & 0x0001) != 0) p.Add("Alt");
            if ((fm & 0x0002) != 0) p.Add("Shift");
            p.Add(key.ToString());
            txtHotkey.Text = string.Join(" + ", p.ToArray()) + "  (\u70B9\u201C\u5E94\u7528\u201D\u786E\u8BA4)";
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ── Snippet editing ──────────────────────────────────────
    void EditDialog(int index)
    {
        var dlg = new Form
        {
            Text            = index < 0 ? "添加短语" : "编辑短语",
            Size            = new Size(420, 240),
            StartPosition   = FormStartPosition.CenterParent,
            BackColor       = BgColor,
            ForeColor       = TextColor,
            Font            = Font,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox     = false,
            MinimizeBox     = false
        };
        ApplyDwm(dlg.Handle);

        dlg.Controls.Add(Lbl("名称:", 16, 16, false));
        var tName = new TextBox
        {
            Location = new Point(16, 42), Width = 370,
            BackColor = CardColor, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle
        };
        dlg.Controls.Add(tName);

        dlg.Controls.Add(Lbl("内容:", 16, 76, false));
        var tText = new TextBox
        {
            Location = new Point(16, 102), Width = 370, Height = 50, Multiline = true,
            BackColor = CardColor, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical
        };
        dlg.Controls.Add(tText);

        if (index >= 0 && snippetList.Items[index].Tag as string != "sep")
        {
            tName.Text = snippetList.Items[index].SubItems[0].Text;
            tText.Text = snippetList.Items[index].SubItems[1].Text;
        }

        var ok = Btn("确定", 216, 162, 80, 32);
        ok.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
        var cancel = Btn("取消", 304, 162, 80, 32);
        cancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok;

        if (dlg.ShowDialog(this) == DialogResult.OK && tName.Text.Trim().Length > 0)
        {
            if (index < 0)
                snippetList.Items.Add(new ListViewItem(new[] { tName.Text.Trim(), tText.Text }));
            else
            {
                snippetList.Items[index].SubItems[0].Text = tName.Text.Trim();
                snippetList.Items[index].SubItems[1].Text = tText.Text;
            }
            SaveFromList();
        }
    }

    void MoveItem(int dir)
    {
        if (snippetList.SelectedIndices.Count == 0) return;
        int idx = snippetList.SelectedIndices[0];
        int target = idx + dir;
        if (target < 0 || target >= snippetList.Items.Count) return;

        var item = snippetList.Items[idx];
        snippetList.Items.RemoveAt(idx);
        snippetList.Items.Insert(target, item);
        item.Selected = true;
        SaveFromList();
    }

    void SaveFromList()
    {
        config.Snippets.Clear();
        for (int i = 0; i < snippetList.Items.Count; i++)
        {
            var li = snippetList.Items[i];
            if (li.Tag as string == "sep")
                config.Snippets.Add(new SnippetItem { IsSeparator = true });
            else
                config.Snippets.Add(new SnippetItem { Name = li.SubItems[0].Text, Text = li.SubItems[1].Text });
        }
        ConfigManager.Save(config);
        app.RebuildSnippetMenu();
    }

    // ── Auto-start ───────────────────────────────────────────
    string LnkPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "QuickPaste.lnk");
    }

    bool IsAutoStart() { return File.Exists(LnkPath()); }

    void SetAutoStart(bool enable)
    {
        string lnk = LnkPath();
        if (enable && !File.Exists(lnk))
        {
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell    = Activator.CreateInstance(t);
                dynamic shortcut = shell.CreateShortcut(lnk);
                shortcut.TargetPath       = Assembly.GetExecutingAssembly().Location;
                shortcut.WorkingDirectory = ConfigManager.AppDir;
                shortcut.Description      = "QuickPaste";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建快捷方式失败: " + ex.Message, "QuickPaste");
                chkAutoStart.Checked = false;
            }
        }
        else if (!enable && File.Exists(lnk))
        {
            try { File.Delete(lnk); } catch { }
        }
    }

    // ── UI helpers ───────────────────────────────────────────
    Label Lbl(string text, int x, int y, bool bold)
    {
        return new Label
        {
            Text     = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = bold ? TextColor : DimColor,
            Font     = new Font("Segoe UI", 10F, bold ? FontStyle.Bold : FontStyle.Regular)
        };
    }

    Panel Card(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Location  = new Point(x, y),
            Size      = new Size(w, h),
            BackColor = CardColor,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        p.Paint += (s, e) =>
        {
            using (var pen = new Pen(BorderColor))
            {
                var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (var path = RoundRect(rect, 6))
                    e.Graphics.DrawPath(pen, path);
            }
        };
        return p;
    }

    Button Btn(string text, int x, int y, int w, int h)
    {
        var b = new Button
        {
            Text      = text,
            Location  = new Point(x, y),
            Size      = new Size(w, h),
            FlatStyle = FlatStyle.Flat,
            BackColor = CardColor,
            ForeColor = TextColor,
            Cursor    = Cursors.Hand,
            Font      = new Font("Segoe UI", 9F)
        };
        b.FlatAppearance.BorderColor         = BorderColor;
        b.FlatAppearance.MouseOverBackColor   = HoverColor;
        return b;
    }

    static GraphicsPath RoundRect(Rectangle r, int rad)
    {
        int d = rad * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
