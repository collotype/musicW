using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, SearchSourceFilter sourceFilter, CancellationToken cancellationToken);
}
