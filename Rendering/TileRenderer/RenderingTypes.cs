using Mapster.Common.MemoryMappedTypes;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public struct GeoFeature : BaseShape
{
    public enum GeoFeatureType
    {
        Plain,
        Hills,
        Mountains,
        Forest,
        Desert,
        Unknown,
        Water,
        Residential,
        Leisure
    }

    public int ZIndex
    {
        get
        {
            switch (Type)
            {
                case GeoFeatureType.Plain:
                    return 10;
                case GeoFeatureType.Hills:
                    return 12;
                case GeoFeatureType.Mountains:
                    return 13;
                case GeoFeatureType.Forest:
                    return 11;
                case GeoFeatureType.Desert:
                    return 9;
                case GeoFeatureType.Unknown:
                    return 8;
                case GeoFeatureType.Water:
                    return 40;
                case GeoFeatureType.Residential:
                case GeoFeatureType.Leisure:
                    return 41;
            }

            return 7;
        }
        set { }
    }

    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }
    public GeoFeatureType Type { get; set; }

    public void Render(IImageProcessingContext context)
    {
        var color = Color.Magenta;
        switch (Type)
        {
            case GeoFeatureType.Plain:
                color = Color.LightGreen;
                break;
            case GeoFeatureType.Hills:
                color = Color.DarkGreen;
                break;
            case GeoFeatureType.Mountains:
                color = Color.LightGray;
                break;
            case GeoFeatureType.Forest:
                color = Color.Green;
                break;
            case GeoFeatureType.Desert:
                color = Color.SandyBrown;
                break;
            case GeoFeatureType.Unknown:
                color = Color.Magenta;
                break;
            case GeoFeatureType.Water:
                color = Color.LightBlue;
                break;
            case GeoFeatureType.Residential:
                color = Color.LightCoral;
                break;
            case GeoFeatureType.Leisure:
                color = Color.LightGreen;
                break;
        }

		if (!IsPolygon)
        {
            var pen = new Pen(color, 1.2f);
            context.DrawLines(pen, ScreenCoordinates);
        }
        else if (Type == GeoFeatureType.Leisure)
        {
            context.DrawPolygon(new Pen(color, 1.2f), ScreenCoordinates);
            context.FillPolygon(color.WithAlpha(0.2f), ScreenCoordinates);
        }
        else
        {
            // TODO: Explore .WithAlpha
            context.FillPolygon(color, ScreenCoordinates);
        }
    }

    public GeoFeature(ReadOnlySpan<Coordinate> c, GeoFeatureType type)
    {
        IsPolygon = true;
        Type = type;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));
    }
}

public struct Railway : BaseShape
{
    public int ZIndex { get; set; } = 45;
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }

    public void Render(IImageProcessingContext context)
    {
        var penA = new Pen(Color.DarkGray, 2.0f);
        var penB = new Pen(Color.LightGray, 1.2f, new[]
        {
            2.0f, 4.0f, 2.0f
        });
        context.DrawLines(penA, ScreenCoordinates);
        context.DrawLines(penB, ScreenCoordinates);
    }

    public Railway(ReadOnlySpan<Coordinate> c)
    {
        IsPolygon = false;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));
    }
}

public struct PopulatedPlace : BaseShape
{
    public int ZIndex { get; set; } = 60;
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }
    public string Name { get; set; }
    public bool ShouldRender { get; set; }

    public void Render(IImageProcessingContext context)
    {
        if (!ShouldRender)
        {
            return;
        }
        var font = SystemFonts.Families.First().CreateFont(12, FontStyle.Bold);
        context.DrawText(Name, font, Color.Black, ScreenCoordinates[0]);
    }

    public PopulatedPlace(ReadOnlySpan<Coordinate> c, MapFeatureData feature)
    {
        IsPolygon = false;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));

        if (feature.Label.IsEmpty)
        {
            ShouldRender = false;
            Name = "Unknown";
        }
        else
        {
            Name = feature.Label.ToString();
            ShouldRender = true;
        }
    }
}

public struct Border : BaseShape
{
    public int ZIndex { get; set; } = 30;
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }

    public void Render(IImageProcessingContext context)
    {
        var pen = new Pen(Color.Gray, 2.0f);
        context.DrawLines(pen, ScreenCoordinates);
    }

    public Border(ReadOnlySpan<Coordinate> c)
    {
        IsPolygon = false;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));
    }
}

public struct Waterway : BaseShape
{
    public int ZIndex { get; set; } = 40;
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }

    public void Render(IImageProcessingContext context)
    {
        if (!IsPolygon)
        {
            var pen = new Pen(Color.LightBlue, 1.2f);
            context.DrawLines(pen, ScreenCoordinates);
        }
        else
        {
            context.FillPolygon(Color.LightBlue, ScreenCoordinates);
        }
    }

    public Waterway(ReadOnlySpan<Coordinate> c, bool isPolygon = false)
    {
        IsPolygon = isPolygon;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));
    }
}

public enum RoadType {
    Unknown,
    Motorway,
    Trunk,
    Primary,
    Secondary,
    Tertiary,
    Residential,
    Track,
}

public struct Road : BaseShape
{
    public int ZIndex { get; set; } = 50;
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }
    public RoadType Type { get; set; }

	public void Render(IImageProcessingContext context)
    {
        if (!IsPolygon)
        {
            Pen fgPen;
            Pen bgPen;
            switch (Type) {
                case RoadType.Motorway: {
                    fgPen = new Pen(Color.DarkRed, 2.0f);                                         
                    bgPen = new Pen(Color.Yellow, 2.2f);
                    break;
                }
                case RoadType.Trunk: {
                    fgPen = new Pen(Color.Red, 1.8f);                                         
                    bgPen = new Pen(Color.Yellow, 2.0f);
                    break;
                }
                case RoadType.Primary: {
                    fgPen = new Pen(Color.Orange, 1.8f);                                         
                    bgPen = new Pen(Color.Yellow, 2.0f);
                    break;
                }
                case RoadType.Secondary: {
                    fgPen = new Pen(Color.Orange, 1.6f);                                         
                    bgPen = new Pen(Color.Yellow, 1.8f);
                    break;
                }
                case RoadType.Tertiary: {
                    fgPen = new Pen(Color.Yellow, 1.6f);                                         
                    bgPen = new Pen(Color.Yellow, 1.8f);
                    break;
                }
                case RoadType.Residential: {
                    fgPen = new Pen(Color.White, 1.6f);                                         
                    bgPen = new Pen(Color.DarkGray, 1.8f);
                    break;
                }
                case RoadType.Track: {
                    fgPen = new Pen(Color.RosyBrown, 1.4f);                                         
                    bgPen = new Pen(Color.Brown, 1.5f);
                    break;
                }
                default: {
                    fgPen = new Pen(Color.Coral, 0.2f);                                         
                    bgPen = new Pen(Color.Yellow, 0.4f);
                    break;
                }
            }    
            context.DrawLines(bgPen, ScreenCoordinates);
            context.DrawLines(fgPen, ScreenCoordinates);
        }
    }

    public Road(ReadOnlySpan<Coordinate> c, RoadType type = RoadType.Unknown, bool isPolygon = false)
    {
		Type = type;
		IsPolygon = isPolygon;
        ScreenCoordinates = new PointF[c.Length];
        for (var i = 0; i < c.Length; i++)
            ScreenCoordinates[i] = new PointF((float)MercatorProjection.lonToX(c[i].Longitude),
                (float)MercatorProjection.latToY(c[i].Latitude));
    }
}

public interface BaseShape
{
    public int ZIndex { get; set; }
    public bool IsPolygon { get; set; }
    public PointF[] ScreenCoordinates { get; set; }

    public void Render(IImageProcessingContext context);

    public void TranslateAndScale(float minX, float minY, float scale, float height)
    {
        for (var i = 0; i < ScreenCoordinates.Length; i++)
        {
            var coord = ScreenCoordinates[i];
            var newCoord = new PointF((coord.X + minX * -1) * scale, height - (coord.Y + minY * -1) * scale);
            ScreenCoordinates[i] = newCoord;
        }
    }
}
