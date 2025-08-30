using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using AudienciasApp.Models;
using AudienciasApp.Services.AI;
using AudienciasApp.Services.DocumentProcessing;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace AudienciasApp.Views.Dialogs
{
    public partial class BatchImportDialog : FluentWindow
    {
        private readonly IAIService _aiService;
        private readonly IDocumentProcessor _documentProcessor;
        private ObservableCollection<ImportHearingViewModel> _hearings;
        public List<Hearing> SelectedHearings { get; private set; }
        private string _selectedFilePath;

        public BatchImportDialog()
        {
            InitializeComponent();
            _aiService = new OpenAIService();
            _documentProcessor = new DocumentProcessor();
            _hearings = new ObservableCollection<ImportHearingViewModel>();
            HearingsDataGrid.ItemsSource = _hearings;
            SelectedHearings = new List<Hearing>();
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Documentos|*.pdf;*.docx;*.txt|PDF|*.pdf|Word|*.docx|Texto|*.txt",
                Title = "Seleccionar documento para importar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(_selectedFilePath);

                // Mostrar información del archivo
                FileNameText.Text = fileInfo.Name;
                FileSizeText.Text = $"{fileInfo.Length / 1024} KB";
                FileInfoCard.Visibility = Visibility.Visible;
                ProcessFileButton.IsEnabled = true;
                StatusText.Text = "Listo para procesar";
            }
        }

        private async void ProcessFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
                return;

            if (!_aiService.IsConfigured)
            {
                System.Windows.MessageBox.Show("El servicio de IA no está configurado. Configure la API key en la configuración.",
                    "Configuración Requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            ProcessFileButton.IsEnabled = false;
            StatusText.Text = "Procesando...";
            _hearings.Clear();

            try
            {
                // Extraer texto del documento
                var extractedText = await _documentProcessor.ExtractTextAsync(_selectedFilePath);

                if (string.IsNullOrEmpty(extractedText))
                {
                    System.Windows.MessageBox.Show("No se pudo extraer texto del documento.",
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                StatusText.Text = "Analizando con IA...";

                // Procesar con IA
                var results = await _aiService.ExtractMultipleHearingsAsync(extractedText);

                if (results?.Any() == true)
                {
                    foreach (var result in results)
                    {
                        _hearings.Add(new ImportHearingViewModel
                        {
                            IsSelected = result.Confidence > 0.7, // Auto-seleccionar si confianza > 70%
                            CaseCode = result.CaseCode,
                            HearingType = result.HearingType,
                            Date = result.Date ?? DateTime.Now,
                            Time = result.Time,
                            Court = result.Court,
                            WasHeld = result.WasHeld ?? false,
                            ReasonNotHeld = result.ReasonNotHeld,
                            Observations = result.Observations,
                            Confidence = result.Confidence,
                            SourceText = result.SourceText
                        });
                    }

                    DetectedCountText.Text = _hearings.Count.ToString();
                    UpdateSelectionCount();
                    ImportButton.IsEnabled = true;
                    StatusText.Text = $"✅ {_hearings.Count} audiencias detectadas";
                }
                else
                {
                    StatusText.Text = "No se detectaron audiencias";
                    System.Windows.MessageBox.Show("No se pudieron detectar audiencias en el documento.",
                        "Sin Resultados", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al procesar el documento: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusText.Text = "Error al procesar";
            }
            finally
            {
                ProcessFileButton.IsEnabled = true;
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var hearing in _hearings)
            {
                hearing.IsSelected = true;
            }
            UpdateSelectionCount();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var hearing in _hearings)
            {
                hearing.IsSelected = false;
            }
            UpdateSelectionCount();
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Wpf.Ui.Controls.Button;
            var hearing = button?.Tag as ImportHearingViewModel;

            if (hearing != null)
            {
                // Aquí podrías abrir un diálogo de edición
                // Por ahora, simplemente permitir edición en la grilla
                HearingsDataGrid.SelectedItem = hearing;
                HearingsDataGrid.BeginEdit();
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Wpf.Ui.Controls.Button;
            var hearing = button?.Tag as ImportHearingViewModel;

            if (hearing != null)
            {
                _hearings.Remove(hearing);
                UpdateSelectionCount();
            }
        }

        private void UpdateSelectionCount()
        {
            var selectedCount = _hearings.Count(h => h.IsSelected);
            SelectedCountRun.Text = selectedCount.ToString();
            TotalCountRun.Text = _hearings.Count.ToString();
            ImportButton.IsEnabled = selectedCount > 0;
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _hearings.Where(h => h.IsSelected).ToList();

            if (!selectedItems.Any())
            {
                System.Windows.MessageBox.Show("No hay audiencias seleccionadas para importar.",
                    "Selección Requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // Preparar lista de audiencias para importar
            SelectedHearings = selectedItems.Select(h => new Hearing
            {
                CaseCode = h.CaseCode,
                HearingType = h.HearingType,
                Date = h.Date,
                Time = h.Time,
                Court = h.Court,
                WasHeld = h.WasHeld,
                ReasonNotHeld = h.ReasonNotHeld,
                Observations = h.Observations
            }).ToList();

            // Mostrar progreso de importación
            ImportProgressCard.Visibility = Visibility.Visible;
            ImportButton.IsEnabled = false;
            ImportProgressBar.Maximum = SelectedHearings.Count;
            ImportProgressBar.Value = 0;

            await Task.Delay(500); // Simular procesamiento

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Modelo para la vista de importación
    public class ImportHearingViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string CaseCode { get; set; }
        public string HearingType { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Court { get; set; }
        public bool WasHeld { get; set; }
        public string ReasonNotHeld { get; set; }
        public string Observations { get; set; }
        public double Confidence { get; set; }
        public string SourceText { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
