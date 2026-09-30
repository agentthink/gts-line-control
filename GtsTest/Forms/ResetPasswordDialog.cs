using System;
using System.Windows.Forms;

namespace GtsTest
{
    public partial class ResetPasswordDialog : Form
    {
        public string NewPassword { get; private set; }

        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Button btnOK;
        private Button btnCancel;
        private Label lblStatus;

        public ResetPasswordDialog(string username)
        {
            InitializeComponent();
            this.Text = $"重置密码 - {username}";
        }

        private void InitializeComponent()
        {
            this.txtNewPassword = new TextBox();
            this.txtConfirmPassword = new TextBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblStatus = new Label();

            this.SuspendLayout();

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 4;
            layout.Padding = new Padding(15);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            layout.Controls.Add(new Label { Text = "新密码:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtNewPassword.PasswordChar = '*';
            txtNewPassword.Dock = DockStyle.Fill;
            layout.Controls.Add(txtNewPassword, 1, 0);

            layout.Controls.Add(new Label { Text = "确认密码:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            txtConfirmPassword.PasswordChar = '*';
            txtConfirmPassword.Dock = DockStyle.Fill;
            layout.Controls.Add(txtConfirmPassword, 1, 1);

            lblStatus.Text = "请输入至少6位密码";
            lblStatus.ForeColor = System.Drawing.Color.Gray;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            layout.Controls.Add(lblStatus, 1, 2);

            var btnPanel = new FlowLayoutPanel();
            btnPanel.Dock = DockStyle.Fill;
            btnPanel.FlowDirection = FlowDirection.RightToLeft;
            btnPanel.Padding = new Padding(0, 5, 0, 0);

            btnOK.Text = "确定";
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Size = new Size(80, 30);
            btnOK.Click += BtnOK_Click;

            btnCancel.Text = "取消";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(80, 30);

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOK);
            layout.Controls.Add(btnPanel, 1, 3);

            this.Controls.Add(layout);

            this.ClientSize = new Size(380, 170);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            txtNewPassword.TextChanged += (s, e) => ValidatePassword();
            txtConfirmPassword.TextChanged += (s, e) => ValidatePassword();

            this.ResumeLayout(false);
        }

        private void ValidatePassword()
        {
            string pwd = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(pwd))
            {
                lblStatus.Text = "请输入至少6位密码";
                lblStatus.ForeColor = System.Drawing.Color.Gray;
                btnOK.Enabled = false;
                return;
            }

            if (pwd.Length < 6)
            {
                lblStatus.Text = "密码至少6位";
                lblStatus.ForeColor = System.Drawing.Color.Orange;
                btnOK.Enabled = false;
                return;
            }

            if (string.IsNullOrEmpty(confirm))
            {
                lblStatus.Text = "请确认密码";
                lblStatus.ForeColor = System.Drawing.Color.Gray;
                btnOK.Enabled = false;
                return;
            }

            if (pwd != confirm)
            {
                lblStatus.Text = "两次输入的密码不一致";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                btnOK.Enabled = false;
                return;
            }

            lblStatus.Text = "✅ 密码有效";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            btnOK.Enabled = true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtNewPassword.Text) || txtNewPassword.Text.Length < 6)
            {
                MessageBox.Show("密码至少6位", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("两次输入的密码不一致", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            NewPassword = txtNewPassword.Text;
            this.DialogResult = DialogResult.OK;
        }
    }
}