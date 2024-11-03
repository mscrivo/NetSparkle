﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetSparkle;

internal class HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath) : IDisposable
{
    public delegate void DownloadCompleteHandler();

    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded,
        double? progressPercentage);

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public event ProgressChangedHandler? ProgressChanged;

    public event DownloadCompleteHandler? DownloadComplete;

    public void StartDownload()
    {
        var response = Task.Run(() => _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            .GetAwaiter().GetResult();
        Task.Run(() => DownloadFileFromHttpResponseMessage(response)).GetAwaiter().GetResult();
    }

    public void Cancel()
    {
        _httpClient.CancelPendingRequests();
    }

    private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await ProcessContentStream(totalBytes, contentStream);
    }

    private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
    {
        var totalBytesRead = 0L;
        var readCount = 0L;
        var buffer = new byte[8192];
        var isMoreToRead = true;

        await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write,
            FileShare.None, 8192, true);
        do
        {
            var bytesRead = await contentStream.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                isMoreToRead = false;
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                continue;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

            totalBytesRead += bytesRead;
            readCount += 1;

            if (readCount % 100 == 0)
            {
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }
        } while (isMoreToRead);

        DownloadComplete?.Invoke();
    }

    private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
    {
        double? progressPercentage = null;
        if (totalDownloadSize.HasValue)
        {
            progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
        }

        ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
    }
}
