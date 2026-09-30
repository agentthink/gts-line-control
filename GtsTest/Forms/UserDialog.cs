using System;
using System.Windows.Forms;
using GtsTest.Models;

namespace GtsTest
{
    public partial class UserDialog : Form
    {
        public string Username { get; private set; }
        public string FullName { get; private set; }
        public string Password { get; private set; }
        public string Role { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsEditMode { get; private set; }

        private TextBox txtUsername;
        private TextBox txtFullName;
        private TextBox txtPassword;
        private ComboBox cmbRole;
        private CheckBox chkActive;
        private Button btnOK;
        private Button btnCancel;

        public UserDialog(User existingUser = null)
        {
            InitializeComponent();
            IsEditMode = existingUser != null;

            if (IsEditMode)
            {
                this.Text = "编辑用户";
                txtUsername.Text = existingUser.Username;
                txtUsername.Enabled = false;
                txtFullName.Text = existingUser.FullName;
                cmbRole.SelectedItem = existingUser.Role;
                chkActive.Checked = existingUser.IsActive == 1;
                txtPassword.Visible = false;
                this.ClientSize = new Size(340, 240);
            }
            else
            {
                this.Text = "添加用户";
                cmbRole.SelectedIndex = 0;
                chkActive.Checked = true;
                txtPassword.Visible = true;
                this.ClientSize = new Size(340, 280);
            }

            this.MinimumSize = new Size(320, 200);
            this.MaximumSize = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void InitializeComponent()
        {
            this.txtUsername = new TextBox();
            this.txtFullName = new TextBox();
            this.txtPassword = new TextBox();
            this.cmbRole = new ComboBox();
            this.chkActive = new CheckBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();

            this.SuspendLayout();

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 6;
            layout.Padding = new Padding(10);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            layout.Controls.Add(new Label { Text = "用户名:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            layout.Controls.Add(txtUsername, 1, 0);
            layout.Controls.Add(new Label { Text = "全名:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            layout.Controls.Add(txtFullName, 1, 1);
            layout.Controls.Add(new Label { Text = "密码:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            txtPassword.PasswordChar = '*';
            layout.Controls.Add(txtPassword, 1, 2);
            layout.Controls.Add(new Label { Text = "角色:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Items.AddRange(new object[] { "Admin", "Engineer", "Operator" });
            layout.Controls.Add(cmbRole, 1, 3);
            layout.Controls.Add(new Label { Text = "启用:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
            layout.Controls.Add(chkActive, 1, 4);

            var btnPanel = new FlowLayoutPanel();
            btnPanel.Dock = DockStyle.Fill;
            btnPanel.FlowDirection = FlowDirection.RightToLeft;
            btnPanel.Padding = new Padding(0, 5, 0, 0);

            btnOK.Text = "确定";
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Size = new Size(80, 30);
            btnOK.UseVisualStyleBackColor = true;

            btnCancel.Text = "取消";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOK);

            layout.Controls.Add(btnPanel, 1, 5);

            this.Controls.Add(layout);
            this.ResumeLayout(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("请输入用户名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                Username = txtUsername.Text.Trim();
                FullName = txtFullName.Text.Trim();
                Password = txtPassword.Text.Trim();
                Role = cmbRole.SelectedItem?.ToString() ?? "Operator";
                IsActive = chkActive.Checked;

                if (!IsEditMode && string.IsNullOrEmpty(Password))
                {
                    MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }
    }
}