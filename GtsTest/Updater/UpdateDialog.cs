using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GtsTest.Updater
{
    /// <summary>
    /// 更新提示对话框：显示版本信息 + 下载进度
    /// </summary>
    public class UpdateDialog : Form
    {
        private Label lblTitle;
        private Label lblVersion;
        private TextBox txtNotes;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Button btnUpdate;
        private Button btnSkip;

        public bool UserConfirmed { get; private set; }
        public string? DownloadedPackagePath { get; private set; }

        private readonly UpdateManifest _manifest;

        public UpdateDialog(UpdateManifest manifest)
        {
            _manifest = manifest;
            BuildUI();

            this.FormClosing += (s, e) =>
            {
                if (!UserConfirmed && !_manifest.Mandatory)
                    this.DialogResult = DialogResult.Cancel;
            };
        }

        private void BuildUI()
        {
            this.Text = "发现新版本";
            this.Size = new Size(560, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle = new Label
            {
                Text = "🎉 发现新版本",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };

            lblVersion = new Label
            {
                Text = $"当前版本: {UpdateConfig.GetCurrentVersion()}   →   最新版本: {_manifest.Version}",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 50),
                AutoSize = true
            };

            var lblNotes = new Label
            {
                Text = "更新内容:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(20, 80),
                AutoSize = true
            };

            txtNotes = new TextBox
            {
                Location = new Point(20, 105),
                Size = new Size(505, 160),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _manifest.ReleaseNotes ?? "",
                Font = new Font("Consolas", 9F),
                BackColor = Color.FromArgb(248, 248, 250)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 280),
                Size = new Size(505, 22),
                Style = ProgressBarStyle.Continuous
            };

            lblProgress = new Label
            {
                Text = "就绪",
                Location = new Point(20, 305),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            btnUpdate = new Button
            {
                Text = "⬇ 立即更新",
                Location = new Point(340, 335),
                Size = new Size(90, 32),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnUpdate.Click += BtnUpdate_Click;

            btnSkip = new Button
            {
                Text = _manifest.Mandatory ? "退出" : "稍后再说",
                Location = new Point(435, 335),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnSkip.Click += (s, e) =>
            {
                if (_manifest.Mandatory)
                    this.DialogResult = DialogResult.Abort;
                else
                    this.DialogResult = DialogResult.Cancel;
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblVersion, lblNotes, txtNotes,
                progressBar, lblProgress, btnUpdate, btnSkip
            });

            if (_manifest.Mandatory)
            {
                this.ControlBox = false;
                btnSkip.Text = "退出";
            }
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnSkip.Enabled = false;

            var progress = new Progress<int>(p =>
            {
                progressBar.Value = Math.Min(100, Math.Max(0, p));
                lblProgress.Text = $"下载中... {p}%";
            });

            lblProgress.Text = "开始下载...";

            var path = await Task.Run(async () =>
                await UpdateDownloader.DownloadAsync(
                    _manifest.PackageUrl, _manifest.PackageSha256, progress));

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("下载失败或校验未通过，请稍后重试。", "更新失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblProgress.Text = "下载失败";
                btnUpdate.Enabled = true;
                btnSkip.Enabled = true;
                return;
            }

            DownloadedPackagePath = path;
            UserConfirmed = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}