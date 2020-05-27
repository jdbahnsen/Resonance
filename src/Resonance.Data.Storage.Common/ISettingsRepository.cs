using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface ISettingsRepository
    {
        Task<Collection> AddCollectionAsync(string name, string path, string filter, bool enabled, CancellationToken cancellationToken);

        Task AddUserAsync(string username, string password, CancellationToken cancellationToken);

        Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken);

        Task<User> GetUserAsync(string username, CancellationToken cancellationToken);

        Task RemoveCollectionAsync(Guid id, CancellationToken cancellationToken);

        Task SetPasswordAsync(string username, string password, CancellationToken cancellationToken);

        Task UpdateCollectionAsync(Guid id, bool enabled, CancellationToken cancellationToken);
    }
}