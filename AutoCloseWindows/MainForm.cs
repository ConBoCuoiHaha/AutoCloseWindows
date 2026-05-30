using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AutoCloseWindows
{
    public class MainForm : Form
    {
        // ──────────────────────────────────────────────
        //  UI Controls
        // ──────────────────────────────────────────────
        private NotifyIcon       _trayIcon;
        private ContextMenuStrip _trayMenu;
        private Panel            _headerPanel;
        private Label            _titleLabel;
        private Label            _subtitleLabel;
        private PictureBox       _iconBox;
        private Button           _closeAllBtn;
        private Button           _previewBtn;
        private CheckBox         _confirmChk;
        private CheckedListBox   _windowListBox;
        private Label            _statusLabel;
        private Label            _listLabel;
        private Panel            _footerPanel;
        private Label            _countLabel;

        private readonly int          _selfPid     = Process.GetCurrentProcess().Id;
        private List<WindowInfo>      _currentWindows = new List<WindowInfo>();

        // ──────────────────────────────────────────────
        //  Constructor
        // ──────────────────────────────────────────────
        public MainForm()
        {
            InitializeComponents();
            BuildTrayIcon();
            Load += (s, e) => RefreshWindowList();
        }

        // ──────────────────────────────────────────────
        //  UI Build
        // ──────────────────────────────────────────────
        private void InitializeComponents()
        {
            // ── Form ──────────────────────────────────
            Text            = "Auto Close Windows";
            Size            = new Size(520, 640);
            MinimumSize     = new Size(420, 500);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(18, 18, 26);
            ForeColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);
            Icon            = CreateAppIcon();

            // ── Header panel ──────────────────────────
            _headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(28, 28, 40),
            };
            _headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(80, 120, 200), 1);
                e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1,
                                         _headerPanel.Width, _headerPanel.Height - 1);
            };

            _iconBox = new PictureBox
            {
                Size     = new Size(48, 48),
                Location = new Point(20, 20),
                Image    = DrawAppBitmap(48),
                SizeMode = PictureBoxSizeMode.StretchImage,
            };

            _titleLabel = new Label
            {
                Text      = "Auto Close Windows",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 180, 255),
                Location  = new Point(78, 18),
                AutoSize  = true,
            };

            _subtitleLabel = new Label
            {
                Text      = "Đóng toàn bộ cửa sổ chỉ với một cú click",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 130, 160),
                Location  = new Point(79, 50),
                AutoSize  = true,
            };

            _headerPanel.Controls.AddRange(new Control[]
                { _iconBox, _titleLabel, _subtitleLabel });

            // ── Content area ──────────────────────────
            var contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20, 16, 20, 0),
                BackColor = Color.Transparent,
            };

            // Buttons row
            _closeAllBtn = MakeButton("⚡  Đóng Tất Cả Ngay", Color.FromArgb(220, 50, 60),
                                     Color.FromArgb(180, 30, 40));
            _closeAllBtn.Font   = new Font("Segoe UI", 11f, FontStyle.Bold);
            _closeAllBtn.Height = 48;
            _closeAllBtn.Click += OnCloseAllClicked;
            _closeAllBtn.Dock   = DockStyle.Top;

            _previewBtn = MakeButton("🔍  Làm Mới Danh Sách", Color.FromArgb(40, 100, 180),
                                     Color.FromArgb(30, 75, 140));
            _previewBtn.Dock  = DockStyle.Top;
            _previewBtn.Click += (s, e) => RefreshWindowList();

            // Checkbox
            _confirmChk = MakeCheckBox("Hỏi xác nhận trước khi đóng", true);

            var chkPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 32,
                FlowDirection = FlowDirection.TopDown,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 6, 0, 0),
            };
            chkPanel.Controls.Add(_confirmChk);

            // List header: label + count
            var listHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 28 };
            _listLabel = new Label
            {
                Text      = "Cửa sổ sẽ đóng:  (bỏ tích ☐ để giữ lại)",
                ForeColor = Color.FromArgb(180, 180, 200),
                Font      = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Location  = new Point(0, 6),
                AutoSize  = true,
            };
            _countLabel = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(100, 200, 120),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock      = DockStyle.Right,
                Width     = 140,
            };
            listHeaderPanel.Controls.AddRange(new Control[] { _listLabel, _countLabel });

            // ── CheckedListBox (main list) ─────────────
            _windowListBox = new CheckedListBox
            {
                Dock           = DockStyle.Fill,
                BackColor      = Color.FromArgb(24, 24, 36),
                ForeColor      = Color.FromArgb(200, 210, 230),
                BorderStyle    = BorderStyle.FixedSingle,
                Font           = new Font("Consolas", 8.8f),
                ItemHeight     = 22,
                IntegralHeight = false,
                CheckOnClick   = true,    // single click toggles
            };

            // Update count whenever a checkbox changes
            _windowListBox.ItemCheck += OnItemCheckChanged;

            // Assemble content (reverse order for DockStyle.Top)
            contentPanel.Controls.Add(_windowListBox);
            contentPanel.Controls.Add(listHeaderPanel);
            contentPanel.Controls.Add(chkPanel);
            contentPanel.Controls.Add(MakeSpacer(8));
            contentPanel.Controls.Add(_previewBtn);
            contentPanel.Controls.Add(MakeSpacer(6));
            contentPanel.Controls.Add(_closeAllBtn);
            contentPanel.Controls.Add(MakeSpacer(4));

            // ── Footer ────────────────────────────────
            _footerPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 36,
                BackColor = Color.FromArgb(22, 22, 32),
            };
            _statusLabel = new Label
            {
                Text      = "Sẵn sàng  •  Double-click icon taskbar để đóng tất cả",
                ForeColor = Color.FromArgb(90, 110, 140),
                Font      = new Font("Segoe UI", 8f),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _footerPanel.Controls.Add(_statusLabel);
            _footerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(50, 80, 140), 1);
                e.Graphics.DrawLine(pen, 0, 0, _footerPanel.Width, 0);
            };

            Controls.AddRange(new Control[] { contentPanel, _footerPanel, _headerPanel });

            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    MinimizeToTray();
                }
            };

            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                    MinimizeToTray();
            };
        }

        // ──────────────────────────────────────────────
        //  Tray Icon
        // ──────────────────────────────────────────────
        private void BuildTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.BackColor = Color.FromArgb(28, 28, 40);
            _trayMenu.ForeColor = Color.White;
            _trayMenu.Renderer  = new DarkMenuRenderer();

            var itemClose = new ToolStripMenuItem("⚡  Đóng Tất Cả Cửa Sổ");
            var itemShow  = new ToolStripMenuItem("🖥  Mở Giao Diện");
            var itemExit  = new ToolStripMenuItem("✖  Thoát Ứng Dụng");

            itemClose.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            itemClose.ForeColor = Color.FromArgb(255, 100, 100);
            itemShow.Click  += (s, e) => RestoreFromTray();
            itemClose.Click += (s, e) => DoCloseAll();
            itemExit.Click  += (s, e) => { _trayIcon.Visible = false; Application.Exit(); };

            _trayMenu.Items.Add(itemShow);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(itemClose);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(itemExit);

            _trayIcon = new NotifyIcon
            {
                Text             = "Auto Close Windows\nDouble-click để đóng tất cả",
                Icon             = CreateAppIcon(),
                ContextMenuStrip = _trayMenu,
                Visible          = true,
            };

            _trayIcon.DoubleClick += (s, e) => DoCloseAll();
        }

        // ──────────────────────────────────────────────
        //  Core logic
        // ──────────────────────────────────────────────
        private void OnCloseAllClicked(object sender, EventArgs e) => DoCloseAll();

        private void DoCloseAll()
        {
            // Build list of windows to close: only the CHECKED items in the list.
            // If the list hasn't been loaded yet, fall back to all windows.
            List<WindowInfo> toClose;

            if (_currentWindows.Count > 0)
            {
                toClose = new List<WindowInfo>();
                for (int i = 0; i < _windowListBox.Items.Count; i++)
                {
                    if (_windowListBox.GetItemChecked(i) && i < _currentWindows.Count)
                        toClose.Add(_currentWindows[i]);
                }
            }
            else
            {
                toClose = WindowManager.GetCloseableWindows(_selfPid);
            }

            if (toClose.Count == 0)
            {
                ShowStatus("Không có cửa sổ nào được chọn để đóng.", Color.FromArgb(255, 180, 50));
                _trayIcon.ShowBalloonTip(2000, "Auto Close Windows",
                    "Không có cửa sổ nào được chọn.", ToolTipIcon.Info);
                return;
            }

            if (_confirmChk.Checked)
            {
                string preview = BuildPreviewText(toClose);
                var dlg = new ConfirmDialog(toClose.Count, preview);
                if (dlg.ShowDialog() != DialogResult.OK) return;
            }

            WindowManager.CloseWindowList(toClose);

            string msg = $"Đã gửi lệnh đóng đến {toClose.Count} cửa sổ.";
            ShowStatus(msg, Color.FromArgb(100, 200, 120));
            _trayIcon.ShowBalloonTip(2500, "Hoàn thành!", msg, ToolTipIcon.Info);

            var t = new System.Windows.Forms.Timer { Interval = 800 };
            t.Tick += (s, e2) => { t.Stop(); RefreshWindowList(); };
            t.Start();
        }

        private void RefreshWindowList()
        {
            // Detach event while bulk-adding to avoid BeginInvoke spam
            _windowListBox.ItemCheck -= OnItemCheckChanged;
            _windowListBox.Items.Clear();

            _currentWindows = WindowManager.GetCloseableWindows(_selfPid);
            foreach (var w in _currentWindows)
                _windowListBox.Items.Add(w.ToString(), true);

            _windowListBox.ItemCheck += OnItemCheckChanged;

            UpdateCountLabel();
            ShowStatus($"Tìm thấy {_currentWindows.Count} cửa sổ đang mở.", Color.FromArgb(130, 160, 210));
        }

        private void OnItemCheckChanged(object sender, ItemCheckEventArgs e)
        {
            if (IsHandleCreated)
                BeginInvoke(new Action(UpdateCountLabel));
        }

        private void UpdateCountLabel()
        {
            int total   = _windowListBox.Items.Count;
            int checked_ = _windowListBox.CheckedItems.Count;
            if (total == 0)
            {
                _countLabel.Text      = "0 cửa sổ";
                _countLabel.ForeColor = Color.FromArgb(130, 130, 160);
            }
            else if (checked_ == total)
            {
                _countLabel.Text      = $"{total} cửa sổ";
                _countLabel.ForeColor = Color.FromArgb(100, 200, 120);
            }
            else
            {
                _countLabel.Text      = $"{checked_}/{total} được chọn";
                _countLabel.ForeColor = Color.FromArgb(255, 180, 50);
            }
        }

        // ──────────────────────────────────────────────
        //  Tray helpers
        // ──────────────────────────────────────────────
        private void MinimizeToTray()
        {
            Hide();
            ShowInTaskbar = false;
            _trayIcon.ShowBalloonTip(1500, "Auto Close Windows",
                "Ứng dụng đang chạy dưới khay hệ thống.\nDouble-click icon để đóng tất cả cửa sổ.",
                ToolTipIcon.Info);
        }

        private void RestoreFromTray()
        {
            Show();
            ShowInTaskbar = true;
            WindowState   = FormWindowState.Normal;
            Activate();
            RefreshWindowList();
        }

        // ──────────────────────────────────────────────
        //  UI Helpers
        // ──────────────────────────────────────────────
        private void ShowStatus(string msg, Color color)
        {
            _statusLabel.Text      = msg;
            _statusLabel.ForeColor = color;
        }

        private static string BuildPreviewText(List<WindowInfo> windows)
        {
            var sb   = new System.Text.StringBuilder();
            int show = Math.Min(windows.Count, 8);
            for (int i = 0; i < show; i++)
                sb.AppendLine($"• {windows[i]}");
            if (windows.Count > show)
                sb.AppendLine($"  ...và {windows.Count - show} cửa sổ khác");
            return sb.ToString().TrimEnd();
        }

        private static Button MakeButton(string text, Color bg, Color hover)
        {
            var btn = new Button
            {
                Text      = text,
                Height    = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0),
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(hover.R - 20, 0), Math.Max(hover.G - 20, 0), Math.Max(hover.B - 20, 0));
            return btn;
        }

        private static CheckBox MakeCheckBox(string text, bool @checked)
        {
            return new CheckBox
            {
                Text      = text,
                Checked   = @checked,
                ForeColor = Color.FromArgb(170, 180, 200),
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                Padding   = new Padding(0, 2, 0, 2),
            };
        }

        private static Panel MakeSpacer(int height) =>
            new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.Transparent };

        // ──────────────────────────────────────────────
        //  Icon generation
        // ──────────────────────────────────────────────
        private static Icon CreateAppIcon()
        {
            using var bmp = DrawAppBitmap(32);
            return Icon.FromHandle(bmp.GetHicon());
        }

        private static Bitmap DrawAppBitmap(int size)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var bgBrush = new SolidBrush(Color.FromArgb(220, 50, 60));
            g.FillEllipse(bgBrush, 0, 0, size - 1, size - 1);

            int pad   = size / 5;
            int thick = Math.Max(size / 8, 2);
            using var pen = new Pen(Color.White, thick) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, pad, pad, size - pad - 1, size - pad - 1);
            g.DrawLine(pen, size - pad - 1, pad, pad, size - pad - 1);

            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _trayMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ──────────────────────────────────────────────
    //  Dark menu renderer
    // ──────────────────────────────────────────────
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
                using (var b = new SolidBrush(Color.FromArgb(50, 90, 160)))
                    e.Graphics.FillRectangle(b, e.Item.ContentRectangle);
            else
                using (var b = new SolidBrush(Color.FromArgb(28, 28, 40)))
                    e.Graphics.FillRectangle(b, e.Item.ContentRectangle);
        }
    }
}
