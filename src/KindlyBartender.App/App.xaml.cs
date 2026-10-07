using System.Windows;
using KindlyBartender.App.Configuration;

namespace KindlyBartender.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The elevated copy started by HearthstoneSetup does one write and exits, before any other startup work.
        // Any other arguments after the switch are rejected rather than starting the whole app with admin rights.
        if (e.Args is [HearthstoneSetup.WriteClientConfigSwitch, ..])
        {
            Shutdown(e.Args is [_, var installFolder] ? HearthstoneSetup.RunElevatedWrite(installFolder) : 1);
            return;
        }
    }
}
