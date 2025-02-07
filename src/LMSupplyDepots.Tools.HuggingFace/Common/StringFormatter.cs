using System;
using System.Globalization;

namespace LMSupplyDepots.Tools.HuggingFace.Common;

/// <summary>
/// Provides utility methods for formatting various types of data into human-readable strings.
/// </summary>
public static class StringFormatter
{
    private static readonly string[] BinaryUnits = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
    private const double BinaryUnitSize = 1024;
    private const string UnknownValue = "Unknown";

    /// <summary>
    /// Formats the size in bytes to a human-readable string with appropriate units.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <param name="decimalPlaces">Number of decimal places (default is 0).</param>
    /// <returns>Formatted size string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when bytes is negative.</exception>
    public static string FormatSize(long bytes, int decimalPlaces = 0)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "Size cannot be negative.");

        if (bytes == 0)
            return $"0 {BinaryUnits[0]}";

        var order = Math.Min((int)Math.Log(bytes, BinaryUnitSize), BinaryUnits.Length - 1);
        var size = bytes / Math.Pow(BinaryUnitSize, order);

        return $"{size.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)} {BinaryUnits[order]}";
    }

    /// <summary>
    /// Formats a TimeSpan to a string in the specified format.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format.</param>
    /// <param name="format">The format string (default is "hh:mm:ss").</param>
    /// <returns>Formatted time string.</returns>
    public static string FormatTimeSpan(TimeSpan? timeSpan, string format = @"hh\:mm\:ss")
    {
        if (!timeSpan.HasValue || timeSpan.Value.TotalSeconds < 0)
            return UnknownValue;

        return timeSpan.Value.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats the speed in bytes per second to a human-readable string.
    /// </summary>
    /// <param name="bytesPerSecond">Speed in bytes per second.</param>
    /// <param name="decimalPlaces">Number of decimal places (default is 0).</param>
    /// <returns>Formatted speed string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is negative.</exception>
    public static string FormatSpeed(double bytesPerSecond, int decimalPlaces = 0)
    {
        if (bytesPerSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(bytesPerSecond), "Speed cannot be negative.");

        if (bytesPerSecond == 0)
            return $"0 {BinaryUnits[0]}/s";

        var order = Math.Min((int)Math.Log(bytesPerSecond, BinaryUnitSize), BinaryUnits.Length - 1);
        var speed = bytesPerSecond / Math.Pow(BinaryUnitSize, order);

        return $"{speed.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)} {BinaryUnits[order]}/s";
    }

    /// <summary>
    /// Formats a progress value as a percentage string.
    /// </summary>
    /// <param name="progress">Progress value from 0.0 to 1.0.</param>
    /// <param name="decimalPlaces">Number of decimal places (default is 0).</param>
    /// <returns>Formatted progress string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when progress is not between 0.0 and 1.0.</exception>
    public static string FormatProgress(double? progress, int decimalPlaces = 0)
    {
        if (!progress.HasValue)
            return UnknownValue;

        if (progress.Value < 0 || progress.Value > 1)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0.0 and 1.0.");

        return $"{(progress.Value * 100).ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}%";
    }
}