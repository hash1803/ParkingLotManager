using Microsoft.Data.Sqlite;
using ParkingLotManager.Models;

namespace ParkingLotManager.Data;

public sealed class AuthRepository
{
    public (long Id, string Username, string Hash, string Salt, string Role, long? StaffId, bool Active)? Find(string username)
    {
        using var db = Db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT AccountId,Username,PasswordHash,PasswordSalt,Role,StaffId,IsActive FROM Accounts WHERE Username=$u COLLATE NOCASE";
        cmd.Parameters.AddWithValue("$u", username);
        using var r = cmd.ExecuteReader(); if (!r.Read()) return null;
        return (r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.IsDBNull(5) ? null : r.GetInt64(5), r.GetInt64(6) == 1);
    }
}

public sealed class ParkingRepository
{
    public List<SpotView> GetSpots()
    {
        using var db = Db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = """
          SELECT s.SpotId,s.AreaId,a.AreaName,a.AllowedVehicleType,s.SpotCode,s.Status,t.PlateNumber,t.TicketCode,t.VehicleType,t.CheckInAt
          FROM ParkingSpots s JOIN ParkingAreas a ON a.AreaId=s.AreaId
          LEFT JOIN Tickets t ON t.SpotId=s.SpotId AND t.Status='Active' ORDER BY a.AreaCode,s.SpotCode
          """;
        using var r = cmd.ExecuteReader(); var result = new List<SpotView>();
        while (r.Read()) result.Add(new(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? null : r.GetString(8),
            r.IsDBNull(9) ? null : DateTimeOffset.Parse(r.GetString(9))));
        return result;
    }

    public long CheckIn(string plate, string vehicleType, long spotId, string ticketCode, long? staffId)
    {
        using var db = Db.Open(); using var tx = db.BeginTransaction();
        using var check = db.CreateCommand(); check.Transaction = tx;
        check.CommandText = "SELECT s.Status,a.AllowedVehicleType FROM ParkingSpots s JOIN ParkingAreas a ON a.AreaId=s.AreaId WHERE s.SpotId=$id"; check.Parameters.AddWithValue("$id", spotId);
        using var spotReader = check.ExecuteReader();
        if (!spotReader.Read() || spotReader.GetString(0) != "Available") throw new InvalidOperationException("Vị trí này không còn trống.");
        if (spotReader.GetString(1) != vehicleType) throw new InvalidOperationException("Loại xe không phù hợp với khu vực đã chọn.");
        spotReader.Close();
        var now = DateTimeOffset.Now.ToString("O");
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = """
          INSERT INTO Tickets(TicketCode,PlateNumber,VehicleType,SpotId,PricePlanId,CheckInAt,CheckInStaffId)
          VALUES($code,$plate,$type,$spot,(SELECT PricePlanId FROM PricePlans WHERE VehicleType=$type AND IsActive=1 AND ValidFrom<=date('now') ORDER BY ValidFrom DESC LIMIT 1),$at,$staff)
          """;
        cmd.Parameters.AddWithValue("$code", ticketCode); cmd.Parameters.AddWithValue("$plate", plate.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("$type", vehicleType); cmd.Parameters.AddWithValue("$spot", spotId); cmd.Parameters.AddWithValue("$at", now);
        cmd.Parameters.AddWithValue("$staff", (object?)staffId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        using var idCommand = db.CreateCommand(); idCommand.Transaction = tx; idCommand.CommandText = "SELECT last_insert_rowid()";
        var id = (long)idCommand.ExecuteScalar()!;
        using var occupy = db.CreateCommand(); occupy.Transaction = tx; occupy.CommandText = "UPDATE ParkingSpots SET Status='Occupied' WHERE SpotId=$spot";
        occupy.Parameters.AddWithValue("$spot", spotId); occupy.ExecuteNonQuery();
        tx.Commit(); return id;
    }

    public ActiveTicket? FindActiveTicket(string codeOrPlate)
    {
        using var db = Db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT t.TicketId,t.TicketCode,t.PlateNumber,t.VehicleType,t.SpotId,s.SpotCode,t.CheckInAt FROM Tickets t JOIN ParkingSpots s ON s.SpotId=t.SpotId WHERE t.Status='Active' AND (t.TicketCode=$q OR t.PlateNumber=$q COLLATE NOCASE) LIMIT 1";
        cmd.Parameters.AddWithValue("$q", codeOrPlate.Trim()); using var r = cmd.ExecuteReader();
        return r.Read() ? new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetInt64(4),r.GetString(5),DateTimeOffset.Parse(r.GetString(6))) : null;
    }

    public void CheckOut(ActiveTicket ticket, DateTimeOffset at, long amount, string paymentMethod, long? staffId)
    {
        using var db = Db.Open(); using var tx = db.BeginTransaction();
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Tickets SET Status='Closed',CheckOutAt=$out,CheckOutStaffId=$staff WHERE TicketId=$id AND Status='Active'";
        cmd.Parameters.AddWithValue("$out", at.ToString("O")); cmd.Parameters.AddWithValue("$staff", (object?)staffId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", ticket.TicketId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Vé này đã được xử lý hoặc không còn hiệu lực.");
        using var free = db.CreateCommand(); free.Transaction = tx; free.CommandText = "UPDATE ParkingSpots SET Status='Available' WHERE SpotId=$spot"; free.Parameters.AddWithValue("$spot", ticket.SpotId); free.ExecuteNonQuery();
        using var invoice = db.CreateCommand(); invoice.Transaction = tx;
        invoice.CommandText = "INSERT INTO Invoices(InvoiceCode,TicketId,Amount,PaymentMethod,IssuedAt,StaffId) VALUES($invoice,$id,$amount,$method,$out,$staff)";
        invoice.Parameters.AddWithValue("$out", at.ToString("O")); invoice.Parameters.AddWithValue("$staff", (object?)staffId ?? DBNull.Value);
        invoice.Parameters.AddWithValue("$id", ticket.TicketId); invoice.Parameters.AddWithValue("$invoice", "HD" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
        invoice.Parameters.AddWithValue("$amount", amount); invoice.Parameters.AddWithValue("$method", paymentMethod); invoice.ExecuteNonQuery();
        tx.Commit();
    }
}
