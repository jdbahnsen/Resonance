using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IMetadataRepository _metadataRepository;

        public SettingsRepository(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public async Task<Collection> AddCollectionAsync(string name, string path, string filter, bool enabled, CancellationToken cancellationToken)
        {
            var collection = new Collection
            {
                DateAdded = DateTime.UtcNow,
                Enabled = enabled,
                Filter = filter,
                Name = name,
                Path = path
            };

            await _metadataRepository.AddCollectionAsync(collection, cancellationToken).ConfigureAwait(false);

            return collection;
        }

        public async Task AddUserAsync(string username, string password, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Name = username,
                Password = password
            };

            await _metadataRepository.AddUserAsync(user, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetCollectionsAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task<User> GetUserAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveCollectionAsync(Guid id, CancellationToken cancellationToken)
        {
            var collections = await _metadataRepository.GetCollectionsAsync(cancellationToken).ConfigureAwait(false);

            var collection = collections.FirstOrDefault(c => c.Id == id);

            if (collection != null)
            {
                await _metadataRepository.RemoveCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task SetPasswordAsync(string username, string password, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCollectionAsync(Guid id, bool enabled, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}