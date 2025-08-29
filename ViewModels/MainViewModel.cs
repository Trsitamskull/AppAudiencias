using System;
using System.Windows;
using System.Windows.Input;
using AudienciasApp.Commands;

namespace AudienciasApp.ViewModels
{
    public class MainViewModel
    {
        //Propiedad publica que la vista usara
        public RelayCommand SaveCommand { get; }

        // Constructor: se ejecuta al crear el ViewModel
        public MainViewModel()
        {
            // Aquí "registramos" qué debe hacer el comando Save
            SaveCommand = new RelayCommand(
                execute: _ => Save(),
                canExecute: _ => true // de momento siempre habilitado
            );
        }

        private void Save()
        {
            // Por ahora solo se muestra un mensaje de prueba
            MessageBox.Show("El comando Guardar fue ejecutado desde el ViewModel ",
                            "Prueba de MVVM",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}