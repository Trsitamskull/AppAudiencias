using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AudienciasApp.Services.Configuration
{
    public interface IConfigurationService
    {
        AIConfiguration GetAIConfiguration();
        Task SaveAIConfiguration(AIConfiguration config);
        string GetApiKey();
        void SetApiKey(string apiKey);
        bool IsAIConfigured();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _configFilePath;
        private AIConfiguration _aiConfig;

        public ConfigurationService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<ConfigurationService>(optional: true)
                .AddEnvironmentVariables();
            
            _configuration = builder.Build();
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            _aiConfig = new AIConfiguration();
            _configuration.GetSection("AISettings").Bind(_aiConfig);
            
            // Intentar obtener API key de diferentes fuentes
            if (string.IsNullOrEmpty(_aiConfig.ApiKey))
            {
                // Intentar desde User Secrets
                _aiConfig.ApiKey = _configuration["AISettings:ApiKey"];
                
                // Intentar desde variable de entorno
                if (string.IsNullOrEmpty(_aiConfig.ApiKey))
                {
                    _aiConfig.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                }
            }
        }

        public AIConfiguration GetAIConfiguration()
        {
            return _aiConfig ?? new AIConfiguration();
        }

        public async Task SaveAIConfiguration(AIConfiguration config)
        {
            _aiConfig = config;
            
            // Leer configuración existente
            var jsonString = File.Exists(_configFilePath) 
                ? await File.ReadAllTextAsync(_configFilePath)
                : "{}";
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            using var doc = JsonDocument.Parse(jsonString);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            
            writer.WriteStartObject();
            
            // Copiar configuración existente excepto AISettings
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name != "AISettings")
                {
                    property.WriteTo(writer);
                }
            }
            
            // Escribir nueva configuración AI
            writer.WritePropertyName("AISettings");
            JsonSerializer.Serialize(writer, config, options);
            
            writer.WriteEndObject();
            writer.Flush();
            
            var updatedJson = Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(_configFilePath, updatedJson);
        }

        public string GetApiKey()
        {
            return _aiConfig?.ApiKey ?? string.Empty;
        }

        public void SetApiKey(string apiKey)
        {
            if (_aiConfig == null)
                _aiConfig = new AIConfiguration();
            
            _aiConfig.ApiKey = apiKey;
        }

        public bool IsAIConfigured()
        {
            return !string.IsNullOrEmpty(GetApiKey());
        }
    }

    public class AIConfiguration
    {
        public string Provider { get; set; } = "OpenAI";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public double Temperature { get; set; } = 0.3;
        public int MaxTokens { get; set; } = 2000;
        public int RetryAttempts { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    }
}
