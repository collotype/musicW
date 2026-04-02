using MusicApp.Models;
using MusicApp.Persistence;

namespace MusicApp.Services;

public class TimedCommentService : ITimedCommentService
{
    private readonly AppDataStore _dataStore;

    public event EventHandler<string>? CommentsChanged;

    public TimedCommentService(AppDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<List<TimedComment>> GetCommentsAsync(string trackId)
    {
        return Task.FromResult(_dataStore.TimedComments
            .Where(comment => string.Equals(comment.TrackId, trackId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(comment => comment.Timestamp)
            .ThenBy(comment => comment.CreatedAt)
            .ToList());
    }

    public async Task AddCommentAsync(string trackId, TimeSpan timestamp, string text, string authorName = "You")
    {
        if (string.IsNullOrWhiteSpace(trackId) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _dataStore.TimedComments.Add(new TimedComment
        {
            TrackId = trackId,
            Timestamp = timestamp,
            Text = text.Trim(),
            AuthorName = string.IsNullOrWhiteSpace(authorName) ? "You" : authorName.Trim()
        });

        await _dataStore.SaveAllAsync();
        CommentsChanged?.Invoke(this, trackId);
    }

    public async Task DeleteCommentAsync(string commentId)
    {
        var comment = _dataStore.TimedComments.FirstOrDefault(item => item.Id == commentId);
        if (comment == null)
        {
            return;
        }

        _dataStore.TimedComments.Remove(comment);
        await _dataStore.SaveAllAsync();
        CommentsChanged?.Invoke(this, comment.TrackId);
    }

    public async Task ToggleFavoriteMomentAsync(string commentId)
    {
        var comment = _dataStore.TimedComments.FirstOrDefault(item => item.Id == commentId);
        if (comment == null)
        {
            return;
        }

        comment.IsFavoriteMoment = !comment.IsFavoriteMoment;
        await _dataStore.SaveAllAsync();
        CommentsChanged?.Invoke(this, comment.TrackId);
    }
}
