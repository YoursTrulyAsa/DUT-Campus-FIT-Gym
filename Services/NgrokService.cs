using System.Text.Json;

namespace DUT_Campus_FIT_Gym.Services
{
    public class NgrokService
    {
        private readonly HttpClient _httpClient;

        public NgrokService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetPublicUrlAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    "http://127.0.0.1:4040/api/tunnels");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty(
                    "tunnels",
                    out var tunnels))
                {
                    return null;
                }

                foreach (var tunnel in tunnels.EnumerateArray())
                {
                    if (!tunnel.TryGetProperty(
                        "public_url",
                        out var publicUrl))
                    {
                        continue;
                    }

                    var url = publicUrl.GetString();

                    if (!string.IsNullOrWhiteSpace(url) &&
                        url.StartsWith(
                            "https://",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return url.TrimEnd('/');
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}