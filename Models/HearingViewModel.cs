using System;

namespace AudienciasApp.Models
{
    public class HearingViewModel
    {
        public int RowNumber { get; set; }
        public string CaseCode { get; set; } = string.Empty;
        public string HearingType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public bool WasHeld { get; set; }
        public string ReasonNotHeld { get; set; } = string.Empty;
        public string Observations { get; set; } = string.Empty;
    }
}
