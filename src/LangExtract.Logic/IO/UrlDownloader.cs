namespace LangExtract.Logic.IO;

public class UrlDownloader
{
    private readonly HttpClient _httpClient;

    public UrlDownloader(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> DownloadTextFromUrlAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download from {url}: {ex.Message}", ex);
        }
    }
}