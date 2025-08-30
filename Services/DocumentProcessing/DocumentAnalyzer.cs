using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AudienciasApp.Services.DocumentProcessing
{
    public class DocumentAnalyzer
    {
        // Patrones para detectar información de audiencias
        private readonly Dictionary<string, Regex> _patterns = new Dictionary<string, Regex>
        {
            ["CaseCode"] = new Regex(@"\b\d{4}[-\s]?\d{5,6}\b|\bRAD(?:ICADO)?\.?\s*[:.-]?\s*(\d{4}[-\s]?\d{5,6})\b",
                RegexOptions.IgnoreCase),

            ["Date"] = new Regex(@"\b(\d{1,2})\s*(?:de\s+)?([a-zA-Z]+)\s*(?:de\s+)?(\d{4})\b|\b(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})\b"),

            ["Time"] = new Regex(@"\b(\d{1,2}):(\d{2})\s*(?:AM|PM|am|pm|a\.m\.|p\.m\.|horas?)?\b"),

            ["Court"] = new Regex(@"[Jj]uzgado\s+(?:[A-Za-zÁÉÍÓÚáéíóúñÑ\s]+)|[Tt]ribunal\s+(?:[A-Za-zÁÉÍÓÚáéíóúñÑ\s]+)"),

            ["HearingType"] = new Regex(@"[Aa]udiencia\s+(?:de\s+)?([A-Za-zÁÉÍÓÚáéíóúñÑ\s]+)"),

            ["NotHeld"] = new Regex(@"no\s+se\s+(?:realizó|llevó\s+a\s+cabo|efectuó)|(?:fue\s+)?(?:cancelada|aplazada|suspendida)",
                RegexOptions.IgnoreCase),

            ["Reason"] = new Regex(@"(?:por|debido\s+a|motivo:|razón:)\s*([^.]+)", RegexOptions.IgnoreCase)
        };

        public DocumentMetadata AnalyzeDocument(string text)
        {
            var metadata = new DocumentMetadata
            {
                TotalCharacters = text.Length,
                EstimatedHearings = EstimateHearingCount(text),
                DocumentType = DetectDocumentType(text),
                DetectedDates = ExtractDates(text),
                DetectedCaseCodes = ExtractCaseCodes(text),
                Language = DetectLanguage(text),
                HasTables = DetectTables(text),
                Confidence = CalculateConfidence(text)
            };

            return metadata;
        }

        private int EstimateHearingCount(string text)
        {
            var hearingMentions = _patterns["HearingType"].Matches(text).Count;
            var caseCodes = _patterns["CaseCode"].Matches(text).Count;
            var dates = _patterns["Date"].Matches(text).Count;

            // Estimar basándose en el mínimo de menciones relevantes
            return Math.Max(1, Math.Min(hearingMentions, Math.Min(caseCodes, dates)));
        }

        private string DetectDocumentType(string text)
        {
            var lowerText = text.ToLower();

            if (lowerText.Contains("acta") && lowerText.Contains("audiencia"))
                return "Acta de Audiencia";

            if (lowerText.Contains("auto") && lowerText.Contains("señala"))
                return "Auto de Programación";

            if (lowerText.Contains("constancia") || lowerText.Contains("certificación"))
                return "Constancia/Certificación";

            if (lowerText.Contains("citación") || lowerText.Contains("notificación"))
                return "Citación/Notificación";

            if (lowerText.Contains("calendario") || lowerText.Contains("cronograma"))
                return "Calendario de Audiencias";

            return "Documento General";
        }

        private List<DateTime> ExtractDates(string text)
        {
            var dates = new List<DateTime>();
            var matches = _patterns["Date"].Matches(text);

            foreach (Match match in matches)
            {
                if (TryParseSpanishDate(match.Value, out var date))
                {
                    dates.Add(date);
                }
            }

            return dates.Distinct().OrderBy(d => d).ToList();
        }

        private List<string> ExtractCaseCodes(string text)
        {
            var codes = new HashSet<string>();
            var matches = _patterns["CaseCode"].Matches(text);

            foreach (Match match in matches)
            {
                codes.Add(match.Value.Trim());
            }

            return codes.ToList();
        }

        private bool TryParseSpanishDate(string dateStr, out DateTime date)
        {
            date = default;

            // Diccionario de meses en español
            var months = new Dictionary<string, int>
            {
                ["enero"] = 1,
                ["febrero"] = 2,
                ["marzo"] = 3,
                ["abril"] = 4,
                ["mayo"] = 5,
                ["junio"] = 6,
                ["julio"] = 7,
                ["agosto"] = 8,
                ["septiembre"] = 9,
                ["octubre"] = 10,
                ["noviembre"] = 11,
                ["diciembre"] = 12
            };

            // Intentar formato: "15 de marzo de 2024"
            var spanishPattern = @"(\d{1,2})\s*(?:de\s+)?([a-zA-Z]+)\s*(?:de\s+)?(\d{4})";
            var match = Regex.Match(dateStr, spanishPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var day = int.Parse(match.Groups[1].Value);
                var monthName = match.Groups[2].Value.ToLower();
                var year = int.Parse(match.Groups[3].Value);

                if (months.ContainsKey(monthName))
                {
                    try
                    {
                        date = new DateTime(year, months[monthName], day);
                        return true;
                    }
                    catch { }
                }
            }

            // Intentar formatos numéricos
            return DateTime.TryParse(dateStr, out date);
        }

        private string DetectLanguage(string text)
        {
            // Detectar idioma basándose en palabras clave
            var spanishKeywords = new[] { "audiencia", "juzgado", "proceso", "radicado", "fiscal", "juez" };
            var spanishCount = spanishKeywords.Count(keyword =>
                text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            return spanishCount >= 3 ? "Español" : "Desconocido";
        }

        private bool DetectTables(string text)
        {
            // Detectar si hay estructuras tabulares (pipes, múltiples tabs, etc.)
            return text.Contains(" | ") ||
                   Regex.IsMatch(text, @"\t{2,}") ||
                   Regex.IsMatch(text, @"^\s*\|.*\|.*\|", RegexOptions.Multiline);
        }

        private double CalculateConfidence(string text)
        {
            var score = 0.0;
            var maxScore = 5.0;

            // Puntos por elementos encontrados
            if (_patterns["CaseCode"].IsMatch(text)) score += 1;
            if (_patterns["Date"].IsMatch(text)) score += 1;
            if (_patterns["Court"].IsMatch(text)) score += 1;
            if (_patterns["HearingType"].IsMatch(text)) score += 1;
            if (text.Length > 100) score += 1;

            return Math.Min(1.0, score / maxScore);
        }
    }

    public class DocumentMetadata
    {
        public int TotalCharacters { get; set; }
        public int EstimatedHearings { get; set; }
        public string DocumentType { get; set; }
        public List<DateTime> DetectedDates { get; set; } = new List<DateTime>();
        public List<string> DetectedCaseCodes { get; set; } = new List<string>();
        public string Language { get; set; }
        public bool HasTables { get; set; }
        public double Confidence { get; set; }

        public string GetSummary()
        {
            return $"Tipo: {DocumentType}\n" +
                   $"Audiencias estimadas: {EstimatedHearings}\n" +
                   $"Casos detectados: {DetectedCaseCodes.Count}\n" +
                   $"Fechas encontradas: {DetectedDates.Count}\n" +
                   $"Confianza: {Confidence:P0}";
        }
    }
}
