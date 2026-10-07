using Microsoft.Data.Sqlite;
using ParkingLotManager.Data;
using ParkingLotManager.Models;

namespace ParkingLotManager.Services;

public sealed class ParkingService
{
    private readonly ParkingRepository _repository = new();
    public List<SpotView> GetSpots() => _repository.GetSpots();
    public void CheckIn(string plate, string vehicleType, long spotId, long? staffId)
    {
        if (string.IsNullOrWhiteSpace(plate)) throw new InvalidOperationException("Vui lòng nhập biển số xe.");
        var ticket = "VE" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        _repository.CheckIn(plate, vehicleType, spotId, ticket, staffId);
    }
    public ActiveTicket? FindActiveTicket(string codeOrPlate) => _repository.FindActiveTicket(codeOrPlate);
    public CheckoutQuote Quote(ActiveTicket ticket, DateTimeOffset at)
    {
        var hours = Math.Max(1, (decimal)Math.Ceiling((at - ticket.CheckInAt).TotalHours));
        using var db = Db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT HourlyRate,DailyRate,OvernightRate FROM PricePlans WHERE VehicleType=$type AND IsActive=1 AND ValidFrom<=date('now') ORDER BY ValidFrom DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$type", ticket.VehicleType); using var r = cmd.ExecuteReader();
        long hourly=5000, daily=50000, overnight=30000;
        if (r.Read()) { hourly=r.GetInt64(0); daily=r.GetInt64(1); overnight=r.GetInt64(2); }
        var fullDays = (long)(hours / 24); var remainingHours = (long)(hours % 24);
        var amount = fullDays * daily + Math.Min(remainingHours * hourly, overnight > 0 ? overnight : long.MaxValue);
        return new(ticket, at, hours, amount);
    }
    public void CheckOut(CheckoutQuote quote, string paymentMethod, long? staffId) =>
        _repository.CheckOut(quote.Ticket, quote.CheckOutAt, quote.Amount, paymentMethod, staffId);
}
