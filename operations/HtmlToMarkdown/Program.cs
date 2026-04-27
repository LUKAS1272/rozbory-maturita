using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ReverseMarkdown;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Použití: dotnet run <url>");
            return;
        }

        var url = args[0];

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true
        };

        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        request.Headers.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        request.Headers.Add("Accept-Language", "cs-CZ,cs;q=0.9,en;q=0.8");
        request.Headers.Add("Referer", "https://www.google.com/");

        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // odstranění script/style/nav/footer
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//nav|//footer");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                    node.Remove();
            }

            var links = doc.DocumentNode.SelectNodes("//a");
            if (links != null)
            {
                foreach (var link in links)
                {
                    link.Remove();
                }
            }

            // odstranění atributů
            foreach (var node in doc.DocumentNode.Descendants())
            {
                node.Attributes.Remove("class");
                node.Attributes.Remove("style");
                node.Attributes.Remove("href");
            }

            var main = doc.DocumentNode.SelectSingleNode("//main|//article");
            var content = main?.InnerHtml
                          ?? doc.DocumentNode.SelectSingleNode("//body")?.InnerHtml
                          ?? html;

            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Drop
            };

            var converter = new Converter(config);
            var markdown = converter.Convert(content);

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "output";
            var safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

            var fileName = $"{safeTitle}.md";

            await File.WriteAllTextAsync(fileName, markdown);

            Console.WriteLine($"Hotovo: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chyba: " + ex.Message);
        }
    }
}