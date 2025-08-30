using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AudienciasApp.Services.AI
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

        public OpenAIService(IConfiguration? configuration = null)
        {
            _httpClient = new HttpClient();
            // AIPromptTemplates is static helper

            if (configuration != null)
            {
                _apiKey = configuration["AISettings:ApiKey"];
                _model = configuration["AISettings:Model"] ?? "gpt-4";
            }
            else
            {
                _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                _model = "gpt-4";
            }

            if (!string.IsNullOrEmpty(_apiKey))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<HearingExtractionResult> ExtractHearingFromTextAsync(string text)
        {
            if (!IsConfigured)
            {
                return new HearingExtractionResult
                {
                    Success = false,
                    Warnings = new List<string> { "Servicio de IA no configurado. Configure la API key." }
                };
            }

            try
            {
                var prompt = AIPromptTemplates.GetSingleExtractionPrompt(text);
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = AIPromptTemplates.SystemPrompt },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    max_tokens = 1000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new HearingExtractionResult
                    {
                        Success = false,
                        Warnings = new List<string> { $"Error de API: {response.StatusCode}" }
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson);

                if (apiResponse?.Choices?.Any() == true)
                {
                    var extractedData = apiResponse.Choices[0].Message.Content;
                    return ParseAIResponse(extractedData, text);
                }

                return new HearingExtractionResult { Success = false, Warnings = new List<string> { "No se pudo extraer información" } };
            }
            catch (Exception ex)
            {
                return new HearingExtractionResult { Success = false, Warnings = new List<string> { $"Error: {ex.Message}" } };
            }
        }

        public async Task<List<HearingExtractionResult>> ExtractMultipleHearingsAsync(string text)
        {
            if (!IsConfigured) return new List<HearingExtractionResult>();

            try
            {
                var prompt = AIPromptTemplates.GetBatchExtractionPrompt(text);
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = AIPromptTemplates.SystemPromptBatch },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    max_tokens = 4000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode) return new List<HearingExtractionResult>();

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson);

                if (apiResponse?.Choices?.Any() == true)
                {
                    var extractedData = apiResponse.Choices[0].Message.Content;
                    return ParseMultipleAIResponses(extractedData);
                }

                return new List<HearingExtractionResult>();
            }
            catch
            {
                return new List<HearingExtractionResult>();
            }
        }

        private HearingExtractionResult ParseAIResponse(string jsonResponse, string originalText)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<AIExtractedData>(jsonResponse, options);

                var result = new HearingExtractionResult
                {
                    Success = true,
                    CaseCode = data.CaseCode ?? string.Empty,
                    HearingType = MapHearingType(data.HearingType),
                    Date = ParseDate(data.Date),
                    Time = data.Time ?? "08:30",
                    Court = data.Court ?? string.Empty,
                    WasHeld = data.WasHeld,
                    ReasonNotHeld = MapReasonNotHeld(data.ReasonNotHeld),
                    Observations = data.Observations ?? string.Empty,
                    Confidence = data.Confidence ?? 0.8,
                    SourceText = originalText
                };

                if (string.IsNullOrEmpty(result.CaseCode)) result.Warnings.Add("No se detectó código de caso");
                if (!result.Date.HasValue) result.Warnings.Add("Fecha no detectada, usando fecha actual");

                return result;
            }
            catch (Exception ex)
            {
                return new HearingExtractionResult { Success = false, Warnings = new List<string> { $"Error al parsear respuesta: {ex.Message}" }, SourceText = originalText };
            }
        }

        private List<HearingExtractionResult> ParseMultipleAIResponses(string jsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<AIBatchExtractedData>(jsonResponse, options);
                if (data?.Hearings == null) return new List<HearingExtractionResult>();

                return data.Hearings.Select(h => new HearingExtractionResult
                {
                    Success = true,
                    CaseCode = h.CaseCode ?? string.Empty,
                    HearingType = MapHearingType(h.HearingType),
                    Date = ParseDate(h.Date),
                    Time = h.Time ?? "08:30",
                    Court = h.Court ?? string.Empty,
                    WasHeld = h.WasHeld,
                    ReasonNotHeld = MapReasonNotHeld(h.ReasonNotHeld),
                    Observations = h.Observations ?? string.Empty,
                    Confidence = h.Confidence ?? 0.8,
                    SourceText = h.SourceText ?? string.Empty
                }).ToList();
            }
            catch
            {
                return new List<HearingExtractionResult>();
            }
        }

        private string MapHearingType(string aiType)
        {
            if (string.IsNullOrEmpty(aiType)) return "Otra";
            var validTypes = new[] { /* same list as before */
                "Alegatos de conclusión","Audiencia concentrada","Audiencia de acusación","Audiencia de conciliación","Audiencia de control de legalidad","Audiencia de individualización de pena","Audiencia de imputación","Audiencia de incidente de reparación integral","Audiencia de juicio oral","Audiencia de medidas de aseguramiento","Audiencia de nulidad","Audiencia de preclusión","Audiencia de prórroga","Audiencia de revisión de medida","Audiencia de verificación de cumplimiento","Audiencia preliminar","Audiencia preparatoria","Otra" };

            var exactMatch = validTypes.FirstOrDefault(t => t.Equals(aiType, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;
            var partialMatch = validTypes.FirstOrDefault(t => t.Contains(aiType, StringComparison.OrdinalIgnoreCase) || aiType.Contains(t, StringComparison.OrdinalIgnoreCase));
            return partialMatch ?? "Otra";
        }

        private string MapReasonNotHeld(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return string.Empty;
            var validReasons = new Dictionary<string, string[]>
            {
                ["Juez"] = new[] { "juez", "magistrado", "despacho" },
                ["Fiscalía"] = new[] { "fiscal", "fiscalía", "ministerio público" },
                ["Usuario"] = new[] { "usuario", "procesado", "imputado", "acusado" },
                ["Inpec"] = new[] { "inpec", "centro penitenciario", "cárcel" },
                ["Víctima"] = new[] { "víctima", "denunciante", "afectado" },
                ["ICBF"] = new[] { "icbf", "bienestar familiar", "menor" },
                ["Defensor Confianza"] = new[] { "defensor confianza", "abogado confianza" },
                ["Defensor Público"] = new[] { "defensor público", "defensoría" }
            };

            var lowerReason = reason.ToLower();
            foreach (var kvp in validReasons)
                if (kvp.Value.Any(keyword => lowerReason.Contains(keyword))) return kvp.Key;

            return string.Empty;
        }

        private DateTime? ParseDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;
            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd 'de' MMMM 'de' yyyy", "d 'de' MMMM 'de' yyyy" };
            foreach (var format in formats)
                if (DateTime.TryParseExact(dateStr, format, System.Globalization.CultureInfo.GetCultureInfo("es-CO"), System.Globalization.DateTimeStyles.None, out var date))
                    return date;
            if (DateTime.TryParse(dateStr, out var parsedDate)) return parsedDate;
            return null;
        }

        // auxiliary classes for deserialization
        private class OpenAIResponse { [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } }
        private class Choice { [JsonPropertyName("message")] public Message Message { get; set; } }
        private class Message { [JsonPropertyName("content")] public string Content { get; set; } }
        private class AIExtractedData { public string CaseCode { get; set; } public string HearingType { get; set; } public string Date { get; set; } public string Time { get; set; } public string Court { get; set; } public bool? WasHeld { get; set; } public string ReasonNotHeld { get; set; } public string Observations { get; set; } public double? Confidence { get; set; } }
        private class AIBatchExtractedData { public List<AIExtractedDataWithSource> Hearings { get; set; } }
        private class AIExtractedDataWithSource : AIExtractedData { public string SourceText { get; set; } }
    }
}
