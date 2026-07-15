using System.Collections.Generic;
using System.Threading.Tasks;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public interface IHomepageBannerService
{
    Task<IEnumerable<HomepageBanner>> GetAllBannersAsync();
    Task<IEnumerable<HomepageBanner>> GetActiveBannersAsync();
    Task<HomepageBanner?> GetBannerByIdAsync(int id);
    Task<bool> CreateBannerAsync(HomepageBanner banner);
    Task<bool> UpdateBannerAsync(HomepageBanner banner);
    Task<bool> DeleteBannerAsync(int id);
}
