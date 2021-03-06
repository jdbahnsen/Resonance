﻿using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("album")]
    public class Album : MediaBase
    {
        [JsonProperty("artists")]
        public virtual HashSet<MediaBundle<Artist>> Artists { get; set; }

        [JsonProperty("discs")]
        public int Discs { get; set; }

        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        [JsonProperty("releaseDate")]
        public int ReleaseDate { get; set; }

        [JsonProperty("tracks")]
        public virtual HashSet<MediaBundle<Track>> Tracks { get; set; }

        public static Album FromDynamic(dynamic result)
        {
            return new Album
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Name = result.Name
            };
        }

        public void AddArtists(IEnumerable<MediaBundle<Artist>> artists)
        {
            Artists = Artists.AddValuesToCollection(artists);
        }

        public void AddTrack(MediaBundle<Track> track)
        {
            Discs = Math.Max(Discs, track.Media.DiscNumber);
            Duration = Duration.Add(track.Media.Duration);
            ReleaseDate = Math.Max(ReleaseDate, track.Media.ReleaseDate);

            Tracks = Tracks.AddValueToCollection(track);
            Genres = Genres.AddValuesToCollection(track.Media.Genres);
        }
    }
}