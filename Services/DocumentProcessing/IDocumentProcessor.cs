using System.Threading.Tasks;

namespace AudienciasApp.Services.DocumentProcessing
{
    public interface IDocumentProcessor
    {
        Task<string> ExtractTextAsync(string filePath);
        bool IsSupported(string filePath);
    }
}
