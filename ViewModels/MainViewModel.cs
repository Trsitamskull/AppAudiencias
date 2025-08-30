using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudienciasApp.Models;
using AudienciasApp.Services;
using Microsoft.Win32;

namespace AudienciasApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ExcelService _excelService;
        private readonly ThemeService _themeService;
        private NotificationService _notificationService;

        // Propiedades del formulario
        [ObservableProperty]
        private string _caseCode = string.Empty;

        [ObservableProperty]
        private string _hearingType = string.Empty;

        [ObservableProperty]
        private DateTime? _hearingDate = DateTime.Now;

        [ObservableProperty]
        private string _hearingTime = "08:30";

        [ObservableProperty]
        private string _court = string.Empty;

        [ObservableProperty]
        private bool _isRealizada;

        [ObservableProperty]
        private bool _isNoRealizada;

        [ObservableProperty]
        private string _motivoNoRealizada = string.Empty;

        [ObservableProperty]
        private string _observations = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Listo";

        // Estadísticas
        [ObservableProperty]
        private int _audienciasRealizadas;

        [ObservableProperty]
        private int _audienciasNoRealizadas;

        [ObservableProperty]
        private int _totalAudiencias;

        // Archivos recientes
        [ObservableProperty]
        private ObservableCollection<RecentFile> _recentFiles;

        [ObservableProperty]
        private bool _hasNoRecentFiles = true;

        // Búsqueda y edición
        [ObservableProperty]
        private ObservableCollection<HearingViewModel> _allHearings;

        [ObservableProperty]
        private ObservableCollection<HearingViewModel> _filteredHearings;

        [ObservableProperty]
        private HearingViewModel _selectedHearing;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _hasNoHearings = true;

        // Edit mode
        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private int _editingRowNumber;

        // Estado del archivo actual
        private string _currentFilePath = string.Empty;

        public MainViewModel()
        {
            _excelService = new ExcelService();
            _themeService = new ThemeService();
            _notificationService = new NotificationService();

            RecentFiles = new ObservableCollection<RecentFile>();
            AllHearings = new ObservableCollection<HearingViewModel>();
            FilteredHearings = new ObservableCollection<HearingViewModel>();

            LoadRecentFiles();
            LoadHearings();
        }

        // Comando para crear nuevo archivo
        [RelayCommand]
        private async Task CreateAsync()
        {
            try
            {
                // Preguntar al usuario dónde guardar y con qué nombre
                var defaultName = $"Audiencias_{DateTime.Now:dd-MM-yyyy}.xlsx";
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = defaultName,
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivosCreados"),
                    Title = "Guardar nuevo archivo de audiencias"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var fileName = await _excelService.CreateNewFileAsync(saveDialog.FileName);
                    _currentFilePath = saveDialog.FileName;

                    StatusMessage = $"Archivo creado: {fileName}";
                    LoadRecentFiles();
                    UpdateStatistics();

                    if (_notificationService != null)
                    {
                        await _notificationService.ShowAsync("✅ Archivo creado",
                            $"Se creó el archivo {fileName}",
                            Wpf.Ui.Controls.ControlAppearance.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear archivo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para abrir archivo existente
        [RelayCommand]
        private async Task OpenAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivosCreados"),
                    Title = "Seleccionar archivo de audiencias"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _currentFilePath = openFileDialog.FileName;
                    await _excelService.OpenSpecificFileAsync(_currentFilePath);

                    StatusMessage = $"Archivo abierto: {Path.GetFileName(_currentFilePath)}";
                    UpdateStatistics();

                    if (_notificationService != null)
                    {
                        await _notificationService.ShowAsync("📂 Archivo abierto",
                        Path.GetFileName(_currentFilePath),
                            Wpf.Ui.Controls.ControlAppearance.Info);
                    }
                    // Refresh hearings list when opening a file
                    LoadHearings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir archivo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para guardar registro
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                if (!ValidateForm())
                {
                    MessageBox.Show("Por favor complete todos los campos requeridos",
                        "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var hearing = new Hearing
                {
                    CaseCode = CaseCode,
                    HearingType = HearingType,
                    Date = HearingDate ?? DateTime.Now,
                    Time = HearingTime,
                    Court = Court,
                    WasHeld = IsRealizada,
                    ReasonNotHeld = IsNoRealizada ? MotivoNoRealizada : string.Empty,
                    Observations = Observations
                };

                if (IsEditMode)
                {
                    await _excelService.UpdateHearingAsync(hearing, EditingRowNumber);
                    StatusMessage = $"Registro #{EditingRowNumber} actualizado";

                    if (_notificationService != null)
                    {
                        await _notificationService.ShowAsync("✅ Actualizado",
                            $"Registro #{EditingRowNumber} actualizado correctamente",
                            Wpf.Ui.Controls.ControlAppearance.Success);
                    }
                }
                else
                {
                    await _excelService.SaveHearingAsync(hearing);
                    StatusMessage = "✅ Registro guardado";

                    if (_notificationService != null)
                    {
                        await _notificationService.ShowAsync("✅ Guardado",
                            $"Audiencia registrada: {CaseCode}",
                            Wpf.Ui.Controls.ControlAppearance.Success);
                    }
                }

                UpdateStatistics();
                LoadHearings();
                ClearForm();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No hay archivo abierto"))
            {
                MessageBox.Show("Primero debe crear o abrir un archivo Excel",
                    "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para limpiar formulario
        [RelayCommand]
        private void Clear()
        {
            ClearForm();
            StatusMessage = "Formulario limpiado";
        }

        // Comando para descargar archivo
        [RelayCommand]
        private async Task DownloadAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    MessageBox.Show("No hay archivo abierto para descargar",
                        "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = Path.GetFileName(_currentFilePath),
                    Title = "Guardar archivo de audiencias"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.Copy(_currentFilePath, saveFileDialog.FileName, true);
                    StatusMessage = "Archivo descargado";

                    if (_notificationService != null)
                    {
                        await _notificationService.ShowAsync("💾 Descargado",
                            $"Archivo guardado en: {saveFileDialog.FileName}",
                            Wpf.Ui.Controls.ControlAppearance.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al descargar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para eliminar archivo
        [RelayCommand]
        private async Task DeleteAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivosCreados"),
                    Title = "Seleccionar archivo a eliminar"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "Assets", "template");

                    if (openFileDialog.FileName.Contains(templatePath))
                    {
                        MessageBox.Show("No se puede eliminar la plantilla del sistema",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = MessageBox.Show($"¿Está seguro de eliminar {Path.GetFileName(openFileDialog.FileName)}?",
                        "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(openFileDialog.FileName);

                        if (_currentFilePath == openFileDialog.FileName)
                        {
                            _currentFilePath = string.Empty;
                        }

                        StatusMessage = "Archivo eliminado";
                        LoadRecentFiles();

                        if (_notificationService != null)
                        {
                            await _notificationService.ShowAsync("🗑️ Eliminado",
                                "Archivo eliminado correctamente",
                                Wpf.Ui.Controls.ControlAppearance.Danger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para cambiar tema
        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
            StatusMessage = "Tema cambiado";
        }

        // Comando para abrir archivo reciente
        [RelayCommand]
        private async Task OpenRecentAsync(RecentFile file)
        {
            if (file != null && File.Exists(file.FilePath))
            {
                _currentFilePath = file.FilePath;
                await _excelService.OpenSpecificFileAsync(file.FilePath);
                StatusMessage = $"Archivo abierto: {file.FileName}";
                UpdateStatistics();
            }
        }

        // Métodos auxiliares
        private bool ValidateForm()
        {
            return !string.IsNullOrWhiteSpace(CaseCode) &&
                   !string.IsNullOrWhiteSpace(HearingType) &&
                   HearingDate.HasValue &&
                   !string.IsNullOrWhiteSpace(HearingTime) &&
                   !string.IsNullOrWhiteSpace(Court) &&
                   (IsRealizada || IsNoRealizada) &&
                   (!IsNoRealizada || !string.IsNullOrWhiteSpace(MotivoNoRealizada));
        }

        private void ClearForm()
        {
            CaseCode = string.Empty;
            HearingType = string.Empty;
            HearingDate = DateTime.Now;
            HearingTime = "08:30";
            Court = string.Empty;
            IsRealizada = false;
            IsNoRealizada = false;
            MotivoNoRealizada = string.Empty;
            Observations = string.Empty;
            // Reset edit mode
            IsEditMode = false;
            EditingRowNumber = 0;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            ClearForm();
            StatusMessage = "Edición cancelada";
        }

        private void LoadRecentFiles()
        {
            RecentFiles.Clear();
            var files = _excelService.GetRecentFiles();
            foreach (var file in files)
            {
                RecentFiles.Add(file);
            }
            HasNoRecentFiles = RecentFiles.Count == 0;
        }

        // Load hearings from current file
        private void LoadHearings()
        {
            try
            {
                AllHearings.Clear();
                var hearings = _excelService.GetAllHearings();
                foreach (var h in hearings)
                {
                    AllHearings.Add(h);
                }
                FilterHearings();
            }
            catch
            {
                AllHearings.Clear();
                FilteredHearings.Clear();
            }
        }

        private void FilterHearings()
        {
            FilteredHearings.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllHearings
                : AllHearings.Where(h =>
                    h.CaseCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    h.HearingType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    h.Court.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var h in filtered)
            {
                FilteredHearings.Add(h);
            }

            HasNoHearings = FilteredHearings.Count == 0;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterHearings();
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadHearings();
            StatusMessage = "Lista actualizada";
        }

        [RelayCommand]
        private void Edit(HearingViewModel hearing)
        {
            if (hearing == null) return;

            // Enable edit mode and remember which row to update
            IsEditMode = true;
            EditingRowNumber = hearing.RowNumber;

            CaseCode = hearing.CaseCode;
            HearingType = hearing.HearingType;
            HearingDate = hearing.Date;
            HearingTime = hearing.Time;
            Court = hearing.Court;
            IsRealizada = hearing.WasHeld;
            IsNoRealizada = !hearing.WasHeld;
            MotivoNoRealizada = hearing.ReasonNotHeld;
            Observations = hearing.Observations;

            StatusMessage = $"Editando registro #{hearing.RowNumber}";
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = _excelService.GetStatistics();
                AudienciasRealizadas = stats.Realizadas;
                AudienciasNoRealizadas = stats.NoRealizadas;
                TotalAudiencias = stats.Total;
            }
            catch
            {
                AudienciasRealizadas = 0;
                AudienciasNoRealizadas = 0;
                TotalAudiencias = 0;
            }
        }

        // Método para inicializar el servicio de notificaciones
        public void SetNotificationService(NotificationService service)
        {
            // Este método puede ser llamado desde MainWindow.xaml.cs si es necesario
            // Por ahora el servicio se inicializa en el constructor
            _notificationService = service ?? _notificationService;
        }
    }
}