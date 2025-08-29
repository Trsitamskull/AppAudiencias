using System.Windows;
using Wpf.Ui.Controls;
using AudienciasApp.Services;

namespace AudienciasApp.Views
{
    public partial class MainWindow : FluentWindow
    {
        private readonly NotificationService _notificationService;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar el servicio de notificaciones: buscar el Snackbar en el árbol visual
            _notificationService = new NotificationService();
            this.Loaded += (s, e) =>
            {
                var presenter = this.FindName("RootSnackbar") as Wpf.Ui.Controls.SnackbarPresenter;
                if (presenter == null)
                {
                    // Buscar por tipo en el árbol visual si no se encontró por name
                    presenter = FindVisualChild<Wpf.Ui.Controls.SnackbarPresenter>(this);
                }
                if (presenter != null)
                    _notificationService.Initialize(presenter);
            };

            // Aplicar efecto de entrada
            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Animación de entrada suave
            this.Opacity = 0;
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                System.TimeSpan.FromMilliseconds(500));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        // Utilidad para buscar un hijo visual por tipo
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
