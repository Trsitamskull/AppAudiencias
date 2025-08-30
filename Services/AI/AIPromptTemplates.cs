namespace AudienciasApp.Services.AI
{
    public static class AIPromptTemplates
    {
        public static string SystemPrompt => "Eres un asistente que extrae datos de audiencias judiciales en Colombia y devuelves JSON siguiendo el esquema proporcionado.";

        public static string GetSingleExtractionPrompt(string text)
        {
            return $"EXTRAE_JSON_UNICO:\nTexto:\n{text}\n\nDevuelve JSON con campos: caseCode, hearingType, date (DD/MM/YYYY), time (HH:MM), court, wasHeld (true/false), reasonNotHeld, observations.";
        }

        public static string GetBatchExtractionPrompt(string documentText)
        {
            return $"EXTRAE_JSON_ARRAY:\nDocumento:\n{documentText}\n\nDevuelve un array JSON con objetos siguiendo el mismo esquema que para una sola audiencia e incluye campo sourceText.";
        }

        public static string SystemPromptBatch => "Eres un asistente que extrae múltiples audiencias de un documento y devuelve un array JSON con el esquema requerido.";
    }
}
