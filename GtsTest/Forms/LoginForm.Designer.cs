using System;
using System.Windows.Forms;

namespace GtsTest
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Button btnCancel;
        private Label lblUser;
        private Label lblPass;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtUser = new TextBox();
            this.txtPass = new TextBox();
            this.btnLogin = new Button();
            this.btnCancel = new Button();
            this.lblUser = new Label();
            this.lblPass = new Label();
            this.SuspendLayout();
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new System.Drawing.Point(20, 20);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(56, 17);
            this.lblUser.Text = "用户名:";
            // 
            // txtUser
            // 
            this.txtUser.Location = new System.Drawing.Point(90, 17);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(150, 23);
            // 
            // lblPass
            // 
            this.lblPass.AutoSize = true;
            this.lblPass.Location = new System.Drawing.Point(20, 55);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(44, 17);
            this.lblPass.Text = "密码:";
            // 
            // txtPass
            // 
            this.txtPass.Location = new System.Drawing.Point(90, 52);
            this.txtPass.Name = "txtPass";
            this.txtPass.Size = new System.Drawing.Size(150, 23);
            this.txtPass.PasswordChar = '*';
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(90, 90);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(75, 30);
            this.btnLogin.Text = "登录";
            this.btnLogin.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(171, 90);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(270, 140);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.txtPass);
            this.Controls.Add(this.lblPass);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.lblUser);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "登录 - GTS产线系统";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}