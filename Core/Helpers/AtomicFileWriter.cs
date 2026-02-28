using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Writes JSON to a file atomically using a temp-file + rename
/// strategy that prevents data corruption on crash or power loss.
///
/// <para><b>Algorithm:</b></para>
/// <list type="number">
///   <item>Ensures the target directory exists.</item>
///   <item>Serializes the payload to a <c>.tmp</c> sibling file.</item>
///   <item>Renames the temp file over the target with
///         <c>overwrite: true</c> — this is an atomic filesystem
///         operation on NTFS.</item>
/// </list>
///
/// <para><b>Error handling:</b> All I/O exceptions are caught and
/// logged at <see cref="LogLevel.Warning"/> — persistence failures
/// are non-fatal in this application (the data is best-effort
/// telemetry / user preferences).</para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// AtomicFileWriter.Write(filePath, dto, jsonOptions, logger, "onboarding journey");
/// </code>
/// </summary>
internal static class AtomicFileWriter
{
    /// <summary>
    /// Serializes <paramref name="value"/> to JSON and writes it to
    /// <paramref name="filePath"/> atomically.
    /// </summary>
    /// <param name="filePath">Absolute path to the target file.</param>
    /// <param name="value">Object to serialize.</param>
    /// <param name="options">JSON serializer options (e.g. indented).</param>
    /// <param name="logger">Logger for diagnostic and warning output.</param>
    /// <param name="label">
    /// Human-readable label for log messages, e.g. <c>"dismissed tips"</c>,
    /// <c>"onboarding journey"</c>, <c>"interaction counters"</c>.
    /// </param>
    public static void Write<T>(
        string filePath,
        T value,
        JsonSerializerOptions options,
        ILogger logger,
        string label)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);

            var tempPath = filePath + ".tmp";
            var json = JsonSerializer.Serialize(value, options);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);

            logger.LogDebug("Flushed {Label} to {Path}", label, filePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to persist {Label} to {Path}", label, filePath);
        }
    }
}
