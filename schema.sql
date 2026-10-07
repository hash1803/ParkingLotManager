PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Staff (
  StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
  StaffCode TEXT NOT NULL UNIQUE,
  FullName TEXT NOT NULL,
  DateOfBirth TEXT,
  Phone TEXT,
  Address TEXT,
  Position TEXT NOT NULL DEFAULT 'Nhân viên',
  ShiftName TEXT,
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
);
CREATE TABLE IF NOT EXISTS Accounts (
  AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
  PasswordHash TEXT NOT NULL,
  PasswordSalt TEXT NOT NULL,
  Role TEXT NOT NULL CHECK (Role IN ('Admin','Staff')),
  StaffId INTEGER UNIQUE REFERENCES Staff(StaffId) ON DELETE SET NULL,
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
  CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE TABLE IF NOT EXISTS ParkingAreas (
  AreaId INTEGER PRIMARY KEY AUTOINCREMENT,
  AreaCode TEXT NOT NULL UNIQUE,
  AreaName TEXT NOT NULL,
  AllowedVehicleType TEXT NOT NULL CHECK (AllowedVehicleType IN ('Car','Motorbike','Bicycle','Other')),
  Status TEXT NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Maintenance'))
);
CREATE TABLE IF NOT EXISTS ParkingSpots (
  SpotId INTEGER PRIMARY KEY AUTOINCREMENT,
  AreaId INTEGER NOT NULL REFERENCES ParkingAreas(AreaId) ON DELETE RESTRICT,
  SpotCode TEXT NOT NULL,
  Status TEXT NOT NULL DEFAULT 'Available' CHECK (Status IN ('Available','Occupied','Reserved','Maintenance')),
  UNIQUE(AreaId, SpotCode)
);
CREATE TABLE IF NOT EXISTS Customers (
  CustomerId INTEGER PRIMARY KEY AUTOINCREMENT,
  CustomerCode TEXT UNIQUE,
  FullName TEXT NOT NULL,
  Phone TEXT,
  Email TEXT,
  Address TEXT,
  CustomerType TEXT NOT NULL DEFAULT 'WalkIn' CHECK (CustomerType IN ('WalkIn','Monthly')),
  CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE TABLE IF NOT EXISTS CustomerVehicles (
  CustomerVehicleId INTEGER PRIMARY KEY AUTOINCREMENT,
  CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId) ON DELETE CASCADE,
  PlateNumber TEXT NOT NULL UNIQUE COLLATE NOCASE,
  VehicleType TEXT NOT NULL CHECK (VehicleType IN ('Car','Motorbike','Bicycle','Other')),
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
);
CREATE TABLE IF NOT EXISTS PricePlans (
  PricePlanId INTEGER PRIMARY KEY AUTOINCREMENT,
  PlanName TEXT NOT NULL,
  VehicleType TEXT NOT NULL CHECK (VehicleType IN ('Car','Motorbike','Bicycle','Other')),
  HourlyRate INTEGER NOT NULL DEFAULT 0 CHECK (HourlyRate >= 0),
  DailyRate INTEGER NOT NULL DEFAULT 0 CHECK (DailyRate >= 0),
  OvernightRate INTEGER NOT NULL DEFAULT 0 CHECK (OvernightRate >= 0),
  MonthlyRate INTEGER NOT NULL DEFAULT 0 CHECK (MonthlyRate >= 0),
  ValidFrom TEXT NOT NULL,
  ValidTo TEXT,
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
  CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_PricePlans_Name_Type_From ON PricePlans(PlanName, VehicleType, ValidFrom);
CREATE TABLE IF NOT EXISTS Tickets (
  TicketId INTEGER PRIMARY KEY AUTOINCREMENT,
  TicketCode TEXT NOT NULL UNIQUE,
  PlateNumber TEXT NOT NULL,
  VehicleType TEXT NOT NULL CHECK (VehicleType IN ('Car','Motorbike','Bicycle','Other')),
  SpotId INTEGER NOT NULL REFERENCES ParkingSpots(SpotId) ON DELETE RESTRICT,
  CustomerId INTEGER REFERENCES Customers(CustomerId) ON DELETE SET NULL,
  PricePlanId INTEGER REFERENCES PricePlans(PricePlanId) ON DELETE SET NULL,
  CheckInAt TEXT NOT NULL,
  CheckOutAt TEXT,
  Status TEXT NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Closed','Lost','Cancelled')),
  CheckInStaffId INTEGER REFERENCES Staff(StaffId) ON DELETE SET NULL,
  CheckOutStaffId INTEGER REFERENCES Staff(StaffId) ON DELETE SET NULL,
  CHECK (CheckOutAt IS NULL OR CheckOutAt >= CheckInAt)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Tickets_ActiveSpot ON Tickets(SpotId) WHERE Status='Active';
CREATE UNIQUE INDEX IF NOT EXISTS UX_Tickets_ActivePlate ON Tickets(PlateNumber) WHERE Status='Active';
CREATE TABLE IF NOT EXISTS Invoices (
  InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
  InvoiceCode TEXT NOT NULL UNIQUE,
  TicketId INTEGER NOT NULL UNIQUE REFERENCES Tickets(TicketId) ON DELETE RESTRICT,
  Amount INTEGER NOT NULL CHECK (Amount >= 0),
  PaymentMethod TEXT NOT NULL CHECK (PaymentMethod IN ('Cash','Card','Transfer','Other')),
  IssuedAt TEXT NOT NULL,
  StaffId INTEGER REFERENCES Staff(StaffId) ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS MonthlySubscriptions (
  SubscriptionId INTEGER PRIMARY KEY AUTOINCREMENT,
  CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId) ON DELETE RESTRICT,
  PlateNumber TEXT NOT NULL,
  VehicleType TEXT NOT NULL CHECK (VehicleType IN ('Car','Motorbike','Bicycle','Other')),
  StartsAt TEXT NOT NULL,
  ExpiresAt TEXT NOT NULL,
  PaidAmount INTEGER NOT NULL CHECK (PaidAmount >= 0),
  Status TEXT NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Expired','Cancelled')),
  CHECK (ExpiresAt >= StartsAt)
);
CREATE TABLE IF NOT EXISTS Reservations (
  ReservationId INTEGER PRIMARY KEY AUTOINCREMENT,
  ReservationCode TEXT NOT NULL UNIQUE,
  CustomerId INTEGER REFERENCES Customers(CustomerId) ON DELETE SET NULL,
  PlateNumber TEXT NOT NULL,
  VehicleType TEXT NOT NULL CHECK (VehicleType IN ('Car','Motorbike','Bicycle','Other')),
  SpotId INTEGER NOT NULL REFERENCES ParkingSpots(SpotId) ON DELETE RESTRICT,
  ReservedFrom TEXT NOT NULL,
  ReservedUntil TEXT NOT NULL,
  Status TEXT NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Used','Expired','Cancelled')),
  CreatedByStaffId INTEGER REFERENCES Staff(StaffId) ON DELETE SET NULL,
  CHECK (ReservedUntil > ReservedFrom)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Reservations_ActiveSpot ON Reservations(SpotId) WHERE Status='Active';
CREATE INDEX IF NOT EXISTS IX_Tickets_Plate_CheckIn ON Tickets(PlateNumber, CheckInAt DESC);
CREATE INDEX IF NOT EXISTS IX_Invoices_IssuedAt ON Invoices(IssuedAt);
CREATE INDEX IF NOT EXISTS IX_Subscriptions_Expiry ON MonthlySubscriptions(ExpiresAt, Status);
CREATE INDEX IF NOT EXISTS IX_Reservations_Window ON Reservations(ReservedFrom, ReservedUntil, Status);

CREATE VIEW IF NOT EXISTS AreaSummary AS
SELECT a.AreaId,a.AreaCode,a.AreaName,a.AllowedVehicleType,
       COUNT(s.SpotId) AS Capacity,
       COALESCE(SUM(CASE WHEN s.Status='Available' THEN 1 ELSE 0 END),0) AS AvailableCount,
       CASE WHEN a.Status='Maintenance' THEN 'Maintenance'
            WHEN COUNT(s.SpotId)>0 AND SUM(CASE WHEN s.Status='Available' THEN 1 ELSE 0 END)=0 THEN 'Full'
            WHEN COUNT(s.SpotId)=0 THEN 'NoSpots'
            ELSE 'Available' END AS DisplayStatus
FROM ParkingAreas a LEFT JOIN ParkingSpots s ON s.AreaId=a.AreaId
GROUP BY a.AreaId,a.AreaCode,a.AreaName,a.AllowedVehicleType,a.Status;
