using System;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AudienciasApp
{
    public partial class App : Application  // Hereda de Application, no de ApplicationBase
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configurar tema inicial
            ApplicationThemeManager.Apply(
                ApplicationTheme.Dark,  // o Light según preferencia
                WindowBackdropType.Mica,
                true
            );
        }

        private void OnDispatcherUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"Ha ocurrido un error inesperado:\n\n{e.Exception.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error
            );

            e.Handled = true;
        }
    }
}
