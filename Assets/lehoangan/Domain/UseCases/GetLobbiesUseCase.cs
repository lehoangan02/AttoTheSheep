using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public class GetLobbiesUseCase
{
    private readonly ILobbyService _lobbyService;

    public GetLobbiesUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<QueryResponse> ExecuteAsync(QueryLobbiesOptions options = null)
    {
        options ??= new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            },
            Order = new List<QueryOrder>
            {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)
            }
        };
        return await _lobbyService.QueryLobbiesAsync(options);
    }
}
