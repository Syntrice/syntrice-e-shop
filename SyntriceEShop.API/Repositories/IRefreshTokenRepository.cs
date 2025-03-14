using SyntriceEShop.API.Models.RefreshTokenModel;

namespace SyntriceEShop.API.Repositories;

public interface IRefreshTokenRepository
{
    RefreshToken Add(RefreshToken token);
    Task<RefreshToken?> GetByTokenValue(string token);
    Task RemoveAllByUserIdAsync(int userId);
}