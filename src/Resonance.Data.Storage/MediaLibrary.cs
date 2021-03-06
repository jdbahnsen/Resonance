﻿using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class MediaLibrary : IMediaLibrary
    {
        private static readonly object Mutex = new object();
        private static volatile bool _scanningLibrary;
        private static volatile ScanProgress _scanProgress;
        private readonly ICoverArtRepository _coverArtRepository;
        private readonly ILastFmClient _lastFmClient;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IMetadataRepositoryCache _metadataRepositoryCache;
        private readonly ISettingsRepository _settingsRepository;

        public MediaLibrary(IMetadataRepository metadataRepository, ILastFmClient lastFmClient, ISettingsRepository settingsRepository, ICoverArtRepository coverArtRepository, IMetadataRepositoryCache metadataRepositoryCache)
        {
            _metadataRepository = metadataRepository;
            _lastFmClient = lastFmClient;
            _coverArtRepository = coverArtRepository;
            _metadataRepositoryCache = metadataRepositoryCache;
            _settingsRepository = settingsRepository;
        }

        public bool ScanInProgress
        {
            get
            {
                lock (Mutex)
                {
                    return _scanningLibrary;
                }
            }
            set
            {
                lock (Mutex)
                {
                    _scanningLibrary = value;
                }
            }
        }

        public ScanProgress ScanProgress
        {
            get
            {
                lock (Mutex)
                {
                    return _scanProgress;
                }
            }

            set
            {
                lock (Mutex)
                {
                    _scanProgress = value;
                }
            }
        }

        public Task AddUserAsync(string username, string password, CancellationToken cancellationToken)
        {
            return _settingsRepository.AddUserAsync(username, password, cancellationToken);
        }

        public async Task ClearLibraryAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            await _metadataRepository.ClearCollectionAsync<Album>(collectionId, cancellationToken).ConfigureAwait(false);
            await _metadataRepository.ClearCollectionAsync<Artist>(collectionId, cancellationToken).ConfigureAwait(false);
            await _metadataRepository.ClearCollectionAsync<Genre>(collectionId, cancellationToken).ConfigureAwait(false);
            await _metadataRepository.ClearCollectionAsync<Track>(collectionId, cancellationToken).ConfigureAwait(false);
            await _metadataRepository.ClearCollectionAsync<Disposition>(collectionId, cancellationToken).ConfigureAwait(false);
            await _metadataRepository.ClearCollectionAsync<Playback>(collectionId, cancellationToken).ConfigureAwait(false);
        }

        public Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetAlbumAsync(userId, id, populate, cancellationToken);
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, string[] albumArtists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken)
        {
            var artists = await GetArtistsFromListAsync(userId, albumArtists, collectionId, cancellationToken).ConfigureAwait(false);

            return await _metadataRepositoryCache.GetAlbumAsync(userId, artists, name, collectionId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MediaInfo> GetAlbumInfoAsync(Album album, CancellationToken cancellationToken)
        {
            //var mediaInfo = await _metadataRepository.GetMediaInfoAsync(album.Id, cancellationToken);

            //if (mediaInfo == null)
            //{
            var mediaInfo = await _lastFmClient.GetAlbumInfoAsync(album, cancellationToken).ConfigureAwait(false);

            //	if (mediaInfo != null)
            //	{
            //		await _metadataRepository.InsertOrUpdateMediaInfoAsync(mediaInfo, cancellationToken);
            //	}
            //}

            return mediaInfo;
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetAlbumsAsync(userId, collectionId, populate, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetAlbumsByArtistAsync(userId, artistId, populate, cancellationToken);
        }

        public Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetArtistAsync(userId, id, cancellationToken);
        }

        public Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string name, Guid collectionId, bool create, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Task.FromResult((MediaBundle<Artist>)null);
            }

            if (create)
            {
                return _metadataRepositoryCache.GetArtistAsync(userId, name, collectionId, cancellationToken);
            }

            return _metadataRepository.GetArtistAsync(userId, name, collectionId, cancellationToken);
        }

        public async Task<MediaInfo> GetArtistInfoAsync(Artist artist, CancellationToken cancellationToken)
        {
            //var mediaInfo = await _metadataRepository.GetMediaInfoAsync(artist.Id, cancellationToken);

            //if (mediaInfo == null)
            //{
            var mediaInfo = await _lastFmClient.GetArtistInfoAsync(artist, cancellationToken).ConfigureAwait(false);

            //	if (mediaInfo != null)
            //	{
            //		await _metadataRepository.InsertOrUpdateMediaInfoAsync(mediaInfo, cancellationToken);
            //	}
            //}

            return mediaInfo;
        }

        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetArtistsAsync(userId, collectionId, cancellationToken);
        }

        public async Task<HashSet<Artist>> GetArtistsFromListAsync(Guid userId, IEnumerable<string> artistNames, Guid collectionId, CancellationToken cancellationToken)
        {
            var artists = new HashSet<Artist>();

            foreach (var artistName in artistNames)
            {
                var artist = await GetArtistAsync(userId, artistName, collectionId, true, cancellationToken).ConfigureAwait(false);
                artists.Add(artist.Media);
            }

            return artists.Count > 0 ? artists : null;
        }

        public async Task<CoverArt> GetCoverArtAsync(Guid userId, Guid id, int? size, CancellationToken cancellationToken)
        {
            var mediaBundle = await _metadataRepositoryCache.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            if (mediaBundle == null)
            {
                return null;
            }

            return _coverArtRepository.GetCoverArt(mediaBundle.Media, size);
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetFavoritedAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public Task<Genre> GetGenreAsync(string name, Guid collectionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Task.FromResult((Genre)null);
            }

            return _metadataRepositoryCache.GetGenreAsync(name, collectionId, cancellationToken);
        }

        public Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetGenreCountsAsync(collectionId, cancellationToken);
        }

        public Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetGenresAsync(collectionId, cancellationToken);
        }

        public async Task<HashSet<Genre>> GetGenresFromListAsync(IEnumerable<string> genreNames, Guid collectionId, CancellationToken cancellationToken)
        {
            var genres = new HashSet<Genre>();

            foreach (var genreName in genreNames)
            {
                var genre = await GetGenreAsync(genreName, collectionId, cancellationToken).ConfigureAwait(false);
                genres.Add(genre);
            }

            return genres.Count > 0 ? genres : null;
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetHighestRatedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetHighestRatedAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetNewestAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetPlaylistAsync(userId, id, getTracks, cancellationToken);
        }

        public Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetPlaylistsAsync(userId, username, getTracks, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetRandomAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaInfo>> GetSimilarArtistsAsync(Guid userId, Artist artist, bool autocorrect, int limit, Guid collectionId, CancellationToken cancellationToken)
        {
            var mediaInfo = new List<MediaInfo>();

            var similarArtists = await _lastFmClient.GetSimilarArtistsAsync(artist, autocorrect, limit, cancellationToken).ConfigureAwait(false);

            foreach (var similarArtist in similarArtists)
            {
                var artistModel = await GetArtistAsync(userId, similarArtist.LastFm.Name, collectionId, false, cancellationToken).ConfigureAwait(false);

                if (artistModel != null)
                {
                    similarArtist.MediaId = artistModel.Media.Id;
                }

                mediaInfo.Add(similarArtist);
            }

            return mediaInfo;
        }

        public Task<IEnumerable<MediaInfo>> GetTopTracksAsync(string artist, int count, CancellationToken cancellationToken)
        {
            return _lastFmClient.GetTopTracksAsync(artist, count, cancellationToken);
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetTrackAsync(userId, id, populate, cancellationToken);
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepositoryCache.GetTrackAsync(userId, artist, track, collectionId, populate, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken)
        {
            return _metadataRepository.GetTracksAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, randomize, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks)
        {
            _metadataRepositoryCache.RemovePlaylistFromCache(userId, id, getTracks);
        }

        public void ScanLibrary(Guid userId, Guid? collectionId, bool clear, CancellationToken cancellationToken)
        {
            if (ScanInProgress)
            {
                return;
            }

            Task.Run(async () =>
             {
                 ScanProgress = new ScanProgress();
                 ScanInProgress = true;
                 _metadataRepositoryCache.UseCache = false;

                 try
                 {
                     var collections = await _settingsRepository.GetCollectionsAsync(cancellationToken).ConfigureAwait(false);

                     var collectionCount = 0;

                     var collectionsToScan = collections.Where(c => c.Enabled).ToList();

                     if (collectionId.HasValue)
                     {
                         collectionsToScan = collectionsToScan.Where(c => c.Id == collectionId).ToList();
                     }

                     ScanProgress.TotalCollectionCount = collectionsToScan.Count;

                     foreach (var collection in collectionsToScan)
                     {
                         collectionCount++;

                         ScanProgress.CurrentCollection = collectionCount;
                         ScanProgress.CurrentCollectionId = collection.Id;

                         if (!Directory.Exists(collection.Path))
                         {
                             throw new Exception();
                         }

                         if (clear)
                         {
                             collection.DateModified = DateTime.MinValue;
                             await _metadataRepository.InsertOrUpdateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
                         }

                         var files = Directory.GetFiles(collection.Path, collection.Filter, SearchOption.AllDirectories);

                         var fileCount = 0;
                         ScanProgress.TotalFileCount = files.Length;

                         foreach (var file in files)
                         {
                             fileCount++;

                             ScanProgress.CurrentFile = fileCount;
                             ScanProgress.CurrentFilename = file;

                             if (fileCount == 1 || fileCount % 250 == 0)
                             {
                                 _metadataRepository.EndTransaction(true, cancellationToken);
                                 _metadataRepository.BeginTransaction(cancellationToken);
                             }

                             await _metadataRepositoryCache.GetTrackAsync(userId, file, collection.Id, false, true, cancellationToken).ConfigureAwait(false);

                             if (!ScanInProgress)
                             {
                                 break;
                             }
                         }

                         if (!ScanInProgress)
                         {
                             break;
                         }

                         _metadataRepository.EndTransaction(true, cancellationToken);

                         foreach (var track in await _metadataRepository.GetTracksAsync(userId, collection.Id, cancellationToken).ConfigureAwait(false))
                         {
                             var fileExists = File.Exists(track.Media.Path);

                             if (track.Media.Visible == fileExists)
                             {
                                 continue;
                             }

                             track.Media.Visible = fileExists;

                             await _metadataRepository.InsertOrUpdateFileInfoAsync(track.Media, cancellationToken).ConfigureAwait(false);

                             if (!fileExists)
                             {
                                 await _metadataRepository.DeleteTrackReferencesAsync(track.Media, cancellationToken).ConfigureAwait(false);
                             }
                         }

                         await _metadataRepository.DeleteAlbumReferencesAsync(cancellationToken).ConfigureAwait(false);
                     }
                 }
                 catch (Exception ex)
                 {
                     _metadataRepository.EndTransaction(false, cancellationToken);

                     File.WriteAllText($"{Guid.NewGuid():n}.txt", ex.ToString());
                 }
                 finally
                 {
                     ScanProgress = null;
                     ScanInProgress = false;
                     _metadataRepositoryCache.UseCache = true;
                 }
             }, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Album>>> SearchAlbumsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.SearchAsync<Album>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Artist>>> SearchArtistsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.SearchAsync<Artist>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Track>>> SearchTracksAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return _metadataRepository.SearchAsync<Track>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public void StopScanningLibrary(Guid userId, CancellationToken cancellationToken)
        {
            ScanInProgress = false;
        }
    }
}