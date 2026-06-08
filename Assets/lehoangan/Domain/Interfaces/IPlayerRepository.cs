using System.Threading.Tasks;

// The Domain defines what it needs from the outside world.
public interface IPlayerRepository
{
    Task SaveAsync(PlayerProfile profile);
    Task<PlayerProfile> LoadAsync();
}