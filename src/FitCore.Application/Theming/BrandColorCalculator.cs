using System;
using System.Globalization;

namespace FitCore.Application.Theming;

/// <summary>
/// Color math for brand theming (PRS §7). WCAG-style contrast check + derived tints.
/// Pure functions — no WPF dependency.
/// </summary>
public static class BrandColorCalculator
{
    /// <summary>Compute WCAG relative luminance for a hex color. Returns 0..1.</summary>
    public static double RelativeLuminance(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        var rl = ChannelLuminance(r);
        var gl = ChannelLuminance(g);
        var bl = ChannelLuminance(b);
        return 0.2126 * rl + 0.7152 * gl + 0.0722 * bl;

        static double ChannelLuminance(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>WCAG contrast ratio between two hex colors (1..21).</summary>
    public static double ContrastRatio(string hexA, string hexB)
    {
        var la = RelativeLuminance(hexA);
        var lb = RelativeLuminance(hexB);
        var lighter = Math.Max(la, lb);
        var darker = Math.Min(la, lb);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Pick the WCAG-readable foreground for a colored brand surface (button, badge).
    /// Rule: for saturated/dark brand colors, white is the brand-safe choice.
    /// Only fall back to dark text if the brand color is genuinely light (luminance > 0.5).
    /// </summary>
    public static string PickReadableForeground(string backgroundHex)
    {
        const string dark = "#141210";
        const string light = "#FFFFFF";

        // If the brand color is light enough that dark text wins by a wide margin, use dark.
        // Otherwise, white — the standard for colored buttons.
        var luminance = RelativeLuminance(backgroundHex);
        if (luminance > 0.5)
            return dark;

        return light;
    }

    /// <summary>Returns true if the pair passes WCAG AA (4.5:1) for normal text.</summary>
    public static bool PassesWcagAa(string backgroundHex, string foregroundHex)
        => ContrastRatio(backgroundHex, foregroundHex) >= 4.5;

    /// <summary>Darken a hex color by a ratio (0..1). For hover states.</summary>
    public static string Darken(string hex, double ratio)
    {
        var (r, g, b) = ParseHex(hex);
        return ToHex((byte)(r * (1 - ratio)), (byte)(g * (1 - ratio)), (byte)(b * (1 - ratio)));
    }

    /// <summary>Validate hex format (#RRGGBB).</summary>
    public static bool IsValidHex(string? hex)
    {
        if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length != 7) return false;
        for (var i = 1; i < 7; i++)
            if (!Uri.IsHexDigit(hex[i])) return false;
        return true;
    }

    private static (byte r, byte g, byte b) ParseHex(string hex)
    {
        if (hex.Length == 7 && hex[0] == '#')
            return (
                byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber),
                byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber),
                byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber));
        throw new ArgumentException($"Invalid hex color: {hex}", nameof(hex));
    }

    private static string ToHex(byte r, byte g, byte b) => $"#{r:X2}{g:X2}{b:X2}";
}


