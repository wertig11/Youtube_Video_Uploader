using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

public interface IAppStateService
{
    AppState LoadState();
    void SaveState(AppState state);
}
