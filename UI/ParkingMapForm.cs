using ParkingLotManager.Models;
using ParkingLotManager.Services;

namespace ParkingLotManager.UI;

public sealed class ParkingMapForm : Form
{
    private readonly ParkingService _service = new();
    private readonly FlowLayoutPanel _map = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
    public ParkingMapForm()
    {
        Text = "Sơ đồ vị trí đỗ xe"; Width = 900; Height = 620;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(14) };
        top.Controls.Add(new Label { Text = "🟢 Trống     🔴 Đang đỗ     🟡 Đặt trước     ⚙ Bảo trì", AutoSize = true, Font = new Font("Segoe UI", 11) });
        var refresh = new Button { Text = "Làm mới", AutoSize = true }; refresh.Click += (_, _) => LoadSpots(); top.Controls.Add(refresh);
        Controls.Add(_map); Controls.Add(top); LoadSpots();
    }
    private void LoadSpots()
    {
        _map.Controls.Clear();
        foreach (var spot in _service.GetSpots())
        {
            var button = new Button { Width = 118, Height = 74, Margin = new Padding(7), Text = spot.SpotCode + Environment.NewLine + Label(spot.Status), BackColor = ColorFor(spot.Status), ForeColor = Color.Black, Tag = spot };
            button.Click += (_, _) => ShowDetails((SpotView)button.Tag!); _map.Controls.Add(button);
        }
    }
    private static string Label(string status) => status switch { "Available" => "Trống", "Occupied" => "Đang đỗ", "Reserved" => "Đặt trước", _ => "Bảo trì" };
    private static Color ColorFor(string status) => status switch { "Available" => Color.LightGreen, "Occupied" => Color.LightCoral, "Reserved" => Color.Khaki, _ => Color.LightGray };
    private void ShowDetails(SpotView s)
    {
        var detail = s.PlateNumber is null ? $"{s.AreaName} / {s.SpotCode}\nTrạng thái: {Label(s.Status)}" :
            $"{s.AreaName} / {s.SpotCode}\nTrạng thái: Đang đỗ\nBiển số: {s.PlateNumber}\nLoại xe: {s.VehicleType}\nMã vé: {s.TicketCode}\nVào lúc: {s.CheckInAt:dd/MM/yyyy HH:mm}";
        MessageBox.Show(detail, "Chi tiết vị trí", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
