using BussinessLayer.DTOs;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    public interface IChunkSettingsService
    {
        ChunkSettingsDto GetSettings();
        Task SaveSettingsAsync(ChunkSettingsDto settings);
    }
}
