using System.Net.Sockets;
using System.Text;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.VirusScanning;

/// <summary>Speaks clamd's INSTREAM protocol directly over TCP — no client library needed.
/// Protocol: send "zINSTREAM\0", then repeated [4-byte big-endian length][chunk] frames, then
/// a zero-length frame to signal end-of-stream, then read a line back ("stream: OK",
/// "stream: &lt;name&gt; FOUND", or an error).</summary>
public class ClamAvVirusScanner(IConfiguration configuration, ILogger<ClamAvVirusScanner> logger) : IVirusScanner
{
    private const int ChunkSize = 8192;

    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("ClamAv");
        var host = section["Host"] ?? "localhost";
        var port = section.GetValue<int?>("Port") ?? 3310;

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            using var stream = client.GetStream();

            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);

            var buffer = new byte[ChunkSize];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                var lengthPrefix = BitConverter.GetBytes(read);
                if (BitConverter.IsLittleEndian) Array.Reverse(lengthPrefix);
                await stream.WriteAsync(lengthPrefix, cancellationToken);
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            // Zero-length chunk signals end of stream.
            await stream.WriteAsync(new byte[4], cancellationToken);
            await stream.FlushAsync(cancellationToken);

            using var responseReader = new StreamReader(stream, Encoding.ASCII);
            var response = (await responseReader.ReadLineAsync(cancellationToken))?.TrimEnd('\0') ?? string.Empty;

            if (response.Contains("OK", StringComparison.Ordinal))
                return new ScanResult(ScanOutcome.Clean, null);

            if (response.Contains("FOUND", StringComparison.Ordinal))
                return new ScanResult(ScanOutcome.Infected, response);

            logger.LogWarning("Unexpected ClamAV response: {Response}", response);
            return new ScanResult(ScanOutcome.ScanFailed, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Virus scan failed — could not reach ClamAV at {Host}:{Port}", host, port);
            return new ScanResult(ScanOutcome.ScanFailed, ex.Message);
        }
    }
}
