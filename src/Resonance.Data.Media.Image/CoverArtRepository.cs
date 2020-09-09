using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Resonance.Data.Media.Image
{
    public class CoverArtRepository : ICoverArtRepository
    {
        private const string CoverArt = "CoverArt";
        private const string Full = "full";
        private static readonly ConcurrentDictionary<string, object> ProcessingFiles = new ConcurrentDictionary<string, object>();

        private readonly string _coverArtPath;
        private readonly string _fullCoverArtPath;
        private readonly ITagReaderFactory _tagReaderFactory;

        public CoverArtRepository(IMetadataRepositorySettings metadataRepositorySettings, ITagReaderFactory tagReaderFactory)
        {
            _tagReaderFactory = tagReaderFactory;
            _coverArtPath = Path.Combine(metadataRepositorySettings.ResonancePath, CoverArt);
            _fullCoverArtPath = Path.Combine(_coverArtPath, Full);
        }

        public CoverArt GetCoverArt(Track track, int? size)
        {
            var fullTrackCoverPath = Path.Combine(_fullCoverArtPath, track.Id.ToString("n"));
            var coverArtPath = size.HasValue ? Path.Combine(_coverArtPath, size.Value.ToString()) : _fullCoverArtPath;
            var trackCoverArtPath = Path.Combine(coverArtPath, track.Id.ToString("n"));

            var coverArt = GetScaledCoverArt(track, trackCoverArtPath);

            if (coverArt != null)
            {
                return coverArt;
            }

            coverArt = GetScaledCoverArt(track, fullTrackCoverPath);

            if (coverArt == null)
            {
                if (File.Exists(track.Path))
                {
                    var tagReader = _tagReaderFactory.CreateTagReader(track.Path, false);

                    coverArt = tagReader.CoverArt.FirstOrDefault(ca => ca.CoverArtType == CoverArtType.Front || ca.CoverArtType == CoverArtType.Other);
                }

                if (coverArt != null)
                {
                    WriteCoverArtToDisk(fullTrackCoverPath, coverArt.CoverArtData);
                }
                else
                {
                    return null;
                }
            }

            // Resize the image if requested
            if (size.HasValue)
            {
                using var imageMemoryStream = new MemoryStream();
                using var image = SixLabors.ImageSharp.Image.Load(coverArt.CoverArtData.Span);
                var resizeOptions = new ResizeOptions { Size = new Size { Height = size.Value, Width = size.Value }, Mode = ResizeMode.Max };

                var resizedImageData = image.Clone(ctx => ctx.Resize(resizeOptions));

                // Save to PNG to retain quality at the expense of file size
                resizedImageData.SaveAsPng(imageMemoryStream);

                coverArt.CoverArtData = imageMemoryStream.ToArray();

                WriteCoverArtToDisk(trackCoverArtPath, coverArt.CoverArtData);
            }

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, trackCoverArtPath);

            return coverArt;
        }

        private static CoverArt GetScaledCoverArt(Track track, string trackCoverArtPath)
        {
            var lockObject = ProcessingFiles.GetOrAdd(trackCoverArtPath, new object());

            try
            {
                lock (lockObject)
                {
                    if (!(File.Exists(trackCoverArtPath) && track.DateFileModified.ToUniversalTime() < File.GetLastWriteTimeUtc(trackCoverArtPath)))
                    {
                        return null;
                    }
                }
            }
            finally
            {
                ProcessingFiles.TryRemove(trackCoverArtPath, out _);
            }

            // Return the album art on disk if the file exists and is newer than the last modified date of the track

            var coverArtData = ReadCoverArtFromDisk(trackCoverArtPath);

            return new CoverArt
            {
                CoverArtData = coverArtData,
                CoverArtType = CoverArtType.Front,
                MediaId = track.Id,
                Size = coverArtData.Length,
                MimeType = MimeType.GetMimeType(coverArtData, trackCoverArtPath)
            };
        }

        private static ReadOnlyMemory<byte> ReadCoverArtFromDisk(string path)
        {
            var lockObject = ProcessingFiles.GetOrAdd(path, new object());

            byte[] result;

            try
            {
                lock (lockObject)
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    result = new byte[stream.Length];

                    stream.Read(result, 0, (int)stream.Length);
                }
            }
            finally
            {
                ProcessingFiles.TryRemove(path, out _);
            }

            return result;
        }

        private static void WriteCoverArtToDisk(string path, ReadOnlyMemory<byte> bytes)
        {
            var lockObject = ProcessingFiles.GetOrAdd(path, new object());

            try
            {
                lock (lockObject)
                {
                    var parentDirectory = Path.GetDirectoryName(path);

                    if (parentDirectory != null && !Directory.Exists(parentDirectory))
                    {
                        Directory.CreateDirectory(parentDirectory);
                    }

                    using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

                    stream.Write(bytes.ToArray(), 0, bytes.Length);
                }
            }
            finally
            {
                ProcessingFiles.TryRemove(path, out _);
            }
        }
    }
}