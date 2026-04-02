using System.Text.RegularExpressions;
using MusicApp.Models;

namespace MusicApp.Services;

public class LyricsService : ILyricsService
{
    private static readonly Regex TimestampRegex = new(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,2}))?\]", RegexOptions.Compiled);

    public Task<LyricsDocument> GetLyricsAsync(Track? track)
    {
        if (track == null)
        {
            return Task.FromResult(CreateUnavailableDocument("Start playback to open lyrics."));
        }

        if (string.IsNullOrWhiteSpace(track.Lyrics))
        {
            return Task.FromResult(CreateUnavailableDocument("Lyrics are not embedded for this track yet."));
        }

        var lines = new List<LyricLine>();
        var timed = false;

        foreach (var rawLine in track.Lyrics.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var matches = TimestampRegex.Matches(rawLine);
            if (matches.Count == 0)
            {
                lines.Add(new LyricLine { Text = rawLine.Trim() });
                continue;
            }

            timed = true;
            var text = TimestampRegex.Replace(rawLine, string.Empty).Trim();
            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var hundredths = match.Groups[3].Success ? int.Parse(match.Groups[3].Value.PadRight(2, '0')) : 0;

                lines.Add(new LyricLine
                {
                    Timestamp = new TimeSpan(0, 0, minutes, seconds, hundredths * 10),
                    Text = string.IsNullOrWhiteSpace(text) ? "..." : text
                });
            }
        }

        if (lines.Count == 0)
        {
            return Task.FromResult(CreateUnavailableDocument("Lyrics are unavailable for this track."));
        }

        return Task.FromResult(new LyricsDocument
        {
            IsAvailable = true,
            IsTimed = timed,
            StatusMessage = timed ? "Synced lyrics available." : "Plain lyrics available.",
            PlainText = track.Lyrics,
            Lines = timed
                ? lines.OrderBy(line => line.Timestamp).ToList()
                : lines
        });
    }

    private static LyricsDocument CreateUnavailableDocument(string message)
    {
        return new LyricsDocument
        {
            IsAvailable = false,
            IsTimed = false,
            StatusMessage = message,
            PlainText = string.Empty,
            Lines = new List<LyricLine>()
        };
    }
}
