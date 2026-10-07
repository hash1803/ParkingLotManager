namespace ParkingLotManager.Models;

public sealed record UserSession(long AccountId, string Username, string Role, long? StaffId);
public sealed record SpotView(long SpotId, long AreaId, string AreaName, string AllowedVehicleType, string SpotCode, string Status,
    string? PlateNumber, string? TicketCode, string? VehicleType, DateTimeOffset? CheckInAt);
public sealed record ActiveTicket(long TicketId, string TicketCode, string PlateNumber, string VehicleType,
    long SpotId, string SpotCode, DateTimeOffset CheckInAt);
public sealed record CheckoutQuote(ActiveTicket Ticket, DateTimeOffset CheckOutAt, decimal Hours, long Amount);
