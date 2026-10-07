using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;

namespace ParkingLotManager.Data;

public static class Db
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParkingLotManager");
    private static readonly string DbPath = Path.Combine(Folder, "parking.db");
    public static string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DbPath, ForeignKeys = true }.ToString();

    public static SqliteConnection Open()
    {
        Directory.CreateDirectory(Folder);
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        using var connection = Open();
        using (var command = connection.CreateCommand())
        {
            using var schema = Assembly.GetExecutingAssembly().GetManifestResourceStream("ParkingLotManager.schema.sql")!;
            using var reader = new StreamReader(schema, Encoding.UTF8);
            command.CommandText = reader.ReadToEnd();
            command.ExecuteNonQuery();
        }
        using var seed = connection.CreateCommand();
        seed.CommandText = """
            INSERT OR IGNORE INTO Staff(StaffCode,FullName,Position) VALUES ('NV001','Quản trị viên','Admin');
            INSERT OR IGNORE INTO Accounts(Username,PasswordHash,PasswordSalt,Role,StaffId)
            SELECT 'admin', $hash, $salt, 'Admin', StaffId FROM Staff WHERE StaffCode='NV001';
            INSERT OR IGNORE INTO ParkingAreas(AreaCode,AreaName,AllowedVehicleType) VALUES
              ('A','Khu xe máy','Motorbike'),('B','Khu ô tô','Car'),('C','Khu xe đạp','Bicycle'),('D','Khu phương tiện khác','Other');
            INSERT OR IGNORE INTO ParkingSpots(AreaId,SpotCode)
            SELECT AreaId, AreaCode || printf('%02d', n) FROM ParkingAreas, (SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15 UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20);
            INSERT OR IGNORE INTO PricePlans(PlanName,VehicleType,HourlyRate,DailyRate,OvernightRate,MonthlyRate,ValidFrom)
            VALUES ('Giá mặc định xe máy','Motorbike',5000,50000,30000,500000,date('now'));
            INSERT OR IGNORE INTO PricePlans(PlanName,VehicleType,HourlyRate,DailyRate,OvernightRate,MonthlyRate,ValidFrom)
            VALUES ('Giá mặc định ô tô','Car',20000,150000,100000,2000000,date('now'));
            INSERT OR IGNORE INTO PricePlans(PlanName,VehicleType,HourlyRate,DailyRate,OvernightRate,MonthlyRate,ValidFrom)
            VALUES ('Giá mặc định xe đạp','Bicycle',2000,15000,10000,150000,date('now'));
            INSERT OR IGNORE INTO PricePlans(PlanName,VehicleType,HourlyRate,DailyRate,OvernightRate,MonthlyRate,ValidFrom)
            VALUES ('Giá mặc định khác','Other',10000,80000,50000,800000,date('now'));
            """;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2("Admin@123", salt, 210_000, HashAlgorithmName.SHA256, 32);
        seed.Parameters.AddWithValue("$hash", Convert.ToBase64String(hash));
        seed.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
        seed.ExecuteNonQuery();
    }
}
