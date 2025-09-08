using Musaed.Core.Dtos;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Logging;

/// <summary>
/// A custom Serilog sink that sends log events to a backend API one by one.
/// It includes a file-based buffer to ensure log resilience if the API is unavailable.
/// </summary>
public class HttpSink : IBatchedLogEventSink
{
    private readonly string _requestUri;
    private readonly string _source;
    private readonly HttpClient _httpClient;
    private readonly string _bufferFilePath;

    public HttpSink(string requestUri, string source, HttpClient httpClient, string bufferFilePath)
    {
        _requestUri = requestUri;
        _source = source;
        _httpClient = httpClient;
        _bufferFilePath = bufferFilePath;
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        await TrySendBufferedLogsAsync();

        var logEntries = batch.Select(logEvent => new LogEntryDto
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Source = _source,
            Hostname = Environment.MachineName
        }).ToList();

        if (!logEntries.Any())
        {
            return;
        }

        // Send logs one by one as required by the API
        foreach (var logEntry in logEntries)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_requestUri, logEntry);

                if (!response.IsSuccessStatusCode)
                {
                    SelfLog.WriteLine($"Error sending a log to backend. Status:" +
                        $" {response.StatusCode}. Buffering the entire batch.");
                    // If one fails, buffer the whole original batch to preserve order and stop trying.
                    await WriteBatchToBufferAsync(logEntries);
                    return; // Exit the loop
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine($"Exception while sending a log: {ex.Message}." +
                    $" Buffering the entire batch.");
                await WriteBatchToBufferAsync(logEntries);
                return; // Exit the loop
            }
        }
    }

    private async Task WriteBatchToBufferAsync(IEnumerable<LogEntryDto> logEntries)
    {
        try
        {
            var jsonEntries = logEntries.Select(e => JsonSerializer.Serialize(e));
            await File.AppendAllLinesAsync(_bufferFilePath, jsonEntries);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine($"Failed to write log batch to buffer file {_bufferFilePath}: {ex.Message}");
        }
    }

    private async Task TrySendBufferedLogsAsync()
    {
        if (!File.Exists(_bufferFilePath)) return;

        var tempBufferFile = _bufferFilePath + ".tmp";

        try
        {
            // Move the file to a temporary name to prevent locking issues if new logs are buffered
            File.Move(_bufferFilePath, tempBufferFile);
            var bufferedLines = await File.ReadAllLinesAsync(tempBufferFile);

            if (!bufferedLines.Any())
            {
                File.Delete(tempBufferFile);
                return;
            }

            SelfLog.WriteLine($"Attempting to send {bufferedLines.Length} " +
                $"buffered log events.");

            var remainingLines = new List<string>(bufferedLines);

            foreach (var line in bufferedLines)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<LogEntryDto>(line);
                    if (entry == null) continue;

                    var response = await _httpClient.PostAsJsonAsync(_requestUri, entry);

                    if (response.IsSuccessStatusCode)
                    {
                        // If successful, remove the line from our list of remaining lines
                        remainingLines.Remove(line);
                    }
                    else
                    {
                        SelfLog.WriteLine($"Failed to send a buffered log. Status: " +
                            $"{response.StatusCode}." +
                            $" Will retry later.");
                        // Stop trying and write the remaining logs back to the buffer.
                        await File.WriteAllLinesAsync(_bufferFilePath, remainingLines);
                        File.Delete(tempBufferFile);
                        return;
                    }
                }
                catch (JsonException ex)
                {
                    SelfLog.WriteLine($"Skipping invalid buffered log entry. Error:" +
                        $" {ex.Message}, Line: {line}");
                    remainingLines.Remove(line);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine($"An error occurred sending a buffered log: {ex.Message}." +
                        $" Will retry later.");
                    await File.WriteAllLinesAsync(_bufferFilePath, remainingLines);
                    File.Delete(tempBufferFile);
                    return;
                }
            }

            // If we successfully sent all logs
            File.Delete(tempBufferFile);
            SelfLog.WriteLine("Successfully sent all buffered logs.");
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine($"An error occurred while processing the buffer file: " +
                $"{ex.Message}");
            // Try to restore the original buffer file if it was moved
            if (File.Exists(tempBufferFile) && !File.Exists(_bufferFilePath))
            {
                File.Move(tempBufferFile, _bufferFilePath);
            }
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;
}

