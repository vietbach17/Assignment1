using BussinessLayer.DTOs;
using BussinessLayer.Interfaces;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace BussinessLayer.Services
{
    public class ChunkSettingsService : IChunkSettingsService
    {
        private readonly string _settingsFilePath;

        public ChunkSettingsService(IWebHostEnvironment env)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            _settingsFilePath = Path.Combine(uploadsFolder, "chunk_settings.json");
        }

        public ChunkSettingsDto GetSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new ChunkSettingsDto { MaxWords = 300, OverlapWords = 50 };
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ChunkSettingsDto>(json);
                return settings ?? new ChunkSettingsDto { MaxWords = 300, OverlapWords = 50 };
            }
            catch
            {
                return new ChunkSettingsDto { MaxWords = 300, OverlapWords = 50 };
            }
        }

        public async Task SaveSettingsAsync(ChunkSettingsDto settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
    }
}
