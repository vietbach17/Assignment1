using BussinessLayer.DTOs;
using System.Threading.Tasks;

namespace BussinessLayer.Interfaces
{
    public interface IChunkSettingsService
    {
        ChunkSettingsDto GetSettings();
        Task SaveSettingsAsync(ChunkSettingsDto settings);
    }
}
