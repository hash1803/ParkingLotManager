using ParkingLotManager.Models;

namespace ParkingLotManager.UI;

public sealed class MainForm : Form
{
    private readonly UserSession _session;

    private void InitializeComponent()
    {

    }

    public MainForm(UserSession session)
    {
        _session = session; Text = "Hệ thống quản lý bãi đỗ xe"; Width = 1000; Height = 680; StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new(SizeType.Absolute, 72)); root.RowStyles.Add(new(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = $"PARKING LOT MANAGER     |     Xin chào {_session.Username} ({_session.Role})", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 14, FontStyle.Bold), Padding = new Padding(20) }, 0, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
        Add("Sơ đồ vị trí", () => new ParkingMapForm().ShowDialog(this)); Add("Xe vào bãi", () => new CheckInForm(_session).ShowDialog(this)); Add("Xe ra bãi", () => new CheckOutForm(_session).ShowDialog(this));
        Add("Quản lý vé", () => MessageBox.Show("Tra cứu vé đang gửi bằng mã vé hoặc biển số trong màn hình Xe ra bãi."));
        if (_session.Role == "Admin")
        {
            Add("Khu vực, bảng giá, khách hàng", () => MessageBox.Show("Các entity và ràng buộc đã có trong database/schema.sql. Đây là vị trí để mở rộng màn hình CRUD."));
            Add("Nhân viên, tài khoản, phân quyền", () => MessageBox.Show("Bảng Staff và Accounts hỗ trợ liên kết nhân viên, vai trò Admin/Nhân viên và khóa tài khoản."));
            Add("Hóa đơn, thống kê & báo cáo", () => MessageBox.Show("Doanh thu tổng hợp từ Invoices theo IssuedAt; vị trí và xe đang gửi lấy từ ParkingSpots và Tickets."));
        }
        var logout = new Button { Text = "Đăng xuất", Width = 140, Height = 48 };
        logout.Click += (_, _) => { Close(); Application.Restart(); };
        buttons.Controls.Add(logout); root.Controls.Add(buttons, 0, 1); Controls.Add(root);
        void Add(string label, Action action) { var b = new Button { Text = label, Width = 270, Height = 72, Margin = new Padding(10), Font = new Font("Segoe UI", 11) }; b.Click += (_, _) => action(); buttons.Controls.Add(b); }
    }
}
