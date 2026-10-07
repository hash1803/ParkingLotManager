using System.Drawing.Printing;
using ParkingLotManager.Models;
using ParkingLotManager.Services;

namespace ParkingLotManager.UI;

public sealed class CheckInForm : Form
{
    private sealed record SpotOption(long Id, string Name) { public override string ToString() => Name; }
    private readonly ParkingService _service = new();
    private readonly UserSession _session;
    private readonly TextBox _plate = new() { Width = 220 };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox _spot = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private string _lastTicket = "";

    public CheckInForm(UserSession session)
    {
        _session = session; Text = "Xe vào bãi · Check-in"; Width = 520; Height = 390; StartPosition = FormStartPosition.CenterParent;
        _type.Items.AddRange([new VehicleOption("Motorbike", "Xe máy"), new VehicleOption("Car", "Ô tô"), new VehicleOption("Bicycle", "Xe đạp"), new VehicleOption("Other", "Khác")]); _type.DisplayMember = "Label"; _type.SelectedIndex = 0;
        _type.SelectedIndexChanged += (_, _) => ReloadSpots();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 6 };
        layout.ColumnStyles.Add(new(SizeType.Absolute, 140)); layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        AddRow("Biển số xe", _plate, 0); AddRow("Loại xe", _type, 1); AddRow("Vị trí trống", _spot, 2);
        AddRow("Thời gian vào", new Label { Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), AutoSize = true }, 3);
        AddRow("Nhân viên", new Label { Text = session.Username, AutoSize = true }, 4);
        var actions = new FlowLayoutPanel { AutoSize = true };
        var submit = new Button { Text = "Nhận xe và tạo vé", AutoSize = true }; submit.Click += (_, _) => Save();
        var print = new Button { Text = "In vé gần nhất", AutoSize = true }; print.Click += (_, _) => PrintTicket();
        actions.Controls.Add(submit); actions.Controls.Add(print); AddRow("", actions, 5);
        Controls.Add(layout); ReloadSpots();
        void AddRow(string label, Control control, int row) { layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row); layout.Controls.Add(control, 1, row); }
    }

    private sealed record VehicleOption(string Value, string Label) { public override string ToString() => Label; }
    private void ReloadSpots()
    {
        _spot.Items.Clear();
        if (_type.SelectedItem is not VehicleOption vehicle) return;
        foreach (var s in _service.GetSpots().Where(x => x.Status == "Available" && x.AllowedVehicleType == vehicle.Value)) _spot.Items.Add(new SpotOption(s.SpotId, $"{s.AreaName} · {s.SpotCode}"));
        if (_spot.Items.Count > 0) _spot.SelectedIndex = 0;
    }
    private void Save()
    {
        try
        {
            if (_spot.SelectedItem is not SpotOption selected) throw new InvalidOperationException("Không có vị trí trống phù hợp.");
            var vehicle = (VehicleOption)_type.SelectedItem!;
            _service.CheckIn(_plate.Text, vehicle.Value, selected.Id, _session.StaffId);
            var ticket = _service.FindActiveTicket(_plate.Text)!; _lastTicket = ticket.TicketCode;
            MessageBox.Show($"Đã nhận xe.\nMã vé: {_lastTicket}\nBiển số: {ticket.PlateNumber}\nVị trí: {ticket.SpotCode}", "Tạo vé thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadSpots();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể check-in", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void PrintTicket()
    {
        if (string.IsNullOrEmpty(_lastTicket)) { MessageBox.Show("Hãy nhận xe trước khi in vé."); return; }
        using var document = new PrintDocument();
        document.PrintPage += (_, e) => { using var font = new Font("Segoe UI", 11); e.Graphics!.DrawString($"VÉ GỬI XE\nMã vé: {_lastTicket}\nBiển số: {_plate.Text}\nThời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nGiữ vé để làm thủ tục lấy xe", font, Brushes.Black, 30, 30); };
        using var preview = new PrintPreviewDialog { Document = document, Width = 800, Height = 600 }; preview.ShowDialog(this);
    }
}
