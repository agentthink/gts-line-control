// LoginForm.cs
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GtsTest
{
    public partial class LoginForm : Form
    {
        private readonly IAuthenticationService _authService;
        private readonly IDataRepository _repo;

        public LoginForm(IAuthenticationService authService, IDataRepository repo)
        {
            InitializeComponent();
            _authService = authService;
            _repo = repo;
            this.btnLogin.Click += BtnLogin_Click;
            this.btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Text; // 保留原始输入（不要 Trim 密码）

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 将验证放到线程池，避免 UI 卡顿（PBKDF2 运算可能比较耗时）
            var result = await Task.Run(() =>
            {
                bool ok = _authService.ValidateCredentials(username, password, out var user);
                return (ok, user);
            }).ConfigureAwait(false);

            // 回到 UI 线程处理结果
            try
            {
                if (result.ok && result.user != null)
                {
                    // 登录成功并设置会话
                    SessionManager.Login(result.user, _repo);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("用户名或密码错误，或账号已禁用/锁定", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (InvalidOperationException)
            {
                // 如果窗体已被关闭或 disposed，忽略
            }
        }
    }
}