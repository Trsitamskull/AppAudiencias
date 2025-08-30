using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudienciasApp.Services.AI
{
    public interface IAIService
    {
        Task<HearingExtractionResult> ExtractHearingFromTextAsync(string text);
        Task<List<HearingExtractionResult>> ExtractMultipleHearingsAsync(string text);
        bool IsConfigured { get; }
    }
}
