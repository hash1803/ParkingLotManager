using ParkingLotManager.Models;
using ParkingLotManager.Services;

namespace ParkingLotManager.UI;

public sealed class CheckOutForm : Form
{
    private readonly ParkingService _service = new();
    private readonly UserSession _session;
    private readonly TextBox _lookup = new() { Width = 260 };
    private readonly Label _details = new() { AutoSize = true, Font = new Font("Segoe UI", 11) };
    private readonly ComboBox _payment = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private CheckoutQuote? _quote;
    public CheckOutForm(UserSession session)
    {
        _session = session; Text = "Xe ra bãi · Check-out"; Width = 570; Height = 380; StartPosition = FormStartPosition.CenterParent;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4 };
        var search = new FlowLayoutPanel { AutoSize = true }; search.Controls.Add(new Label { Text = "Mã vé / biển số", AutoSize = true, Padding = new Padding(0, 7, 8, 0) }); search.Controls.Add(_lookup);
        var find = new Button { Text = "Tra cứu", AutoSize = true }; find.Click += (_, _) => FindTicket(); search.Controls.Add(find);
        var pay = new Button { Text = "Xác nhận thanh toán và cho xe ra", AutoSize = true, Height = 42 }; pay.Click += (_, _) => Complete();
        _payment.Items.AddRange([new PaymentOption("Cash", "Tiền mặt"), new PaymentOption("Card", "Thẻ"), new PaymentOption("Transfer", "Chuyển khoản"), new PaymentOption("Other", "Khác")]); _payment.DisplayMember = "Label"; _payment.SelectedIndex = 0;
        var paymentRow = new FlowLayoutPanel { AutoSize = true }; paymentRow.Controls.Add(new Label { Text = "Phương thức", AutoSize = true, Padding = new Padding(0, 7, 8, 0) }); paymentRow.Controls.Add(_payment);
        root.Controls.Add(search, 0, 0); root.Controls.Add(_details, 0, 1); root.Controls.Add(paymentRow, 0, 2); root.Controls.Add(pay, 0, 3); Controls.Add(root);
    }
    private void FindTicket()
    {
        var ticket = _service.FindActiveTicket(_lookup.Text);
        if (ticket is null) { _quote = null; _details.Text = "Không tìm thấy vé đang gửi."; return; }
        _quote = _service.Quote(ticket, DateTimeOffset.Now);
        _details.Text = $"Mã vé: {ticket.TicketCode}\nBiển số: {ticket.PlateNumber}\nVị trí: {ticket.SpotCode}\nVào lúc: {ticket.CheckInAt:dd/MM/yyyy HH:mm}\nThời gian gửi (làm tròn): {_quote.Hours} giờ\nSố tiền: {_quote.Amount:N0} đ";
    }
    private void Complete()
    {
        if (_quote is null) { MessageBox.Show("Tra cứu vé trước khi thanh toán."); return; }
        var choice = MessageBox.Show($"Xác nhận thu {_quote.Amount:N0} đ và cho xe {_quote.Ticket.PlateNumber} ra?", "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (choice != DialogResult.Yes) return;
        try
        {
            var payment = (PaymentOption)_payment.SelectedItem!;
            _service.CheckOut(_quote, payment.Value, _session.StaffId);
            MessageBox.Show($"Đã thanh toán bằng {payment.Label}. Hóa đơn đã được lập cho vé {_quote.Ticket.TicketCode}.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _quote = null; _details.Text = ""; _lookup.Clear();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể check-out", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private sealed record PaymentOption(string Value, string Label) { public override string ToString() => Label; }
}
