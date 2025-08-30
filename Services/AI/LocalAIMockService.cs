using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AudienciasApp.Services.AI
{
    // Minimal local parser to allow UI testing without external API calls.
    public class LocalAIMockService : IAIService
    {
        public bool IsConfigured => true;

        public Task<HearingExtractionResult> ExtractHearingFromTextAsync(string text)
        {
            // naive parsing: look for a 4+ digit year like 2024-123 or date
            var result = new HearingExtractionResult { Success = true, SourceText = text };

            // Case code: sequence like 2024-123 or words with digits
            var m = Regex.Match(text, @"\d{4}-\d{3,}");
            if (m.Success) result.CaseCode = m.Value;

            // Date: dd de <mes> or dd/mm/yyyy
            var dm = Regex.Match(text, @"(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})");
            if (dm.Success && DateTime.TryParse(dm.Value, out var dt)) result.Date = dt;

            // Time HH:MM
            var tm = Regex.Match(text, @"(\d{1,2}:\d{2})");
            if (tm.Success) result.Time = tm.Value;

            // Was held detection
            if (text.ToLower().Contains("no se realiz") || text.ToLower().Contains("no se realizó"))
                result.WasHeld = false;
            else if (text.ToLower().Contains("se realizó") || text.ToLower().Contains("realizada"))
                result.WasHeld = true;

            // Simple hearing type pick first listed type word
            if (text.ToLower().Contains("imputaci")) result.HearingType = "Audiencia de imputación";

            // Court: look for 'Juzgado' word and following
            var cm = Regex.Match(text, @"Juzgad[oa]\s+[^.,;\n]+", RegexOptions.IgnoreCase);
            if (cm.Success) result.Court = cm.Value.Trim();

            // Observations = last sentence
            var parts = text.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) result.Observations = parts[parts.Length - 1].Trim();

            return Task.FromResult(result);
        }

        public Task<List<HearingExtractionResult>> ExtractMultipleHearingsAsync(string text)
        {
            // naive split by double newline
            var blocks = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<HearingExtractionResult>();
            foreach (var b in blocks)
            {
                var r = ExtractHearingFromTextAsync(b).Result;
                list.Add(r);
            }
            return Task.FromResult(list);
        }
    }
}
