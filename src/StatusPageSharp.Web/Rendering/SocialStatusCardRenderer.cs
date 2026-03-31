using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Web.Extensions;

namespace StatusPageSharp.Web.Rendering;

public static class SocialStatusCardRenderer
{
    private const int Width = 1200;
    private const int Height = 630;
    private const int MaxRenderedGroups = 3;
    private const float PanelBottom = 588f;
    private const float GroupRowsTop = 338f;
    private const float GroupRowStep = 72f;
    private const float FooterBottomPadding = 12f;
    private const float BadgeHeight = 76f;
    private const float BadgeTextGap = 4f;

    private static readonly Color Background = Color.FromRgb(6, 6, 11);
    private static readonly Color Panel = Color.FromRgba(18, 18, 28, 255);
    private static readonly Color Surface = Color.FromRgba(24, 24, 38, 255);
    private static readonly Color SurfaceMuted = Color.FromRgba(33, 33, 49, 255);
    private static readonly Color Foreground = Color.FromRgb(250, 250, 252);
    private static readonly Color MutedForeground = Color.FromRgb(168, 174, 190);
    private static readonly Color Accent = Color.FromRgba(129, 140, 248, 255);

    public static string BuildVersionToken(PublicSiteSummaryModel siteSummary)
    {
        var monitoredServices = siteSummary.Groups.Sum(group => group.Services.Count);
        var segments = new List<string>
        {
            siteSummary.SiteTitle,
            siteSummary.SiteStatus.ToString(),
            monitoredServices.ToString(CultureInfo.InvariantCulture),
            siteSummary.ActiveIncidents.Count.ToString(CultureInfo.InvariantCulture),
            siteSummary.ActiveMaintenance.Count.ToString(CultureInfo.InvariantCulture),
        };

        foreach (var group in siteSummary.Groups.Take(MaxRenderedGroups))
        {
            segments.Add(group.Name);
            segments.Add(group.Status.ToString());
            segments.Add(group.Services.Count.ToString(CultureInfo.InvariantCulture));

            foreach (var service in group.Services.Take(12))
            {
                segments.Add(service.Slug);
                segments.Add(service.Status.ToString());
            }
        }

        var payload = string.Join('\n', segments);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash[..8]).ToLowerInvariant();
    }

    public static byte[] Render(PublicSiteSummaryModel siteSummary)
    {
        var monitoredServices = siteSummary.Groups.Sum(group => group.Services.Count);
        var renderedGroups = siteSummary.Groups.Take(MaxRenderedGroups).ToArray();

        using var image = new Image<Rgba32>(Width, Height, Background);
        image.Mutate(context =>
        {
            var footer =
                $"{StatusDisplayHelper.ToLabel(siteSummary.SiteStatus)} | Generated {DateTime.UtcNow:MMM d, yyyy HH:mm 'UTC'}";
            var footerFont = CreateFont(18);
            var footerY =
                PanelBottom - FooterBottomPadding - MeasureTextSize(footer, footerFont).Height;

            context.Fill(Accent.WithAlpha(0.2f), new RectangleF(42, 42, 1116, 10));
            context.Fill(Panel, new RectangleF(42, 52, 1116, 536));
            context.Fill(Color.FromRgba(47, 47, 78, 255), new RectangleF(62, 72, 1076, 1));
            context.DrawText(
                Truncate(siteSummary.SiteTitle, 28),
                CreateFont(64, FontStyle.Bold),
                Foreground,
                new PointF(64, 86)
            );
            context.DrawText(
                "Live uptime overview",
                CreateFont(26),
                MutedForeground,
                new PointF(68, 156)
            );

            DrawStatusBadge(context, siteSummary.SiteStatus);
            DrawMetric(
                context,
                "Active incidents",
                siteSummary.ActiveIncidents.Count.ToString(),
                new PointF(64, 232),
                GetMetricColor(siteSummary.ActiveIncidents.Count > 0, siteSummary.SiteStatus),
                336
            );
            DrawMetric(
                context,
                "Maintenance",
                siteSummary.ActiveMaintenance.Count.ToString(),
                new PointF(432, 232),
                Color.FromRgb(245, 158, 11),
                336
            );
            DrawMetric(
                context,
                "Monitored services",
                monitoredServices.ToString(),
                new PointF(800, 232),
                Accent,
                336
            );

            var rowY = GroupRowsTop;
            if (renderedGroups.Length == 0)
            {
                DrawEmptyState(context, rowY);
            }
            else
            {
                foreach (var group in renderedGroups)
                {
                    DrawGroupRow(context, group, rowY);
                    rowY += GroupRowStep;
                }
            }

            context.DrawText(footer, footerFont, MutedForeground, new PointF(64, footerY));
        });

        using var stream = new MemoryStream();
        image.Save(
            stream,
            new PngEncoder
            {
                ColorType = PngColorType.RgbWithAlpha,
                CompressionLevel = PngCompressionLevel.DefaultCompression,
            }
        );

        return stream.ToArray();
    }

    private static void DrawStatusBadge(IImageProcessingContext context, ServiceStatus siteStatus)
    {
        var badgeColor = GetStatusColor(siteStatus);
        var title = StatusDisplayHelper.ToLabel(siteStatus);
        var subtitle = "Current site status";
        var titleFont = CreateFont(28, FontStyle.Bold);
        var subtitleFont = CreateFont(18);
        var titleHeight = MeasureTextSize(title, titleFont).Height;
        var subtitleHeight = MeasureTextSize(subtitle, subtitleFont).Height;
        var contentY =
            88f + Math.Max(8f, (BadgeHeight - titleHeight - BadgeTextGap - subtitleHeight) / 2f);

        context.Fill(badgeColor.WithAlpha(0.18f), new RectangleF(862, 88, 274, BadgeHeight));
        context.Fill(badgeColor, new RectangleF(862, 88, 6, BadgeHeight));
        context.DrawText(title, titleFont, Foreground, new PointF(888, contentY));
        context.DrawText(
            subtitle,
            subtitleFont,
            MutedForeground,
            new PointF(888, contentY + titleHeight + BadgeTextGap)
        );
    }

    private static void DrawMetric(
        IImageProcessingContext context,
        string label,
        string value,
        PointF origin,
        Color accentColor,
        float width
    )
    {
        context.Fill(Surface, new RectangleF(origin.X, origin.Y, width, 92));
        context.Fill(accentColor, new RectangleF(origin.X, origin.Y, 6, 92));
        context.DrawText(
            label,
            CreateFont(18),
            MutedForeground,
            new PointF(origin.X + 24, origin.Y + 18)
        );
        context.DrawText(
            value,
            CreateFont(36, FontStyle.Bold),
            Foreground,
            new PointF(origin.X + 24, origin.Y + 42)
        );
    }

    private static void DrawGroupRow(
        IImageProcessingContext context,
        PublicServiceGroupModel group,
        float y
    )
    {
        var groupColor = GetStatusColor(group.Status);
        var orderedServices = group.Services.Take(12).ToArray();

        context.Fill(SurfaceMuted, new RectangleF(64, y, 1072, 62));
        context.Fill(groupColor, new RectangleF(64, y, 8, 62));
        context.DrawText(
            Truncate(group.Name, 28),
            CreateFont(24, FontStyle.Bold),
            Foreground,
            new PointF(92, y + 14)
        );
        context.DrawText(
            $"{StatusDisplayHelper.ToLabel(group.Status)} | {group.Services.Count} {(group.Services.Count == 1 ? "service" : "services")}",
            CreateFont(17),
            MutedForeground,
            new PointF(92, y + 40)
        );

        var indicatorX = 784f;
        foreach (var service in orderedServices)
        {
            context.Fill(
                GetStatusColor(service.Status),
                new RectangleF(indicatorX, y + 18, 20, 26)
            );
            indicatorX += 26f;
        }
    }

    private static void DrawEmptyState(IImageProcessingContext context, float y)
    {
        context.Fill(SurfaceMuted, new RectangleF(64, y, 1072, 62));
        context.Fill(Accent, new RectangleF(64, y, 8, 62));
        context.DrawText(
            "No monitored services configured yet",
            CreateFont(24, FontStyle.Bold),
            Foreground,
            new PointF(92, y + 14)
        );
        context.DrawText(
            "Create service groups and monitors to populate the live uptime card.",
            CreateFont(17),
            MutedForeground,
            new PointF(92, y + 40)
        );
    }

    private static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        foreach (
            var familyName in new[]
            {
                "Segoe UI",
                "Arial",
                "Helvetica",
                "DejaVu Sans",
                "Liberation Sans",
                "Noto Sans",
            }
        )
        {
            if (SystemFonts.TryGet(familyName, out var family))
            {
                return family.CreateFont(size, style);
            }
        }

        if (SystemFonts.Collection.Families.Any())
        {
            return SystemFonts.Collection.Families.First().CreateFont(size, style);
        }

        throw new InvalidOperationException(
            "No system fonts are available for social card rendering."
        );
    }

    private static FontRectangle MeasureTextSize(string value, Font font) =>
        TextMeasurer.MeasureSize(value, new TextOptions(font));

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..Math.Max(0, maxLength - 3)]}...";

    private static Color GetMetricColor(bool isImpacted, ServiceStatus siteStatus) =>
        isImpacted ? Color.FromRgb(244, 63, 94) : GetStatusColor(siteStatus);

    private static Color GetStatusColor(ServiceStatus status) =>
        status switch
        {
            ServiceStatus.Operational => Color.FromRgb(34, 197, 94),
            ServiceStatus.PartialOutage => Color.FromRgb(245, 158, 11),
            ServiceStatus.MajorOutage => Color.FromRgb(244, 63, 94),
            ServiceStatus.UnderMaintenance => Color.FromRgb(59, 130, 246),
            _ => Color.FromRgb(163, 163, 184),
        };
}
