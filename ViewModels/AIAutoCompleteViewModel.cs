using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudienciasApp.Services.AI;

namespace AudienciasApp.ViewModels
{
    public partial class AIAutoCompleteViewModel : ObservableObject
    {
        private readonly IAIService _aiService;

        public AIAutoCompleteViewModel()
        {
            // use local mock by default so dialog works without API keys
            _aiService = new LocalAIMockService();
            Extracted = new HearingExtractionResult();
        }

        [ObservableProperty]
        private string _inputText = string.Empty;

        [ObservableProperty]
        private HearingExtractionResult _extracted;

        [ObservableProperty]
        private bool _canApply;

        [RelayCommand]
        private async Task Process()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InputText)) return;
                var r = await _aiService.ExtractHearingFromTextAsync(InputText);
                Extracted = r;
                CanApply = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Apply()
        {
            // Close dialog with DialogResult true if hosted as Window
            if (Application.Current?.Windows != null)
            {
                foreach (Window w in Application.Current.Windows)
                {
                    if (w.DataContext == this)
                    {
                        if (w is System.Windows.Window ww)
                        {
                            ww.DialogResult = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
