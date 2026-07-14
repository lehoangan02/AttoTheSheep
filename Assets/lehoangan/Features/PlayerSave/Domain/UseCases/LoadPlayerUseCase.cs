using System.Threading.Tasks;

public class LoadPlayerUseCase
{
    private readonly IPlayerRepository _repository;

    public LoadPlayerUseCase(IPlayerRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerProfile> ExecuteAsync()
    {
        var profile = await _repository.LoadAsync();
        
        return profile ?? new PlayerProfile(); 
    }
}