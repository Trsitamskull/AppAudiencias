using System;
using System.Globalization;
using System.Windows.Data;

namespace AudienciasApp.Utils
{
    public class BoolToSaveButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditMode && isEditMode)
                return "Actualizar Registro";
            return "Guardar Registro";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
