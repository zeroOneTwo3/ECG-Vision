using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces;

using SkiaSharp;

namespace EcgVision.Infrastructure.Graphics;

public class EcgImageGenerator : IEcgImageGenerator
{
    public byte[] GenerateLeadsPlot(EcgLeadsDto ecgLeads, JobType jobType)
    {
        bool isScreening = jobType == JobType.Extract4Leads;
        int width = isScreening ? 2000 : 6000;
        int height = isScreening ? 1200 : 3600;

        var leadData = isScreening ? ecgLeads.FourLeads.ToArray() : ecgLeads.AllLeads.ToArray();
        int leadCount = leadData.Length;
        int leadHeight = height / leadCount;

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var processedLeads = new (string Label, SKPath Path)[leadCount];
        // We create the paths in parallel, then draw them to the canvas sequentially.
        Parallel.For(0, leadCount, i =>
        {
            var (label, data) = leadData[i];
            int yOffset = i * leadHeight;
            processedLeads[i] = (label, CreateLeadPath(data, width, leadHeight, yOffset));
        });

        // Sequential Drawing (Canvas is not thread-safe)
        using var signalPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var textPaint = new SKPaint { Color = SKColors.DarkBlue, IsAntialias = true };
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 36f);

        for (int i = 0; i < leadCount; i++)
        {
            var (label, path) = processedLeads[i];
            int yOffset = i * leadHeight;

            canvas.DrawText(label, 20, yOffset + 50, font, textPaint);
            canvas.DrawPath(path, signalPaint);

            path.Dispose(); // Clean up native memory
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var dataEncoded = image.Encode(SKEncodedImageFormat.Png, 90); // 90 is often indistinguishable from 100 but smaller
        return dataEncoded.ToArray();
    }

    private static SKPath CreateLeadPath(double[] data, int w, int h, int yOffset)
    {
        var path = new SKPath();
        if (data.Length == 0) return path;

        float centerY = yOffset + (h / 2f);
        path.MoveTo(0, centerY);

        for (int x = 0; x < data.Length; x++)
        {
            float xPos = (float)x / data.Length * w;
            // Scaled for typical ECG millivolt range
            float yPos = centerY - (float)(data[x] * 150);
            path.LineTo(xPos, yPos);
        }
        return path;
    }
}