using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudienciasApp.Models;
using OfficeOpenXml;
using Microsoft.Win32;

namespace AudienciasApp.Services
{
    public class ExcelService
    {
        private readonly string _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "template", "plantilla_audiencias.xlsx");
        private readonly string _createdFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "ArchivosCreados");
        private string _currentFilePath = string.Empty;
        private int _currentRow = 11; // Primera fila de datos según la estructura

        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Crear directorio si no existe
            if (!Directory.Exists(_createdFilesPath))
            {
                Directory.CreateDirectory(_createdFilesPath);
            }
        }

        public async Task<string> CreateNewFileAsync()
        {
            return await Task.Run(() =>
            {
                var fileName = $"Audiencias_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var newFilePath = Path.Combine(_createdFilesPath, fileName);

                // Copiar plantilla
                File.Copy(_templatePath, newFilePath, true);

                _currentFilePath = newFilePath;
                _currentRow = 11; // Reset al inicio

                // Log en memoria.md
                LogAction($"CREAR - Nuevo archivo: {fileName}");

                return fileName;
            });
        }

        // Create file by explicit destination path (used when user chooses a filename)
        public async Task<string> CreateNewFileAsync(string destinationPath)
        {
            return await Task.Run(() =>
            {
                var dir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                // Copiar plantilla
                File.Copy(_templatePath, destinationPath, true);

                _currentFilePath = destinationPath;
                _currentRow = 11;

                var fileName = Path.GetFileName(destinationPath);
                LogAction($"CREAR - Nuevo archivo: {fileName}");

                return fileName;
            });
        }

        public async Task<bool> OpenFileAsync()
        {
            return await Task.Run(() =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    InitialDirectory = _createdFilesPath,
                    Title = "Seleccionar archivo de audiencias"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _currentFilePath = openFileDialog.FileName;

                    // Encontrar la última fila con datos
                    using (var package = new ExcelPackage(new FileInfo(_currentFilePath)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        _currentRow = 11;

                        while (_currentRow <= 110 &&
                               !string.IsNullOrEmpty(worksheet.Cells[$"B{_currentRow}"].Text))
                        {
                            _currentRow++;
                        }
                    }

                    LogAction($"ABRIR - Archivo: {Path.GetFileName(_currentFilePath)}");
                    return true;
                }

                return false;
            });
        }

        public async Task SaveHearingAsync(Hearing hearing)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    throw new InvalidOperationException("No hay archivo abierto. Por favor, cree o abra un archivo primero.");
                }

                if (_currentRow > 110)
                {
                    throw new InvalidOperationException("El archivo ha alcanzado el límite máximo de 100 registros.");
                }

                using (var package = new ExcelPackage(new FileInfo(_currentFilePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    // A - Número de registro (autoincremental)
                    worksheet.Cells[$"A{_currentRow}"].Value = _currentRow - 10;

                    // B - Radicado del proceso
                    worksheet.Cells[$"B{_currentRow}"].Value = hearing.CaseCode;

                    // C - Tipo de audiencia
                    worksheet.Cells[$"C{_currentRow}"].Value = hearing.HearingType;

                    // D - Fecha de la audiencia
                    worksheet.Cells[$"D{_currentRow}"].Value = hearing.Date.ToString("dd/MM/yyyy");

                    // E - Hora
                    worksheet.Cells[$"E{_currentRow}"].Value = hearing.Time;

                    // F - Juzgado
                    worksheet.Cells[$"F{_currentRow}"].Value = hearing.Court;

                    // G - ¿Se realizó? SI / H - ¿Se realizó? NO
                    // Always clear the peer cells first to avoid leftover values
                    worksheet.Cells[$"G{_currentRow}"].Value = null;
                    worksheet.Cells[$"H{_currentRow}"].Value = null;

                    // Clear all reason columns I-P for this row before setting the correct one
                    string[] reasonCols = { "I", "J", "K", "L", "M", "N", "O", "P" };
                    foreach (var rc in reasonCols)
                    {
                        worksheet.Cells[$"{rc}{_currentRow}"] .Value = null;
                    }

                    if (hearing.WasHeld)
                    {
                        worksheet.Cells[$"G{_currentRow}"].Value = "SI";
                    }
                    else
                    {
                        worksheet.Cells[$"H{_currentRow}"].Value = "NO";

                        // Set the corresponding reason (I-P)
                        switch (hearing.ReasonNotHeld)
                        {
                            case "Juez":
                                worksheet.Cells[$"I{_currentRow}"].Value = "X";
                                break;
                            case "Fiscalía":
                                worksheet.Cells[$"J{_currentRow}"].Value = "X";
                                break;
                            case "Usuario":
                                worksheet.Cells[$"K{_currentRow}"].Value = "X";
                                break;
                            case "Inpec":
                                worksheet.Cells[$"L{_currentRow}"].Value = "X";
                                break;
                            case "Víctima":
                                worksheet.Cells[$"M{_currentRow}"].Value = "X";
                                break;
                            case "ICBF":
                                worksheet.Cells[$"N{_currentRow}"].Value = "X";
                                break;
                            case "Defensor Confianza":
                                worksheet.Cells[$"O{_currentRow}"].Value = "X";
                                break;
                            case "Defensor Público":
                                worksheet.Cells[$"P{_currentRow}"].Value = "X";
                                break;
                        }
                    }

                    // Q - Observaciones
                    worksheet.Cells[$"Q{_currentRow}"].Value = hearing.Observations;

                    // Actualizar totales en fila 111
                    UpdateTotals(worksheet);

                    package.Save();
                    _currentRow++;

                    LogAction($"GUARDAR - Registro #{_currentRow - 11} - Radicado: {hearing.CaseCode}");
                }
            });
        }

        private void UpdateTotals(ExcelWorksheet worksheet)
        {
            // Contar SI en columna G
            int totalSi = 0;
            for (int row = 11; row <= 110; row++)
            {
                if (worksheet.Cells[$"G{row}"].Text == "SI")
                    totalSi++;
            }
            worksheet.Cells["G111"].Value = totalSi;

            // Contar NO en columna H (para observaciones)
            int totalNo = 0;
            for (int row = 11; row <= 110; row++)
            {
                if (worksheet.Cells[$"H{row}"].Text == "NO")
                    totalNo++;
            }

            // Escribir total de NO en Q111 por compatibilidad con plantillas que muestren el resumen en Observaciones
            worksheet.Cells["Q111"].Value = totalNo;

            // Contar cada motivo (I-P)
            string[] columns = { "I", "J", "K", "L", "M", "N", "O", "P" };
            foreach (string col in columns)
            {
                int count = 0;
                for (int row = 11; row <= 110; row++)
                {
                    if (worksheet.Cells[$"{col}{row}"].Text == "X")
                        count++;
                }
                worksheet.Cells[$"{col}111"].Value = count;
            }
        }

        public async Task<bool> DownloadFileAsync()
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    throw new InvalidOperationException("No hay archivo abierto para descargar.");
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
                    LogAction($"DESCARGAR - Archivo guardado en: {saveFileDialog.FileName}");
                    return true;
                }

                return false;
            });
        }

        public async Task<bool> DeleteFileAsync()
        {
            return await Task.Run(() =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    InitialDirectory = _createdFilesPath,
                    Title = "Seleccionar archivo a eliminar"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // No permitir eliminar la plantilla
                    if (openFileDialog.FileName.Contains("template"))
                    {
                        throw new InvalidOperationException("No se puede eliminar la plantilla del sistema.");
                    }

                    File.Delete(openFileDialog.FileName);

                    // Si es el archivo actual, limpiarlo
                    if (_currentFilePath == openFileDialog.FileName)
                    {
                        _currentFilePath = string.Empty;
                        _currentRow = 11;
                    }

                    LogAction($"ELIMINAR - Archivo: {Path.GetFileName(openFileDialog.FileName)}");
                    return true;
                }

                return false;
            });
        }

        public async Task OpenSpecificFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(filePath))
                {
                    _currentFilePath = filePath;

                    using (var package = new ExcelPackage(new FileInfo(_currentFilePath)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        _currentRow = 11;

                        while (_currentRow <= 110 &&
                               !string.IsNullOrEmpty(worksheet.Cells[$"B{_currentRow}"].Text))
                        {
                            _currentRow++;
                        }
                    }

                    LogAction($"ABRIR_RECIENTE - Archivo: {Path.GetFileName(filePath)}");
                }
            });
        }

        public List<RecentFile> GetRecentFiles()
        {
            var files = new List<RecentFile>();

            if (Directory.Exists(_createdFilesPath))
            {
                var directoryInfo = new DirectoryInfo(_createdFilesPath);
                var excelFiles = directoryInfo.GetFiles("*.xlsx")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(5);

                foreach (var file in excelFiles)
                {
                    files.Add(new RecentFile
                    {
                        FileName = file.Name,
                        FilePath = file.FullName,
                        LastModified = file.LastWriteTime,
                        FileSize = file.Length
                    });
                }
            }

            return files;
        }

        public Statistics GetStatistics()
        {
            var stats = new Statistics();

            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                using (var package = new ExcelPackage(new FileInfo(_currentFilePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    for (int row = 11; row <= 110; row++)
                    {
                        if (!string.IsNullOrEmpty(worksheet.Cells[$"B{row}"].Text))
                        {
                            if (worksheet.Cells[$"G{row}"].Text == "SI")
                                stats.Realizadas++;
                            else if (worksheet.Cells[$"H{row}"].Text == "NO")
                                stats.NoRealizadas++;
                        }
                    }
                }
            }

            return stats;
        }

        private void LogAction(string action)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memoria.md");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action}{Environment.NewLine}";

            File.AppendAllText(logPath, logEntry);
        }
    }
}
