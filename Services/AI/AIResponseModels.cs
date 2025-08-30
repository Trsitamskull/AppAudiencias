using System;
using System.Collections.Generic;

namespace AudienciasApp.Services.AI
{
    public class HearingExtractionResult
    {
        public bool Success { get; set; } = false;
        public string CaseCode { get; set; } = string.Empty;
        public string HearingType { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public bool? WasHeld { get; set; }
        public string ReasonNotHeld { get; set; } = string.Empty;
        public string Observations { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.0;
        public List<string> Warnings { get; set; } = new List<string>();
        public string SourceText { get; set; } = string.Empty;

        public AudienciasApp.Models.Hearing ToHearing()
        {
            return new AudienciasApp.Models.Hearing
            {
                CaseCode = CaseCode ?? string.Empty,
                HearingType = HearingType ?? string.Empty,
                Date = Date ?? DateTime.Now,
                Time = string.IsNullOrWhiteSpace(Time) ? "08:30" : Time,
                Court = Court ?? string.Empty,
                WasHeld = WasHeld ?? false,
                ReasonNotHeld = ReasonNotHeld ?? string.Empty,
                Observations = Observations ?? string.Empty
            };
        }
    }
}
