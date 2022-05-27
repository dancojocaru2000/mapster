using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct FileHeader
{
    [FieldOffset(0)] public long Version;
    [FieldOffset(8)] public int TileCount;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TileHeaderEntry
{
    [FieldOffset(0)] public int ID;
    [FieldOffset(4)] public ulong OffsetInBytes;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TileBlockHeader
{
    /// <summary>
    ///     Number of renderable features in the tile.
    /// </summary>
    [FieldOffset(0)] public int FeaturesCount;

    /// <summary>
    ///     Number of coordinates used for the features in the tile.
    /// </summary>
    [FieldOffset(4)] public int CoordinatesCount;

    /// <summary>
    ///     Number of strings used for the features in the tile.
    /// </summary>
    [FieldOffset(8)] public int StringCount;

    /// <summary>
    ///     Number of characters used by the strings in the tile.
    /// </summary>
    [FieldOffset(12)] public int CharactersCount;

    [FieldOffset(16)] public ulong CoordinatesOffsetInBytes;
    [FieldOffset(24)] public ulong StringsOffsetInBytes;
    [FieldOffset(32)] public ulong CharactersOffsetInBytes;
}

/// <summary>
///     References a string in a large character array.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StringEntry
{
    [FieldOffset(0)] public int Offset;
    [FieldOffset(4)] public int Length;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Coordinate
{
    [FieldOffset(0)] public double Latitude;
    [FieldOffset(8)] public double Longitude;

    public Coordinate()
    {
        Latitude = 0;
        Longitude = 0;
    }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public bool Equals(Coordinate other)
    {
        return Math.Abs(Latitude - other.Latitude) < double.Epsilon &&
               Math.Abs(Longitude - other.Longitude) < double.Epsilon;
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    public static bool operator ==(Coordinate self, Coordinate other)
    {
        return self.Equals(other);
    }

    public static bool operator !=(Coordinate self, Coordinate other)
    {
        return !(self == other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude);
    }
}

public enum GeometryType : byte
{
    Polyline,
    Polygon,
    Point
}

/// <summary>
/// The type of a shape that's rendered
///
/// Multiples of 1000 are categories. 
///
/// The number of underscores (_) determines the hierarchy:
/// General_Category_Subcategory_Feature
/// </summary>
public enum RenderType : int
{
    UNKNOWN          =     0,
    WATERWAY         =     1,
    PLACE_NAME       =     2,

    HIGHWAY          =  1000, // https://wiki.openstreetmap.org/wiki/Highways
    H__MOTORWAY      =  1080,
    H__TRUNK         =  1010,
    H__PRIMARY       =  1020,
    H__SECONDARY     =  1030,
    H__TERTIARY      =  1040,
    H__RESIDENTIAL   =  1050,
    H__SERVICE       =  1060,
    H__TRACK         =  1070,

    RAILWAY          =  2000, // https://wiki.openstreetmap.org/wiki/Railways
    R__MAINLINE      =  2010,
    R__SUBWAY        =  2020,
    R__LIGHT_RAIL    =  2030,
    R__TRAM          =  2040,
    R__NARROW_GAUGE  =  2050,
    R__MONORAIL      =  2060,
    R__PRESERVED     =  2070,
    R__MINIATURE     =  2080,
    R__FUNICULAR     =  2090,

    BORDER           =  3000,

    BUILDING         =  4000,

    LANDUSE          =  5000,
    LU__NATURAL      =  5110,
    LU__N_FOREST     =  5111,
    LU__N_PLAIN      =  5112,
    LU__N_HILLS      =  5113,
    LU__N_MOUNTAINS  =  5114,
    LU__N_DESERT     =  5115,
    LU__N_WATER      =  5116,
    LU__LEISURE      =  5120,
    LU_RESIDENTIAL   =  5200,
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PropertyEntryList
{
    [FieldOffset(0)] public int Count;
    [FieldOffset(4)] public ulong OffsetInBytes;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct MapFeature
{
    // https://wiki.openstreetmap.org/wiki/Key:highway
    public static string[] HighwayTypes =
    {
        "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "road"
    };

    [FieldOffset(0)] public long Id;
    [FieldOffset(8)] public int LabelOffset;
    [FieldOffset(12)] public GeometryType GeometryType;
    [FieldOffset(13)] public int CoordinateOffset;
    [FieldOffset(17)] public int CoordinateCount;
    [FieldOffset(21)] public int PropertiesOffset;
    [FieldOffset(25)] public int PropertyCount;
}
