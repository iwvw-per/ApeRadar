using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApeRadar.Utils
{
    static internal class NetworkUtils
    {
        // The update manifest and statistics calls are small; a short timeout is correct for them.
        private const int RequestTimeoutMilliseconds = 20000;

        // The ship-list package is ~24 KB but the CDN can be very slow from some networks
        // (observed 56-120 s for a single file). The timeout below is therefore an *idle*
        // timeout: it only fires when no bytes arrive for this long, not when the transfer
        // is merely slow. A hard ceiling still applies so a stalled connection cannot hang
        // the updater forever.
        private const int DownloadIdleTimeoutMilliseconds = 30000;
        private const int DownloadMaximumDurationMilliseconds = 10 * 60 * 1000;

        private const int CopyBufferSize = 81920;

        static readonly HttpClient hc = new()
        {
            Timeout = TimeSpan.FromMilliseconds(RequestTimeoutMilliseconds)
        };

        public static void InitializeHttpClient()
        {
            hc.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        public static async Task<string> HttpGet(string url)
        {
            try
            {
                using HttpResponseMessage response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }

        public static async Task<string> HttpPost(string url, string content, string mediaType)
        {
            try
            {
                using HttpResponseMessage response = await hc.PostAsync(url, new StringContent(content, Encoding.UTF8, mediaType));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }

        public static async Task<string> HttpDownloadFile(string url, string filename, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource overallTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            overallTimeout.CancelAfter(DownloadMaximumDurationMilliseconds);

            try
            {
                using HttpResponseMessage response = await hc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, overallTimeout.Token);
                response.EnsureSuccessStatusCode();

                using FileStream fs = new(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                await CopyWithIdleTimeoutAsync(response.Content, fs, overallTimeout.Token);
                return filename;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                DeletePartialFile(filename);
                throw new HttpRequestException("HttpRequestTimeout", ex);
            }
            catch (HttpRequestException ex) when (ex.Message == "HttpRequestTimeout")
            {
                DeletePartialFile(filename);
                throw;
            }
            catch (Exception ex)
            {
                DeletePartialFile(filename);
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }

        /// <summary>
        /// Copies the response body while enforcing an idle timeout: each individual read
        /// must produce data within <see cref="DownloadIdleTimeoutMilliseconds"/>, otherwise
        /// the transfer is treated as stalled and aborted.
        /// </summary>
        private static async Task CopyWithIdleTimeoutAsync(HttpContent content, Stream destination, CancellationToken cancellationToken)
        {
            using Stream source = await content.ReadAsStreamAsync(cancellationToken);
            byte[] buffer = new byte[CopyBufferSize];

            while (true)
            {
                int read;
                using (CancellationTokenSource idleTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    idleTimeout.CancelAfter(DownloadIdleTimeoutMilliseconds);
                    try
                    {
                        read = await source.ReadAsync(buffer.AsMemory(), idleTimeout.Token);
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException("DownloadMadeNoProgress", ex);
                    }
                }

                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await destination.FlushAsync(cancellationToken);
        }

        private static void DeletePartialFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }
            catch
            {
                // A leftover partial file is removed by the next download attempt anyway.
            }
        }
    }
}
