﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Trakt.Api.DataContracts.Users.Collection;
using Trakt.Api.Enums;
using Trakt.Helpers;

namespace Trakt
{
    /// <summary>
    /// Class for trakt.tv plugin extension functions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert string to int.
        /// </summary>
        /// <param name="input">String to convert to int.</param>
        /// <returns>int?.</returns>
        public static int? ConvertToInt(this string input)
        {
            if (int.TryParse(input, out int result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Checks if <see cref="TraktMetadata"/> is empty.
        /// </summary>
        /// <param name="metadata">String to convert to int.</param>
        /// <returns><see cref="bool"/> indicating if the provided <see cref="TraktMetadata"/> is empty.</returns>
        public static bool IsEmpty(this TraktMetadata metadata)
            => metadata.MediaType == null
               && metadata.Resolution == null
               && metadata.Audio == null
               && string.IsNullOrEmpty(metadata.AudioChannels);

        /// <summary>
        /// Gets the trakt.tv codec representation of a <see cref="MediaStream"/>.
        /// </summary>
        /// <param name="audioStream">The <see cref="MediaStream"/>.</param>
        /// <returns>TraktAudio.</returns>
        public static TraktAudio? GetCodecRepresetation(this MediaStream audioStream)
        {
            var audio = audioStream != null && !string.IsNullOrEmpty(audioStream.Codec)
                ? audioStream.Codec.ToLowerInvariant().Replace(' ', '_')
                : null;
            switch (audio)
            {
                case "truehd":
                    return TraktAudio.dolby_truehd;
                case "dts":
                case "dca":
                    return TraktAudio.dts;
                case "dtshd":
                    return TraktAudio.dts_ma;
                case "ac3":
                    return TraktAudio.dolby_digital;
                case "aac":
                    return TraktAudio.aac;
                case "mp2":
                    return TraktAudio.mp3;
                case "pcm":
                    return TraktAudio.lpcm;
                case "ogg":
                    return TraktAudio.ogg;
                case "wma":
                    return TraktAudio.wma;
                case "flac":
                    return TraktAudio.flac;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if metadata of new collected movie is different from the already collected.
        /// </summary>
        /// <param name="collectedMovie">The <see cref="TraktMovieCollected"/>.</param>
        /// <param name="movie">The <see cref="Movie"/>.</param>
        /// <returns><see cref="bool"/> indicating if the new movie has different metadata to the already collected.</returns>
        public static bool MetadataIsDifferent(this TraktMovieCollected collectedMovie, Movie movie)
        {
            var audioStream = movie.GetMediaStreams().FirstOrDefault(x => x.Type == MediaStreamType.Audio);

            var resolution = movie.GetDefaultVideoStream().GetResolution();
            var is3D = movie.Is3D;
            var hdr = movie.GetDefaultVideoStream().GetHdr();
            var audio = GetCodecRepresetation(audioStream);
            var audioChannels = audioStream.GetAudioChannels();

            if (collectedMovie.Metadata == null || collectedMovie.Metadata.IsEmpty())
            {
                return resolution != null
                       || audio != null
                       || !string.IsNullOrEmpty(audioChannels);
            }

            return collectedMovie.Metadata.Audio != audio
                   || collectedMovie.Metadata.AudioChannels != audioChannels
                   || collectedMovie.Metadata.Resolution != resolution
                   || collectedMovie.Metadata.Is3D != is3D
                   || collectedMovie.Metadata.Hdr != hdr;
        }

        /// <summary>
        /// Gets the resolution of a <see cref="MediaStream"/>.
        /// </summary>
        /// <param name="videoStream">The <see cref="MediaStream"/>.</param>
        /// <returns>string.</returns>
        public static TraktResolution? GetResolution(this MediaStream videoStream)
        {
            if (videoStream == null)
            {
                return null;
            }

            if (!videoStream.Width.HasValue)
            {
                return null;
            }

            if (videoStream.Width.Value >= 3800)
            {
                return TraktResolution.uhd_4k;
            }

            if (videoStream.Width.Value >= 1900)
            {
                return TraktResolution.hd_1080p;
            }

            if (videoStream.Width.Value >= 1270)
            {
                return TraktResolution.hd_720p;
            }

            if (videoStream.Width.Value >= 700)
            {
                return TraktResolution.sd_480p;
            }

            return null;
        }

        /// <summary>
        /// Gets the HDR type of a <see cref="MediaStream"/>.
        /// </summary>
        /// <param name="videoStream">The <see cref="MediaStream"/>.</param>
        /// <returns>string.</returns>
        public static TraktHdr? GetHdr(this MediaStream videoStream)
        {
            return null;
        }

        /// <summary>
        /// Gets the ISO-8620 representation of a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/>.</param>
        /// <returns>string.</returns>
        public static string ToISO8601(this DateTime dt)
            => dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets the season number of an <see cref="Episode"/>.
        /// </summary>
        /// <param name="episode">The <see cref="Episode"/>.</param>
        /// <returns>int.</returns>
        public static int GetSeasonNumber(this Episode episode)
            => (episode.ParentIndexNumber != 0 ? episode.ParentIndexNumber ?? 1 : episode.ParentIndexNumber).Value;

        /// <summary>
        /// Gets the number of audio channels of a <see cref="MediaStream"/>.
        /// </summary>
        /// <param name="audioStream">The <see cref="MediaStream"/>.</param>
        /// <returns>string.</returns>
        public static string GetAudioChannels(this MediaStream audioStream)
        {
            if (audioStream == null || string.IsNullOrEmpty(audioStream.ChannelLayout))
            {
                return null;
            }

            var channels = audioStream.ChannelLayout.Split('(')[0];
            switch (channels)
            {
                case "7":
                    return "6.1";
                case "6":
                    return "5.1";
                case "5":
                    return "5.0";
                case "4":
                    return "4.0";
                case "3":
                    return "2.1";
                case "stereo":
                    return "2.0";
                case "mono":
                    return "1.0";
                default:
                    return channels;
            }
        }

        /// <summary>
        /// Transforms an enumerable into a list with a speciifc amount of chunks.
        /// </summary>
        /// <param name="enumerable">The IEnumberable{T}.</param>
        /// <param name="chunkSize">Size of the Chunks.</param>
        /// <returns>IList{IEnumerable{T}}.</returns>
        /// <typeparam name="T">The type of IEnumerable.</typeparam>
        public static IList<IEnumerable<T>> ToChunks<T>(this IEnumerable<T> enumerable, int chunkSize)
        {
            var itemsReturned = 0;
            var list = enumerable.ToList(); // Prevent multiple execution of IEnumerable.
            var count = list.Count;
            var chunks = new List<IEnumerable<T>>();
            while (itemsReturned < count)
            {
                chunks.Add(list.Take(chunkSize).ToList());
                list = list.Skip(chunkSize).ToList();
                itemsReturned += chunkSize;
            }

            return chunks;
        }

        /// <summary>
        /// Splits a progress into multiple parts.
        /// </summary>
        /// <param name="parent">The progress.</param>
        /// <param name="parts">The number of parts to split into.</param>
        /// <returns>ISplittableProgress{double}.</returns>
        public static ISplittableProgress<double> Split(this IProgress<double> parent, int parts)
        {
            var current = parent.ToSplittableProgress();
            return current.Split(parts);
        }

        /// <summary>
        /// Converts a progress into a splittable progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <returns>ISplittableProgress{double}.</returns>
        public static ISplittableProgress<double> ToSplittableProgress(this IProgress<double> progress)
        {
            var splittable = new SplittableProgress(progress.Report);
            return splittable;
        }
    }
}
