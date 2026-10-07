using ParkingLotManager.Models;
using ParkingLotManager.Services;

namespace ParkingLotManager.UI;

public sealed class LoginForm : Form
{
    private readonly TextBox _username = new() { Width = 240, Text = "admin" };
    private readonly TextBox _password = new() { Width = 240, UseSystemPasswordChar = true };
    private readonly AuthService _auth = new();
    public UserSession? Session { get; private set; }

    public LoginForm()
    {
        Text = "Đăng nhập · ParkingLotManager"; Width = 380; Height = 260; StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new(SizeType.Absolute, 105)); layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Tên tài khoản", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); layout.Controls.Add(_username, 1, 0);
        layout.Controls.Add(new Label { Text = "Mật khẩu", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1); layout.Controls.Add(_password, 1, 1);
        var signIn = new Button { Text = "Đăng nhập", Width = 110, DialogResult = DialogResult.None };
        signIn.Click += (_, _) => SignIn(); layout.Controls.Add(signIn, 1, 2);
        layout.Controls.Add(new Label { Text = "Mẫu: admin / Admin@123", AutoSize = true, ForeColor = Color.DimGray }, 1, 3);
        Controls.Add(layout); AcceptButton = signIn;
    }
    private void SignIn()
    {
        Session = _auth.SignIn(_username.Text, _password.Text);
        if (Session is null) { MessageBox.Show("Tên tài khoản hoặc mật khẩu không hợp lệ.", "Đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        DialogResult = DialogResult.OK; Close();
    }
}
