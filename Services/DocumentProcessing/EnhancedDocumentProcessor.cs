using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace AudienciasApp.Services.DocumentProcessing
{
    public class EnhancedDocumentProcessor : IDocumentProcessor
    {
        private readonly int _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly int _chunkSize = 2000;
        private readonly int _overlapSize = 200;

        public async Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("El archivo no existe", filePath);

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > _maxFileSize)
                throw new InvalidOperationException($"El archivo excede el tamaño máximo de {_maxFileSize / (1024 * 1024)}MB");

            var extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".txt" => await ExtractFromTextFile(filePath),
                ".pdf" => await ExtractFromPdf(filePath),
                ".docx" => await ExtractFromDocx(filePath),
                ".doc" => throw new NotSupportedException("Formato .doc no soportado. Por favor convierta a .docx"),
                _ => throw new NotSupportedException($"Formato no soportado: {extension}")
            };
        }

        public bool IsSupported(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return new[] { ".txt", ".pdf", ".docx" }.Contains(extension);
        }

        private async Task<string> ExtractFromTextFile(string filePath)
        {
            var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return CleanAndNormalizeText(text);
        }

        private async Task<string> ExtractFromPdf(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                try
                {
                    using (var pdfReader = new PdfReader(filePath))
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                        {
                            var strategy = new SimpleTextExtractionStrategy();
                            var pageText = PdfTextExtractor.GetTextFromPage(
                                pdfDocument.GetPage(page), strategy);

                            if (!string.IsNullOrWhiteSpace(pageText))
                            {
                                text.AppendLine(pageText);
                                text.AppendLine(); // Separador entre páginas
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al leer PDF: {ex.Message}", ex);
                }

                return CleanAndNormalizeText(text.ToString());
            });
        }

        private async Task<string> ExtractFromDocx(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                try
                {
                    using (var wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        var body = wordDoc.MainDocumentPart?.Document?.Body;

                        if (body != null)
                        {
                            // Extraer texto de párrafos
                            foreach (var paragraph in body.Elements<Paragraph>())
                            {
                                var paragraphText = ExtractTextFromParagraph(paragraph);
                                if (!string.IsNullOrWhiteSpace(paragraphText))
                                {
                                    text.AppendLine(paragraphText);
                                }
                            }

                            // Extraer texto de tablas
                            foreach (var table in body.Elements<Table>())
                            {
                                var tableText = ExtractTextFromTable(table);
                                if (!string.IsNullOrWhiteSpace(tableText))
                                {
                                    text.AppendLine(tableText);
                                }
                            }
                        }

                        // Extraer encabezados y pies de página
                        ExtractHeadersAndFooters(wordDoc, text);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al leer DOCX: {ex.Message}", ex);
                }

                return CleanAndNormalizeText(text.ToString());
            });
        }

        private string ExtractTextFromParagraph(Paragraph paragraph)
        {
            var text = new StringBuilder();

            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var textElement in run.Elements<Text>())
                {
                    text.Append(textElement.Text);
                }
            }

            return text.ToString().Trim();
        }

        private string ExtractTextFromTable(Table table)
        {
            var text = new StringBuilder();

            foreach (var row in table.Elements<TableRow>())
            {
                var rowText = new List<string>();

                foreach (var cell in row.Elements<TableCell>())
                {
                    var cellText = new StringBuilder();
                    foreach (var paragraph in cell.Elements<Paragraph>())
                    {
                        var paragraphText = ExtractTextFromParagraph(paragraph);
                        if (!string.IsNullOrWhiteSpace(paragraphText))
                        {
                            cellText.Append(paragraphText + " ");
                        }
                    }

                    if (cellText.Length > 0)
                    {
                        rowText.Add(cellText.ToString().Trim());
                    }
                }

                if (rowText.Any())
                {
                    text.AppendLine(string.Join(" | ", rowText));
                }
            }

            return text.ToString();
        }

        private void ExtractHeadersAndFooters(WordprocessingDocument doc, StringBuilder text)
        {
            // Extraer encabezados
            var headers = doc.MainDocumentPart?.HeaderParts;
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    var headerText = ExtractTextFromElement(header.Header);
                    if (!string.IsNullOrWhiteSpace(headerText))
                    {
                        text.AppendLine($"[Encabezado: {headerText}]");
                    }
                }
            }

            // Extraer pies de página
            var footers = doc.MainDocumentPart?.FooterParts;
            if (footers != null)
            {
                foreach (var footer in footers)
                {
                    var footerText = ExtractTextFromElement(footer.Footer);
                    if (!string.IsNullOrWhiteSpace(footerText))
                    {
                        text.AppendLine($"[Pie: {footerText}]");
                    }
                }
            }
        }

        private string ExtractTextFromElement(OpenXmlElement element)
        {
            if (element == null) return string.Empty;

            var text = new StringBuilder();
            foreach (var paragraph in element.Elements<Paragraph>())
            {
                var paragraphText = ExtractTextFromParagraph(paragraph);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    text.Append(paragraphText + " ");
                }
            }

            return text.ToString().Trim();
        }

        private string CleanAndNormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Normalizar saltos de línea
            text = Regex.Replace(text, @"\r\n?|\n", "\n");

            // Eliminar múltiples espacios
            text = Regex.Replace(text, @"[ \t]+", " ");

            // Eliminar múltiples saltos de línea (más de 2)
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Limpiar caracteres especiales problemáticos
            text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

            // Trim
            text = text.Trim();

            return text;
        }

        public List<string> SplitIntoChunks(string text)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // Dividir por párrafos primero
            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = new StringBuilder();
            var currentLength = 0;

            foreach (var paragraph in paragraphs)
            {
                var paragraphLength = paragraph.Length;

                // Si el párrafo solo cabe en un chunk nuevo
                if (currentLength + paragraphLength > _chunkSize && currentLength > 0)
                {
                    chunks.Add(currentChunk.ToString());

                    // Iniciar nuevo chunk con overlap del chunk anterior
                    currentChunk = new StringBuilder();
                    if (chunks.Count > 0 && _overlapSize > 0)
                    {
                        var lastChunk = chunks.Last();
                        var overlapText = lastChunk.Substring(
                            Math.Max(0, lastChunk.Length - _overlapSize));
                        currentChunk.Append(overlapText);
                        currentChunk.Append("\n\n");
                        currentLength = overlapText.Length;
                    }
                    else
                    {
                        currentLength = 0;
                    }
                }

                // Si el párrafo es muy grande, dividirlo
                if (paragraphLength > _chunkSize)
                {
                    var sentences = SplitIntoSentences(paragraph);
                    foreach (var sentence in sentences)
                    {
                        if (currentLength + sentence.Length > _chunkSize && currentLength > 0)
                        {
                            chunks.Add(currentChunk.ToString());
                            currentChunk = new StringBuilder();
                            currentLength = 0;
                        }
                        currentChunk.Append(sentence + " ");
                        currentLength += sentence.Length + 1;
                    }
                }
                else
                {
                    currentChunk.Append(paragraph);
                    currentChunk.Append("\n\n");
                    currentLength += paragraphLength;
                }
            }

            // Agregar el último chunk si tiene contenido
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }

            return chunks;
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Patrón para detectar finales de oración en español
            var pattern = @"(?<=[.!?])\s+(?=[A-Z])";
            var sentences = Regex.Split(text, pattern);

            return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
    }
}
