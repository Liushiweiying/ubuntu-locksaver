using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace UbuntuLockSaver
{
    internal sealed class ConfigForm : Form
    {
        public ConfigForm()
        {
            SuspendLayout();

            Text = "Ubuntu Lock Saver";
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 190);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            Label description = new Label();
            description.AutoSize = false;
            description.Location = new Point(18, 18);
            description.Size = new Size(424, 96);
            description.Text =
                "仿 Ubuntu 锁屏的屏幕保护程序：模糊壁纸、24 小时制时钟、本地化日期，" +
                "以及淡入淡出、分钟数字淡变和壁纸缓慢缩放动画。" + Environment.NewLine + Environment.NewLine +
                "等待时间、是否在恢复时显示登录界面，由 Windows 屏幕保护程序设置管理。";

            Button settings = new Button();
            settings.Text = "打开 Windows 屏保设置";
            settings.Location = new Point(18, 132);
            settings.Size = new Size(200, 34);
            settings.Click += delegate
            {
                Process.Start("control.exe", "desk.cpl,,@screensaver");
            };

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new Point(336, 132);
            close.Size = new Size(106, 34);
            close.Click += delegate { Close(); };

            Controls.Add(description);
            Controls.Add(settings);
            Controls.Add(close);

            ResumeLayout(false);
        }
    }
}
