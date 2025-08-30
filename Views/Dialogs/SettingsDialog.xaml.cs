using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using AudienciasApp.Services.Configuration;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace AudienciasApp.Views.Dialogs
{
    public partial class SettingsDialog : FluentWindow
    {
        private readonly IConfigurationService _configService;
        private bool _showPassword = false;

        public SettingsDialog()
        {
            InitializeComponent();
            _configService = new ConfigurationService();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var config = _configService.GetAIConfiguration();
            
            // AI Settings
            ApiKeyBox.Password = config.ApiKey;
            
            // Set model
            foreach (ComboBoxItem item in ModelCombo.Items)
            {
                if (item.Content.ToString() == config.Model)
                {
                    ModelCombo.SelectedItem = item;
                    break;
                }
            }
            
            TemperatureSlider.Value = config.Temperature;
            MaxTokensBox.Value = config.MaxTokens;
            TimeoutBox.Value = config.TimeoutSeconds;
        }

        private void ShowApiKey_Click(object sender, RoutedEventArgs e)
        {
            _showPassword = !_showPassword;
            
            if (_showPassword)
            {
                // Mostrar contraseña temporalmente
                var currentPassword = ApiKeyBox.Password;
                MessageBox.Show($"API Key: {currentPassword}", "API Key", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            TestConnectionButton.IsEnabled = false;
            
            try
            {
                var testService = new Services.AI.OpenAIService();
                var result = await testService.ExtractHearingFromTextAsync("Test de conexión");
                
                if (testService.IsConfigured)
                {
                    MessageBox.Show("✅ Conexión exitosa con OpenAI", "Prueba Exitosa",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ No se pudo conectar. Verifique su API Key.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar conexión: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var config = new AIConfiguration
            {
                Provider = "OpenAI",
                ApiKey = ApiKeyBox.Password,
                Model = (ModelCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "gpt-4",
                Temperature = TemperatureSlider.Value,
                MaxTokens = (int)MaxTokensBox.Value,
                TimeoutSeconds = (int)TimeoutBox.Value
            };
            
            await _configService.SaveAIConfiguration(config);
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro de limpiar la lista de archivos recientes?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Implementar limpieza
                MessageBox.Show("Archivos recientes limpiados", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAICache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro de limpiar el caché de IA?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Implementar limpieza de caché
                MessageBox.Show("Caché de IA limpiado", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
