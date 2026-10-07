using ParkingLotManager.Data;
using ParkingLotManager.UI;

namespace ParkingLotManager;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Db.Initialize();
        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK && login.Session is not null)
            Application.Run(new MainForm(login.Session));
    }
}
