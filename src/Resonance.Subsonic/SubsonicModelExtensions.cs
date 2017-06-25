﻿using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Resonance.Common;
using Resonance.Data.Storage.Common;
using Subsonic.Common.Classes;

namespace Resonance.SubsonicCompat
{
	public static class SubsonicModelExtensions
	{
		public static AlbumID3 ToSubsonicAlbumID3(this MediaBundle<Album> albumMediaBundle)
		{
			var subsonicAlbum = new AlbumID3();

			var album = albumMediaBundle.Media;

			var artist = album.Artists?.FirstOrDefault();

			if (artist != null)
			{
				subsonicAlbum.ArtistId = artist.Media.Id.ToString("n");
				subsonicAlbum.Artist = string.Join(" / ", album.Artists.Select(a => a.Media.Name));
			}

			if (album.Tracks != null)
			{
				subsonicAlbum.CoverArt = album.Tracks.First().Media.Id.ToString("n");
				subsonicAlbum.Year = album.Tracks.First().Media.ReleaseDate;
				subsonicAlbum.Duration = (int)album.Duration.TotalSeconds;
				subsonicAlbum.SongCount = album.Tracks.Count;
			}

			subsonicAlbum.Id = album.Id.ToString("n");

			if (album?.Genres?.Any() == true)
			{
				subsonicAlbum.Genre = string.Join(" / ", album.Genres.Select(g => g.Name));
			}

			var disposition = albumMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				if (disposition.Favorited.HasValue)
				{
					subsonicAlbum.Starred = disposition.Favorited.Value;
				}
			}

			subsonicAlbum.Created = album.DateAdded;
			subsonicAlbum.Name = album.Name;

			return subsonicAlbum;
		}

		public static AlbumWithSongsID3 ToSubsonicAlbumWithSongsID3(this MediaBundle<Album> albumMediaBundle)
		{
			var subsonicAlbum = new AlbumWithSongsID3();

			var album = albumMediaBundle.Media;

			var artist = album.Artists?.FirstOrDefault();

			if (artist != null)
			{
				subsonicAlbum.ArtistId = artist.Media.Id.ToString("n");
				subsonicAlbum.Artist = string.Join(" / ", album.Artists.Select(a => a.Media.Name));
			}

			if (album.Tracks != null)
			{
				subsonicAlbum.CoverArt = album.Tracks.First().Media.Id.ToString("n");
				subsonicAlbum.Year = album.Tracks.First().Media.ReleaseDate;
				subsonicAlbum.Duration = (int)album.Duration.TotalSeconds;
				subsonicAlbum.SongCount = album.Tracks.Count;
			}

			subsonicAlbum.Id = album.Id.ToString("n");

			if (album?.Genres?.Any() == true)
			{
				subsonicAlbum.Genre = string.Join(" / ", album.Genres.Select(g => g.Name));
			}

			var disposition = albumMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				if (disposition.Favorited.HasValue)
				{
					subsonicAlbum.Starred = disposition.Favorited.Value;
				}
			}

			subsonicAlbum.Created = album.DateAdded;
			subsonicAlbum.Name = album.Name;

			subsonicAlbum.Songs = album.Tracks.Select(t => t.ToSubsonicSong(albumMediaBundle)).ToList();

			return subsonicAlbum;
		}

		public static Subsonic.Common.Classes.Artist ToSubsonicArtist(this MediaBundle<Data.Models.Artist> artistMediaBundle)
		{
			var subsonicArtist = new Subsonic.Common.Classes.Artist();

			var artist = artistMediaBundle.Media;

			subsonicArtist.Name = artist.Name;
			subsonicArtist.Id = artist.Id.ToString("n");

			var disposition = artistMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				subsonicArtist.AverageRating = disposition.AverageRating ?? 0.0;

				if (disposition.Favorited.HasValue)
				{
					subsonicArtist.Starred = disposition.Favorited.Value;
				}

				subsonicArtist.UserRating = disposition.UserRating ?? 0;
			}

			return subsonicArtist;
		}

		public static ArtistID3 ToSubsonicArtistID3(this MediaBundle<Data.Models.Artist> artistMediaBundle)
		{
			var subsonicArtist = new ArtistID3();

			var artist = artistMediaBundle.Media;

			subsonicArtist.Name = artist.Name;
			subsonicArtist.Id = artist.Id.ToString("n");
			subsonicArtist.CoverArt = $"ar-{subsonicArtist.Id}";

			var disposition = artistMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				if (disposition.Favorited.HasValue)
				{
					subsonicArtist.Starred = disposition.Favorited.Value;
				}
			}

			return subsonicArtist;
		}

		public static ArtistWithAlbumsID3 ToSubsonicArtistWithAlbumsID3(this MediaBundle<Data.Models.Artist> artistMediaBundle, IEnumerable<MediaBundle<Album>> albumMediaBundles)
		{
			var subsonicArtist = new ArtistWithAlbumsID3();

			var artist = artistMediaBundle.Media;

			subsonicArtist.Name = artist.Name;
			subsonicArtist.Id = artist.Id.ToString("n");
			subsonicArtist.CoverArt = $"ar-{subsonicArtist.Id}";

			var disposition = artistMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				if (disposition.Favorited.HasValue)
				{
					subsonicArtist.Starred = disposition.Favorited.Value;
				}
			}

			subsonicArtist.AlbumCount = albumMediaBundles.Count();
			subsonicArtist.Albums = albumMediaBundles.Select(a => a.ToSubsonicAlbumID3()).ToList();

			return subsonicArtist;
		}

		public static async Task<Subsonic.Common.Classes.Bookmark> ToSubsonicBookmarkAsync(this Marker marker, IMediaLibrary mediaLibrary, CancellationToken cancellationToken)
		{
			var userId = marker.User.Id;

			var track = await mediaLibrary.GetTrackAsync(userId, marker.TrackId, false, cancellationToken).ConfigureAwait(false);
			var subsonicSong = track.ToSubsonicSong(await mediaLibrary.GetAlbumAsync(userId, track.Media.AlbumId, false, cancellationToken).ConfigureAwait(false));

			var subsonicBookmark = new Subsonic.Common.Classes.Bookmark
			{
				Position = marker.Position,
				Comment = marker.Comment ?? string.Empty,
				Username = marker.User.Name,
				Changed = marker.DateModified ?? marker.DateAdded,
				Created = marker.DateAdded,
				Children = new List<Child>
				{
					subsonicSong
				}
			};

			return subsonicBookmark;
		}

		public static async Task<Subsonic.Common.Classes.Bookmarks> ToSubsonicBookmarksAsync(this IEnumerable<Marker> markers, IMediaLibrary mediaLibrary, CancellationToken cancellationToken)
		{
			var subsonicBookmarkItems = new List<Bookmark>();

			foreach (var marker in markers)
			{
				var subsonicBookmark = await marker.ToSubsonicBookmarkAsync(mediaLibrary, cancellationToken).ConfigureAwait(false);
				subsonicBookmarkItems.Add(subsonicBookmark);
			}

			var subsonicBookmarks = new Subsonic.Common.Classes.Bookmarks
			{
				Items = subsonicBookmarkItems
			};

			return subsonicBookmarks;
		}

		public static Subsonic.Common.Classes.ChatMessage ToSubsonicChatMessage(this Chat chat)
		{
			var subsonicChatMessage = new Subsonic.Common.Classes.ChatMessage
			{
				Time = DateTimeExtensions.GetUnixTimestampMillis(chat.Timestamp),
				Message = chat.Message,
				Username = chat.User.Name
			};

			return subsonicChatMessage;
		}

		public static Subsonic.Common.Classes.Child ToSubsonicChild(this MediaBundle<Album> albumMediaBundle)
		{
			var subsonicChild = new Subsonic.Common.Classes.Child();

			var album = albumMediaBundle.Media;

			subsonicChild.Album = album.Name;

			var artist = album.Artists?.FirstOrDefault();

			if (artist != null)
			{
				subsonicChild.ArtistId = artist.Media.Id.ToString("n");
				subsonicChild.Artist = string.Join(" / ", album.Artists.Select(a => a.Media.Name));
				subsonicChild.Parent = subsonicChild.ArtistId;
			}

			if (album.Tracks != null)
			{
				subsonicChild.CoverArt = album.Tracks.First().Media.Id.ToString("n");
				subsonicChild.Year = album.Tracks.First().Media.ReleaseDate;
				subsonicChild.Duration = (int)album.Duration.TotalSeconds;
			}

			subsonicChild.Id = album.Id.ToString("n");
			subsonicChild.Title = album.Name;
			subsonicChild.IsDir = true;

			if (album?.Genres?.Any() == true)
			{
				subsonicChild.Genre = string.Join(" / ", album.Genres.Select(g => g.Name));
			}

			var disposition = albumMediaBundle.Dispositions.FirstOrDefault();

			if (disposition != null)
			{
				if (disposition.AverageRating.HasValue)
				{
					subsonicChild.AverageRating = disposition.AverageRating.Value;
				}

				if (disposition.Favorited.HasValue)
				{
					subsonicChild.Starred = disposition.Favorited.Value;
				}

				if (disposition.UserRating.HasValue)
				{
					subsonicChild.UserRating = disposition.UserRating.Value;
				}
			}

			subsonicChild.Created = album.DateAdded;

			return subsonicChild;
		}

		public static Subsonic.Common.Classes.Genre ToSubsonicGenre(this Data.Models.Genre genre, int albumCount, int songCount)
		{
			var subsonicGenre = new Subsonic.Common.Classes.Genre
			{
				AlbumCount = albumCount,
				Name = genre.Name,
				SongCount = songCount
			};

			return subsonicGenre;
		}

		public static Subsonic.Common.Classes.NowPlayingEntry ToSubsonicNowPlayingEntry(this MediaBundle<Track> trackMediaBundle, MediaBundle<Album> albumMediaBundle, Disposition disposition, Playback playback, Data.Models.User user)
		{
			var nowPlayingEntry = new Subsonic.Common.Classes.NowPlayingEntry();

			var track = trackMediaBundle.Media;

			if (albumMediaBundle != null)
			{
				nowPlayingEntry.Album = albumMediaBundle.Media.Name;
			}

			nowPlayingEntry.AlbumId = track.AlbumId.ToString("n");

			var artist = track.Artists.FirstOrDefault();

			if (artist != null)
			{
				nowPlayingEntry.ArtistId = artist.Media.Id.ToString("n");
				nowPlayingEntry.Artist = string.Join(" / ", track.Artists.Select(a => a.Media.Name));
			}

			nowPlayingEntry.BitRate = track.Bitrate;
			nowPlayingEntry.ContentType = track.ContentType;
			nowPlayingEntry.CoverArt = track.Id.ToString("n");
			nowPlayingEntry.Created = track.DateFileCreated;
			nowPlayingEntry.DiscNumber = track.DiscNumber;
			nowPlayingEntry.Duration = (int)track.Duration.TotalSeconds;

			if (track.Genres.Count > 0)
			{
				nowPlayingEntry.Genre = string.Join(" / ", track.Genres.Select(g => g.Name));
			}

			var parentDirectory = System.IO.Directory.GetParent(track.Path);
			var grandParentDirectory = System.IO.Directory.GetParent(parentDirectory.FullName);

			nowPlayingEntry.Id = track.Id.ToString("n");
			nowPlayingEntry.IsDir = false;
			nowPlayingEntry.IsVideo = false;
			nowPlayingEntry.Parent = track.AlbumId.ToString("n");
			nowPlayingEntry.Path = Path.Combine(grandParentDirectory.Name, parentDirectory.Name, Path.GetFileName(track.Path)).Replace(Path.DirectorySeparatorChar, '/');
			nowPlayingEntry.Size = track.Size;

			if (disposition != null)
			{
				nowPlayingEntry.AverageRating = disposition.AverageRating ?? 0.0;

				if (disposition.Favorited.HasValue)
				{
					nowPlayingEntry.Starred = disposition.Favorited.Value;
				}

				nowPlayingEntry.UserRating = disposition.UserRating ?? 0;
			}

			nowPlayingEntry.Suffix = Path.GetExtension(track.Path);
			nowPlayingEntry.Title = track.Name;
			nowPlayingEntry.Track = track.Number;
			nowPlayingEntry.Type = Subsonic.Common.Enums.MediaType.Music;
			nowPlayingEntry.Year = track.ReleaseDate;

			nowPlayingEntry.MinutesAgo = (DateTime.UtcNow - playback.PlaybackDateTime).Minutes;
			nowPlayingEntry.PlayerName = playback.ClientId;
			nowPlayingEntry.Username = user.Name;

			return nowPlayingEntry;
		}

		public static Subsonic.Common.Classes.Playlist ToSubsonicPlaylist(this Data.Models.Playlist playlist)
		{
			var subsonicPlaylist = new Subsonic.Common.Classes.Playlist
			{
				Changed = playlist.DateModified ?? playlist.DateAdded,
				Comment = playlist.Comment,
				CoverArt = $"pl-{playlist.Id.ToString()}",
				Created = playlist.DateAdded,
				Id = playlist.Id.ToString("n"),
				Name = playlist.Name,
				Owner = playlist.User.Name,
				Public = playlist.Accessibility == Accessibility.Public
			};

			if (playlist.Tracks != null)
			{
				subsonicPlaylist.Duration = playlist.Tracks.Sum(t => (int)t.Media.Duration.TotalSeconds);
				subsonicPlaylist.SongCount = playlist.Tracks.Count;
			}

			return subsonicPlaylist;
		}

		public static PlaylistWithSongs ToSubsonicPlaylistWithSongs(this Data.Models.Playlist playlist)
		{
			var subsonicPlaylist = new PlaylistWithSongs
			{
				Changed = playlist.DateModified ?? playlist.DateAdded,
				Comment = playlist.Comment,
				CoverArt = $"pl-{playlist.Id.ToString()}",
				Created = playlist.DateAdded,
				Id = playlist.Id.ToString("n"),
				Name = playlist.Name,
				Owner = playlist.User.Name,
				Public = playlist.Accessibility == Accessibility.Public
			};

			if (playlist.Tracks != null)
			{
				subsonicPlaylist.Duration = playlist.Tracks.Sum(t => (int)t.Media.Duration.TotalSeconds);
				subsonicPlaylist.SongCount = playlist.Tracks.Count;
			}

			return subsonicPlaylist;
		}

		public static Subsonic.Common.Classes.PlayQueue ToSubsonicPlayQueue(this Data.Models.PlayQueue playQueue)
		{
			if (playQueue == null)
			{
				return new Subsonic.Common.Classes.PlayQueue();
			}

			var subsonicPlayQueue = new Subsonic.Common.Classes.PlayQueue
			{
				Changed = playQueue.DateModified ?? playQueue.DateAdded,
				ChangedBy = playQueue.ClientName,
				Current = playQueue.CurrentTrackId?.ToString("n"),
				Position = playQueue.Position ?? 0,
				Username = playQueue.User.Name,
				Entries = new System.Collections.Generic.List<Subsonic.Common.Classes.Child>()
			};

			if (playQueue.Tracks == null)
				return subsonicPlayQueue;

			foreach (var track in playQueue.Tracks)
			{
				subsonicPlayQueue.Entries.Add(track.ToSubsonicSong(null));
			}

			return subsonicPlayQueue;
		}

		public static Subsonic.Common.Classes.Child ToSubsonicSong(this MediaBundle<Track> track, MediaBundle<Album> album)
		{
			return track.Media.ToSubsonicSong(album?.Media, track.Dispositions.FirstOrDefault());
		}

		public static Subsonic.Common.Classes.Child ToSubsonicSong(this Track track, Album album, Disposition disposition)
		{
			var subsonicSong = new Subsonic.Common.Classes.Child();

			if (album != null)
			{
				subsonicSong.Album = album.Name;
			}

			subsonicSong.AlbumId = track.AlbumId.ToString("n");

			var artist = track.Artists?.FirstOrDefault();

			if (artist != null)
			{
				subsonicSong.ArtistId = artist.Media.Id.ToString("n");
				subsonicSong.Artist = string.Join(" / ", track.Artists.Select(a => a.Media.Name));
			}

			subsonicSong.BitRate = track.Bitrate;
			subsonicSong.ContentType = track.ContentType;
			subsonicSong.CoverArt = track.Id.ToString("n");
			subsonicSong.Created = track.DateFileCreated;
			subsonicSong.DiscNumber = track.DiscNumber;
			subsonicSong.Duration = (int)track.Duration.TotalSeconds;

			if (track?.Genres?.Any() == true)
			{
				subsonicSong.Genre = string.Join(" / ", track.Genres.Select(g => g.Name));
			}

			var parentDirectory = System.IO.Directory.GetParent(track.Path);
			var grandParentDirectory = System.IO.Directory.GetParent(parentDirectory.FullName);

			subsonicSong.Id = track.Id.ToString("n");
			subsonicSong.IsDir = false;
			subsonicSong.IsVideo = false;
			subsonicSong.Parent = track.AlbumId.ToString("n");
			subsonicSong.Path = Path.Combine(grandParentDirectory.Name, parentDirectory.Name, Path.GetFileName(track.Path)).Replace(Path.DirectorySeparatorChar, '/');
			subsonicSong.Size = track.Size;

			if (disposition != null)
			{
				subsonicSong.AverageRating = disposition.AverageRating ?? 0.0;

				if (disposition.Favorited.HasValue)
				{
					subsonicSong.Starred = disposition.Favorited.Value;
				}

				subsonicSong.UserRating = disposition.UserRating ?? 0;
			}

			subsonicSong.Suffix = Path.GetExtension(track.Path);
			subsonicSong.Title = track.Name;
			subsonicSong.Track = track.Number;
			subsonicSong.Type = Subsonic.Common.Enums.MediaType.Music;
			subsonicSong.Year = track.ReleaseDate;

			return subsonicSong;
		}

		public static Subsonic.Common.Classes.User ToSubsonicUser(this Data.Models.User user)
		{
			var subsonicUser = new Subsonic.Common.Classes.User
			{
				AdminRole = user.Roles.Contains(Role.Administrator),
				CommentRole = user.Roles.Contains(Role.Administrator),
				CoverArtRole = user.Roles.Contains(Role.Administrator),
				JukeboxRole = user.Roles.Contains(Role.Administrator),
				PlaylistRole = user.Roles.Contains(Role.Administrator),
				PodcastRole = user.Roles.Contains(Role.Administrator),
				ScrobblingEnabled = user.Roles.Contains(Role.Administrator),
				ShareRole = user.Roles.Contains(Role.Administrator),
				UploadRole = user.Roles.Contains(Role.Administrator),
				VideoConversionRole = user.Roles.Contains(Role.Administrator),
				DownloadRole = user.Roles.Contains(Role.Download) || user.Roles.Contains(Role.Administrator),
				Email = user.EmailAddress,
				SettingsRole = user.Roles.Contains(Role.Settings) || user.Roles.Contains(Role.Administrator),
				StreamRole = user.Roles.Contains(Role.Playback) || user.Roles.Contains(Role.Administrator),
				Username = user.Name
			};

			return subsonicUser;
		}
	}
}