using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

#nullable enable

namespace AudienciasApp.Services
{
    public class NotificationService
    {
        private SnackbarPresenter? _snackbarPresenter;

        public void Initialize(SnackbarPresenter snackbarPresenter)
        {
            _snackbarPresenter = snackbarPresenter;
        }

        public async Task ShowAsync(string message, ControlAppearance appearance = ControlAppearance.Secondary)
        {
            if (_snackbarPresenter != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var snackbar = new Snackbar(_snackbarPresenter)
                    {
                        Title = "Notificación",
                        Content = message,
                        Appearance = appearance
                    };
                    snackbar.Show();
                });
            }
        }

        public async Task ShowAsync(string title, string message, ControlAppearance appearance = ControlAppearance.Secondary)
        {
            if (_snackbarPresenter != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var snackbar = new Snackbar(_snackbarPresenter)
                    {
                        Title = title,
                        Content = message,
                        Appearance = appearance
                    };
                    snackbar.Show();
                });
            }
        }
    }
}