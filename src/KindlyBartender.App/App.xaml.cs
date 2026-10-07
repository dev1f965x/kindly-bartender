using System.Windows;
using KindlyBartender.App.Configuration;

namespace KindlyBartender.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The elevated copy started by HearthstoneSetup does one write and exits, before any other startup work.
        if (e.Args is [HearthstoneSetup.WriteClientConfigSwitch, var installFolder])
        {
            Shutdown(HearthstoneSetup.RunElevatedWrite(installFolder));
        }
    }
}
