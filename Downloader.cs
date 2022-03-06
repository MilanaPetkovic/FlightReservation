using PuppeteerSharp;
using Serilog;
using System.Runtime.InteropServices;

namespace FlightReservationConsole
{
    public class Downloader
    {
        private readonly bool _isLinux;
        private readonly bool _isHeadless;

        private string DownloadDirectory { get; set; }

        public Downloader(string downloadDirectory, bool headless = true)
        {
            _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            _isHeadless = headless;
            DownloadDirectory = downloadDirectory;
        }

        public LaunchOptions SetBrowserOptions()
        {
            LaunchOptions options = new()
            {
                Headless = _isHeadless,
                Args = new string[] { "--no-sandbox", "--disable-extensions", "--disable-gpu" },
                DumpIO = true,
            };

            if (!_isLinux)
                options.ExecutablePath = DownloadChromium();


            return options;
        }

        public string DownloadChromium()
        {
            BrowserFetcherOptions browserFetcherOptions = new() { Path = DownloadDirectory };
            BrowserFetcher browserFetcher = new(browserFetcherOptions);
            RevisionInfo revision = browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision).Result;

            if (!revision.Downloaded)
                Log.Warning("Chromium has not downloaded");

            return revision.ExecutablePath;

        }
    }
}
