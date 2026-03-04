using System.Windows;
using VoidTerminal.Services;
using VoidTerminal.Views;

namespace VoidTerminal;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (SecurityManager.DatabaseExists())
        {
            new LoginWindow().Show();
        }
        else
        {
            new SetupWindow().Show();
        }
    }
}