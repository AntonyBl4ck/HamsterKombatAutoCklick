using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Text.Json;
using System.IO;
using System.IO.Compression;

class Program
{
    static async Task Main()
    {
        // Set console output encoding to UTF-8
        Console.OutputEncoding = Encoding.UTF8;

        var config = LoadConfig("config.json");
        int requestCount = 0;
        var httpClient = new HttpClient();

        while (true)
        {
            await SendRequest(httpClient, config);
            requestCount++;
            Console.WriteLine($"Total requests sent: {requestCount}");

            int delay = new Random().Next(config.MinDelay, config.MaxDelay + 1);
            for (int remaining = delay; remaining > 0; remaining--)
            {
                TimeSpan time = TimeSpan.FromSeconds(remaining);
                Console.Write($"\rTime until next request: {time:mm\\:ss}");
                Thread.Sleep(1000);
            }
            Console.WriteLine();
        }
    }

    static async Task<string> GetDecompressedResponseContent(HttpResponseMessage response)
    {
        var contentEncoding = response.Content.Headers.ContentEncoding.FirstOrDefault();
        if (contentEncoding == "br")
        {
            using (var compressedStream = await response.Content.ReadAsStreamAsync())
            using (var decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(decompressionStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
        else
        {
            return await response.Content.ReadAsStringAsync();
        }
    }
    static async Task SendRequest(HttpClient httpClient, Config config)
    {
        var url = "https://api.hamsterkombatgame.io/clicker/tap";
        var userAgent = GenerateRandomUserAgent();

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers =
        {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip, deflate, br" },
            { "Accept-Language", "ru-UA,ru;q=0.9,uk-UA;q=0.8,uk;q=0.7,ru-RU;q=0.6,en-US;q=0.5,en;q=0.4" },
            { "Authorization", $"Bearer {config.AuthorizationToken}" },
            { "Origin", "https://hamsterkombatgame.io" },
            { "Priority", "u=1, i" },
            { "Referer", "https://hamsterkombatgame.io/" },
            { "Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Google Chrome\";v=\"126\"" },
            { "Sec-Ch-Ua-Mobile", "?1" },
            { "Sec-Ch-Ua-Platform", "\"iOS\"" },
            { "Sec-Fetch-Dest", "empty" },
            { "Sec-Fetch-Mode", "cors" },
            { "Sec-Fetch-Site", "same-site" },
            { "User-Agent", userAgent }
        },
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    count = config.Count,
                    availableTaps = 0,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }),
                Encoding.UTF8,
                "application/json"
            )
        };

        var response = await httpClient.SendAsync(request);
        var responseBody = await GetDecompressedResponseContent(response);
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Response Body: {responseBody}");
    }


    static string GenerateRandomUserAgent()
    {
        var random = new Random();
        var userAgents = new[]
        {
            "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Linux; Android 11; Pixel 5 Build/RQ3A.210605.001) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Mobile Safari/537.36",
            "Mozilla/5.0 (Android 10; Mobile; rv:89.0) Gecko/89.0 Firefox/89.0",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7 like Mac OS X) AppleWebKit/537.36 (KHTML, like Gecko) Version/14.7 Mobile/15E148 Safari/604.1",
            "Mozilla/5.0 (Linux; Android 10; SM-G980F Build/QP1A.190711.020) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/13.0 Chrome/83.0.4103.106 Mobile Safari/537.36"
        };
        return userAgents[random.Next(userAgents.Length)];
    }

    static Config LoadConfig(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Config>(json);
    }
}

class Config
{
    public int Count { get; set; }
    public string AuthorizationToken { get; set; }
    public int MinDelay { get; set; }
    public int MaxDelay { get; set; }
}
