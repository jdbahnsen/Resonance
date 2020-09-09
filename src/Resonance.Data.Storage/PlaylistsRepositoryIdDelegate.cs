using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public sealed class PlaylistsRepositoryIdDelegate : RepositoryCacheDelegate<IEnumerable<Playlist>>
    {
        public PlaylistsRepositoryIdDelegate(Guid userId, string username, bool getTracks)
        {
            UserId = userId;
            Username = username;
            GetTracks = getTracks;
        }

        private bool GetTracks { get; }
        private Guid UserId { get; }
        private string Username { get; }

        public Func<CancellationToken, Task<IEnumerable<Playlist>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return cancellationToken => metadataRepository.GetPlaylistsAsync(UserId, Username, GetTracks, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(PlaylistsRepositoryIdDelegate left, PlaylistsRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(PlaylistsRepositoryIdDelegate left, PlaylistsRepositoryIdDelegate right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            return left.PropertiesEqual(right, nameof(GetTracks), nameof(UserId), nameof(Username));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as PlaylistsRepositoryIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(GetTracks, UserId, Username);
        }

        private bool Equals(PlaylistsRepositoryIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}