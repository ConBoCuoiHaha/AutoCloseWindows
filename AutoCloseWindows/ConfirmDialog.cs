using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoCloseWindows
{
    public class ConfirmDialog : Form
    {
        public ConfirmDialog(int windowCount, string preview)
        {
            Text            = "Xác nhận đóng cửa sổ";
            Size            = new Size(440, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(24, 24, 36);
            ForeColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // Warning icon area
            var iconLabel = new Label
            {
                Text      = "⚠",
                Font      = new Font("Segoe UI", 28f),
                ForeColor = Color.FromArgb(255, 200, 50),
                Location  = new Point(20, 18),
                AutoSize  = true,
            };

            var headerLabel = new Label
            {
                Text      = $"Bạn sắp đóng {windowCount} cửa sổ!",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 255),
                Location  = new Point(65, 20),
                AutoSize  = true,
            };

            var subLabel = new Label
            {
                Text      = "Các ứng dụng chưa lưu có thể hỏi bạn trước khi đóng.\nDanh sách cửa sổ sẽ bị đóng:",
                ForeColor = Color.FromArgb(160, 165, 185),
                Location  = new Point(65, 50),
                Size      = new Size(350, 40),
            };

            var listBox = new ListBox
            {
                Location    = new Point(20, 100),
                Size        = new Size(395, 130),
                BackColor   = Color.FromArgb(18, 18, 28),
                ForeColor   = Color.FromArgb(190, 200, 220),
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Consolas", 8.5f),
            };
            foreach (var line in preview.Split('\n'))
            {
                var l = line.Trim();
                if (!string.IsNullOrEmpty(l))
                    listBox.Items.Add(l);
            }

            var btnOk = new Button
            {
                Text      = "✔  Đóng Tất Cả",
                Location  = new Point(200, 246),
                Size      = new Size(130, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 40, 50),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Cursor    = Cursors.Hand,
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text      = "✖  Hủy",
                Location  = new Point(88, 246),
                Size      = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 70),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f),
                DialogResult = DialogResult.Cancel,
                Cursor    = Cursors.Hand,
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
                { iconLabel, headerLabel, subLabel, listBox, btnOk, btnCancel });
        }
    }
}
