using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AudienciasApp.Services.AI;
using Wpf.Ui.Controls;

namespace AudienciasApp.Views.Dialogs
{
    public partial class AIAutoCompleteDialog : FluentWindow
    {
        private readonly IAIService _aiService;
        public HearingExtractionResult ExtractedHearing { get; private set; }

        public AIAutoCompleteDialog()
        {
            InitializeComponent();
            _aiService = new OpenAIService();

            // Configurar eventos
            PreviewWasHeldNo.Checked += (s, e) => UpdateReasonVisibility();
            PreviewWasHeldYes.Checked += (s, e) => UpdateReasonVisibility();
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var inputText = InputTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(inputText))
            {
                System.Windows.MessageBox.Show("Por favor ingrese una descripción de la audiencia",
                    "Información Requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            if (!_aiService.IsConfigured)
            {
                System.Windows.MessageBox.Show("El servicio de IA no está configurado. Por favor configure la API key en la configuración.",
                    "Configuración Requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Mostrar progreso
            ProgressCard.Visibility = Visibility.Visible;
            ProcessButton.IsEnabled = false;

            try
            {
                // Llamar al servicio de IA
                ExtractedHearing = await _aiService.ExtractHearingFromTextAsync(inputText);

                if (ExtractedHearing.Success)
                {
                    // Mostrar resultados en vista previa
                    PopulatePreview(ExtractedHearing);

                    // Cambiar a tab de vista previa
                    PreviewTab.IsEnabled = true;
                    MainTabs.SelectedItem = PreviewTab;

                    // Habilitar botón de aplicar
                    ApplyButton.IsEnabled = true;

                    // Mostrar mensaje de éxito
                    SuccessInfoBar.IsOpen = true;

                    // Mostrar advertencias si las hay
                    if (ExtractedHearing.Warnings?.Any() == true)
                    {
                        WarningsControl.ItemsSource = ExtractedHearing.Warnings;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("No se pudo extraer información de la descripción proporcionada.",
                        "Error de Procesamiento", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

                    if (ExtractedHearing.Warnings?.Any() == true)
                    {
                        System.Windows.MessageBox.Show(string.Join("\n", ExtractedHearing.Warnings),
                            "Detalles del Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al procesar con IA: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                ProgressCard.Visibility = Visibility.Collapsed;
                ProcessButton.IsEnabled = true;
            }
        }

        private void PopulatePreview(HearingExtractionResult result)
        {
            // Llenar campos de vista previa
            PreviewCaseCode.Text = result.CaseCode;

            // Seleccionar tipo de audiencia en combo
            foreach (var item in PreviewHearingType.Items.OfType<ComboBoxItem>())
            {
                if (item.Content.ToString() == result.HearingType)
                {
                    PreviewHearingType.SelectedItem = item;
                    break;
                }
            }

            PreviewDate.SelectedDate = result.Date;
            PreviewTime.Text = result.Time;
            PreviewCourt.Text = result.Court;

            if (result.WasHeld.HasValue)
            {
                PreviewWasHeldYes.IsChecked = result.WasHeld.Value;
                PreviewWasHeldNo.IsChecked = !result.WasHeld.Value;
            }

            // Seleccionar motivo si aplica
            if (!string.IsNullOrEmpty(result.ReasonNotHeld))
            {
                foreach (var item in PreviewReason.Items.OfType<ComboBoxItem>())
                {
                    if (item.Content.ToString() == result.ReasonNotHeld)
                    {
                        PreviewReason.SelectedItem = item;
                        break;
                    }
                }
            }

            PreviewObservations.Text = result.Observations;

            // Mostrar confianza
            ConfidenceBar.Value = result.Confidence * 100;
            ConfidenceText.Text = $"{result.Confidence:P0}";

            UpdateReasonVisibility();
        }

        private void UpdateReasonVisibility()
        {
            var shouldShow = PreviewWasHeldNo.IsChecked == true;
            ReasonLabel.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
            PreviewReason.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Actualizar el objeto con los valores editados en la vista previa
            if (ExtractedHearing != null)
            {
                ExtractedHearing.CaseCode = PreviewCaseCode.Text;
                ExtractedHearing.HearingType = (PreviewHearingType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                ExtractedHearing.Date = PreviewDate.SelectedDate;
                ExtractedHearing.Time = PreviewTime.Text;
                ExtractedHearing.Court = PreviewCourt.Text;
                ExtractedHearing.WasHeld = PreviewWasHeldYes.IsChecked == true;
                ExtractedHearing.ReasonNotHeld = (PreviewReason.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                ExtractedHearing.Observations = PreviewObservations.Text;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
