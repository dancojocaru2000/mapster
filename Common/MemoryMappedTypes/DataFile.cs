using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes;

/// <summary>
///     Action to be called when iterating over <see cref="MapFeature" /> in a given bounding box via a call to
///     <see cref="DataFile.ForeachFeature" />
/// </summary>
/// <param name="feature">The current <see cref="MapFeature" />.</param>
/// <param name="label">The label of the feature, <see cref="string.Empty" /> if not available.</param>
/// <param name="coordinates">The coordinates of the <see cref="MapFeature" />.</param>
/// <returns></returns>
public delegate bool MapFeatureDelegate(MapFeatureData featureData);

/// <summary>
///     Aggregation of all the data needed to render a map feature
/// </summary>
public readonly ref struct MapFeatureData
{
	public long Id { get; init; }

	public GeometryType Type { get; init; }
	public ReadOnlySpan<char> Label { get; init; }
	public ReadOnlySpan<Coordinate> Coordinates { get; init; }
	public RenderType RenderType { get; init; }
}

/// <summary>
///     Represents a file with map data organized in the following format:<br />
///     <see cref="FileHeader" /><br />
///     Array of <see cref="TileHeaderEntry" /> with <see cref="FileHeader.TileCount" /> records<br />
///     Array of tiles, each tile organized:<br />
///     <see cref="TileBlockHeader" /><br />
///     Array of <see cref="MapFeature" /> with <see cref="TileBlockHeader.FeaturesCount" /> at offset
///     <see cref="TileHeaderEntry.OffsetInBytes" /> + size of <see cref="TileBlockHeader" /> in bytes.<br />
///     Array of <see cref="Coordinate" /> with <see cref="TileBlockHeader.CoordinatesCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
///     Array of <see cref="StringEntry" /> with <see cref="TileBlockHeader.StringCount" /> at offset
///     <see cref="TileBlockHeader.StringsOffsetInBytes" />.<br />
///     Array of <see cref="char" /> with <see cref="TileBlockHeader.CharactersCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
/// </summary>
public unsafe class DataFile : IDisposable
{
	private readonly FileHeader* _fileHeader;
	private readonly MemoryMappedViewAccessor _mma;
	private readonly MemoryMappedFile _mmf;

	private readonly byte* _ptr;
	private readonly int CoordinateSizeInBytes = Marshal.SizeOf<Coordinate>();
	private readonly int FileHeaderSizeInBytes = Marshal.SizeOf<FileHeader>();
	private readonly int MapFeatureSizeInBytes = Marshal.SizeOf<MapFeature>();
	private readonly int StringEntrySizeInBytes = Marshal.SizeOf<StringEntry>();
	private readonly int TileBlockHeaderSizeInBytes = Marshal.SizeOf<TileBlockHeader>();
	private readonly int TileHeaderEntrySizeInBytes = Marshal.SizeOf<TileHeaderEntry>();

	private bool _disposedValue;

	public DataFile(string path)
	{
		_mmf = MemoryMappedFile.CreateFromFile(path);
		_mma = _mmf.CreateViewAccessor();
		_mma.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
		_fileHeader = (FileHeader*)_ptr;
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_mma?.SafeMemoryMappedViewHandle.ReleasePointer();
				_mma?.Dispose();
				_mmf?.Dispose();
			}

			_disposedValue = true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private TileHeaderEntry* GetNthTileHeader(int i)
	{
		return (TileHeaderEntry*)(_ptr + i * TileHeaderEntrySizeInBytes + FileHeaderSizeInBytes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private (TileBlockHeader? Tile, ulong TileOffset) GetTile(int tileId)
	{
		ulong tileOffset = 0;
		for (var i = 0; i < _fileHeader->TileCount; ++i)
		{
			var tileHeaderEntry = GetNthTileHeader(i);
			if (tileHeaderEntry->ID == tileId)
			{
				tileOffset = tileHeaderEntry->OffsetInBytes;
				return (*(TileBlockHeader*)(_ptr + tileOffset), tileOffset);
			}
		}

		return (null, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private MapFeature* GetFeature(int i, ulong offset)
	{
		return (MapFeature*)(_ptr + offset + TileBlockHeaderSizeInBytes + i * MapFeatureSizeInBytes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private ReadOnlySpan<Coordinate> GetCoordinates(ulong coordinateOffset, int ithCoordinate, int coordinateCount)
	{
		return new ReadOnlySpan<Coordinate>(_ptr + coordinateOffset + ithCoordinate * CoordinateSizeInBytes, coordinateCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private void GetString(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> value)
	{
		var stringEntry = (StringEntry*)(_ptr + stringsOffset + i * StringEntrySizeInBytes);
		value = new ReadOnlySpan<char>(_ptr + charsOffset + stringEntry->Offset * 2, stringEntry->Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private void GetProperty(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
	{
		if (i % 2 != 0)
		{
			throw new ArgumentException("Properties are key-value pairs and start at even indices in the string list (i.e. i % 2 == 0)");
		}

		GetString(stringsOffset, charsOffset, i, out key);
		GetString(stringsOffset, charsOffset, i + 1, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static RenderType ClassifyProperties(IDictionary<string, string> properties, GeometryType geometryType) 
	{
		if (properties.Any(p => p.Key == "highway")) 
		{
			return properties.Where(p => p.Key == "highway").First().Value switch
			{
				"motorway"       => RenderType.H__MOTORWAY,
				"trunk"          => RenderType.H__TRUNK,
				"primary"        => RenderType.H__PRIMARY,
				"secondary"      => RenderType.H__SECONDARY,
				"tertiary"       => RenderType.H__TERTIARY,
				"residential"    => RenderType.H__RESIDENTIAL,
				"living_street"  => RenderType.H__RESIDENTIAL,
				"service"        => RenderType.H__SERVICE,
				"track"          => RenderType.H__TRACK,
				_                => RenderType.HIGHWAY,
			};
		} 
		else if (properties.Any(p => p.Key.StartsWith("water")) && geometryType != GeometryType.Point) 
		{
			return RenderType.WATERWAY;
		} 
		else if (properties.Any(p => p.Key == "railway"))
		{
			return properties.Where(p => p.Key == "railway").First().Value switch
			{
				"rail"         => RenderType.R__MAINLINE,
				"subway"       => RenderType.R__SUBWAY,
				"light_rail"   => RenderType.R__LIGHT_RAIL,
				"tram"         => RenderType.R__TRAM,
				"narrow_gauge" => RenderType.R__NARROW_GAUGE,
				"monorail"     => RenderType.R__MONORAIL,
				"preserved"    => RenderType.R__PRESERVED,
				"miniature"    => RenderType.R__MINIATURE,
				"funicular"    => RenderType.R__FUNICULAR,
				_ => RenderType.RAILWAY,
			};
		}
		else if (properties.Any(p => p.Key.StartsWith("boundary") && p.Value.StartsWith("administrative") && properties.Any(p => p.Key.StartsWith("admin_level") && p.Value == "2")))
		{
			return RenderType.BORDER;
		}
		else if (geometryType != GeometryType.Point && properties.Any(p => p.Key.StartsWith("place") && new List<string> { "city", "town", "locality", "hamlet" }.Contains(p.Value)))
		{
			return RenderType.PLACE_NAME;
		}
		else if (properties.Any(p => p.Key.StartsWith("boundary") && p.Value.StartsWith("forest")))
		{
			// TODO: This actually should only show the outer edge of a forest (B_FOREST maybe), not the entire land use, but keep like this for now
			return RenderType.LU__N_FOREST;
		}
		else if (properties.Any(p => p.Key.StartsWith("landuse") && (p.Value.StartsWith("forest") || p.Value.StartsWith("orchard"))))
		{
			return RenderType.LU__N_FOREST;
		}
		else if (properties.Any(p => p.Key.StartsWith("landuse") && new List<string> { "residential", "cemetery", "industrial", "commercial", "square", "construction", "military", "quarry", "brownfield" }.Contains(p.Value)))
		{
			return RenderType.LU_RESIDENTIAL;
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("landuse") && new List<string> { "form", "meadow", "grass", "greenfield", "recreation_ground", "winter_sports", "allotments" }.Contains(p.Value)))
		{
			return RenderType.LU__N_PLAIN;
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("landuse") && new List<string> { "reservoir", "basin" }.Contains(p.Value)))
		{
			return RenderType.LU__N_WATER;
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("building")))
		{
			return RenderType.LU_RESIDENTIAL;
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("amenity")))
		{
			return properties.Where(p => p.Key.StartsWith("amenity")).First().Value switch
			{
				"fountain"   => RenderType.LU_R__FOUNTAIN,
				_            => RenderType.LU_RESIDENTIAL,
			};
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("leisure")))
		{
			return RenderType.LU__LEISURE;
		}
		else if (geometryType == GeometryType.Polygon && properties.Any(p => p.Key.StartsWith("natural")))
		{
			return properties.Where(p => p.Key.StartsWith("natural")).Select(p => p.Value).First() switch
			{
				"fell" or "grassland" or "heath" or "moor" or "scrub" or "wetland" => RenderType.LU__N_PLAIN,
				"wood" or "tree_row" => RenderType.LU__N_FOREST,
				"bare_rock" or "rock" or "scree" => RenderType.LU__N_MOUNTAINS,
				"beach" or "sand" => RenderType.LU__N_DESERT,
				"water" => RenderType.LU__N_WATER,
				_ => RenderType.LU__NATURAL,
			};
		}

		return RenderType.UNKNOWN;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public void ForeachFeature(BoundingBox b, MapFeatureDelegate? action)
	{
		if (action == null)
		{
			return;
		}

		var tiles = TiligSystem.GetTilesForBoundingBox(b.MinLat, b.MinLon, b.MaxLat, b.MaxLon);
		for (var i = 0; i < tiles.Length; ++i)
		{
			var header = GetTile(tiles[i]);
			if (header.Tile == null)
			{
				continue;
			}
			for (var j = 0; j < header.Tile.Value.FeaturesCount; ++j)
			{
				var feature = GetFeature(j, header.TileOffset);
				var coordinates = GetCoordinates(header.Tile.Value.CoordinatesOffsetInBytes, feature->CoordinateOffset, feature->CoordinateCount);
				var isFeatureInBBox = false;

				for (var k = 0; k < coordinates.Length; ++k)
				{
					if (b.Contains(coordinates[k]))
					{
						isFeatureInBBox = true;
						break;
					}
				}

				var label = ReadOnlySpan<char>.Empty;
				if (feature->LabelOffset >= 0)
				{
					GetString(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes, feature->LabelOffset, out label);
				}

				if (isFeatureInBBox)
				{
					var properties = new Dictionary<string, string>(feature->PropertyCount);
					for (var p = 0; p < feature->PropertyCount; ++p)
					{
						GetProperty(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes, p * 2 + feature->PropertiesOffset, out var key, out var value);
						properties.Add(key.ToString(), value.ToString());
					}

					if (!action(new MapFeatureData
						{
							Id = feature->Id,
							Label = properties.Where(p => p.Key == "name").Select(p => p.Value).FirstOrDefault() ?? label,
							Coordinates = coordinates,
							Type = feature->GeometryType,
							RenderType = ClassifyProperties(properties, feature->GeometryType),
						}))
					{
						break;
					}
				}
			}
		}
	}
}
