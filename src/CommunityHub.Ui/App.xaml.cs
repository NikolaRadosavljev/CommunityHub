using System;
using System.Windows;
using CommunityHub.Ui.Converters;

namespace CommunityHub.Ui;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Resources["BoolToStatusConverter"] = new BoolToStatusConverter();

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.ExceptionObject.ToString());
        };

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.Exception.ToString());
            ex.Handled = true;
        };
    }
}