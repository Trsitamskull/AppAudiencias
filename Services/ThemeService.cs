using System;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AudienciasApp.Services
{
    public class ThemeService
    {
        private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

        public void ToggleTheme()
        {
            _currentTheme = _currentTheme == ApplicationTheme.Dark
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.Mica, true);
        }

        public ApplicationTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        public void SetTheme(ApplicationTheme theme)
        {
            _currentTheme = theme;
            ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.Mica, true);
        }
    }
}