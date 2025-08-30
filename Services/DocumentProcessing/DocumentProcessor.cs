using System;
using System.IO;
using System.Threading.Tasks;

namespace AudienciasApp.Services.DocumentProcessing
{
    public class DocumentProcessor : IDocumentProcessor
    {
        public async Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("El archivo no existe", filePath);

            var extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".txt" => await ExtractFromTextFile(filePath),
                ".pdf" => await ExtractFromPdf(filePath),
                ".docx" => await ExtractFromDocx(filePath),
                _ => throw new NotSupportedException($"Formato no soportado: {extension}")
            };
        }

        public bool IsSupported(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".txt" || extension == ".pdf" || extension == ".docx";
        }

        private async Task<string> ExtractFromTextFile(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        private async Task<string> ExtractFromPdf(string filePath)
        {
            // Para PDF necesitarías una librería como iTextSharp o PdfSharp
            // Por ahora retornamos un placeholder
            await Task.Delay(100);

            // Implementación básica - deberías usar una librería PDF real
            return "PDF parsing requiere librería adicional como iTextSharp. " +
                   "Instale: Install-Package itext7";
        }

        private async Task<string> ExtractFromDocx(string filePath)
        {
            // Para DOCX podrías usar DocumentFormat.OpenXml
            // Por ahora retornamos un placeholder
            await Task.Delay(100);

            // Implementación básica - deberías usar DocumentFormat.OpenXml
            return "DOCX parsing requiere DocumentFormat.OpenXml. " +
                   "Instale: Install-Package DocumentFormat.OpenXml";
        }
    }
}
