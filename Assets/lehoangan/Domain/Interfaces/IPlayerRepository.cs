using System.Threading.Tasks;

public interface IPlayerRepository
{
    Task SaveAsync(PlayerProfile profile);
    Task<PlayerProfile> LoadAsync();
}