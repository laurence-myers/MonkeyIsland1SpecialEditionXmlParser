using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	/// <summary>
	/// A room object sprite resolved to its on-screen position.
	/// </summary>
	public class RoomSpritePlacement(
		int groupIndex,
		SpriteHeader spriteHeader,
		Sprite sprite,
		RectangleF screenRect,
		bool hasClassicMatch,
		ClassicObject? classicObject
	)
	{
		/// <summary>
		/// Gets or sets the index of the sprite group within the room.
		/// </summary>
		public int GroupIndex
		{
			get;
			set;
		} = groupIndex;

		/// <summary>
		/// Gets or sets the header of the sprite group (its Identifier is the classic object number).
		/// </summary>
		public SpriteHeader SpriteHeader
		{
			get;
			set;
		} = spriteHeader;

		/// <summary>
		/// Gets or sets the sprite.
		/// </summary>
		public Sprite Sprite
		{
			get;
			set;
		} = sprite;

		/// <summary>
		/// Gets or sets the resolved screen rectangle (HD room pixels).
		/// </summary>
		public RectangleF ScreenRect
		{
			get;
			set;
		} = screenRect;

		/// <summary>
		/// Gets or sets a value indicating whether a classic object supplied the base position.
		/// When false the sprite is placed at its offset alone.
		/// </summary>
		public bool HasClassicMatch
		{
			get;
			set;
		} = hasClassicMatch;

		/// <summary>
		/// Gets or sets the matched classic object, if any.
		/// </summary>
		public ClassicObject? ClassicObject
		{
			get;
			set;
		} = classicObject;

		public override string ToString()
		{
			return string.Concat( "group ", this.GroupIndex, " id=", this.SpriteHeader.Identifier, " sprite ", this.Sprite.Index, " @ ", this.ScreenRect );
		}
	}

	/// <summary>
	/// Renders SE rooms the way the game composites them: the first static sprite layer (the
	/// background) at its absolute positions, then object sprites placed via their classic
	/// SCUMM object position scaled to HD plus the per-sprite offset, then any further static
	/// sprite layers (foreground overlays such as tables and door frames) on top.
	/// </summary>
	public static class Renderer
	{
		/// <summary>
		/// The factor between classic room coordinates (320x200-era pixels) and HD room pixels.
		/// Verified against the retail data: X is 1920/320 = 6.0 and Y is 6.0 * 1.2 = 7.2
		/// (the classic art used non-square pixels; 1.2 is the VGA aspect correction).
		/// </summary>
		public static readonly SizeF DefaultHdScale = new SizeF( 6.0f, 7.2f );

		/// <summary>
		/// Computes the total pixel size of the room background from its static sprites.
		/// </summary>
		public static Size GetBackgroundSize( Room room )
		{
			if( room.StaticSpriteList == null || room.StaticSpriteList.Count == 0 || room.StaticSpriteList.All( ssl => ssl.Count == 0 ) )
			{
				return Size.Empty;
			}

			var width = room.StaticSpriteList.Where( ssl => ssl.Count > 0 ).Max( ssl => ssl.Max( ss => ss.X + ss.Width ) );
			var height = room.StaticSpriteList.Where( ssl => ssl.Count > 0 ).Max( ssl => ssl.Max( ss => ss.Y + ss.Height ) );
			return new Size( width, height );
		}

		/// <summary>
		/// Renders the first static sprite layer of the room into a background bitmap. The game
		/// draws this layer below the object sprites; any further static sprite layers are
		/// foreground overlays (see <see cref="RenderForeground"/>).
		/// </summary>
		/// <param name="room">The room to render.</param>
		/// <param name="textureLoader">Loads a texture by file name; may return null for missing textures.</param>
		/// <returns>The composited background, or null when the room has no static sprites.</returns>
		public static Bitmap? RenderBackground( Room room, Func<string?, Image?> textureLoader )
		{
			return RenderStaticLayers( room, textureLoader, firstLayer: 0, lastLayer: 0 );
		}

		/// <summary>
		/// Renders every static sprite layer after the first into a single foreground bitmap,
		/// sized like the background so the two overlay 1:1. The game draws these layers over
		/// the object sprites: they hold props the actors walk behind (tables, door frames) and
		/// they mask object sprite regions that stick out past the visible scene (e.g. the
		/// kitchen door of room 28, whose sprites keep their classic position while the painted
		/// background moved).
		/// </summary>
		/// <param name="room">The room to render.</param>
		/// <param name="textureLoader">Loads a texture by file name; may return null for missing textures.</param>
		/// <returns>The composited foreground, or null when the room has no foreground layers.</returns>
		public static Bitmap? RenderForeground( Room room, Func<string?, Image?> textureLoader )
		{
			return RenderStaticLayers( room, textureLoader, firstLayer: 1, lastLayer: int.MaxValue );
		}

		private static Bitmap? RenderStaticLayers( Room room, Func<string?, Image?> textureLoader, int firstLayer, int lastLayer )
		{
			var size = GetBackgroundSize( room );
			if( size.IsEmpty || room.StaticSpriteList == null )
			{
				return null;
			}

			var lastIndex = Math.Min( lastLayer, room.StaticSpriteList.Count - 1 );
			if( firstLayer > lastIndex || room.StaticSpriteList.Skip( firstLayer ).Take( lastIndex - firstLayer + 1 ).All( ssl => ssl.Count == 0 ) )
			{
				return null;
			}

			var bitmap = new Bitmap( size.Width, size.Height );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( Color.Transparent );

				for( var layer = firstLayer; layer <= lastIndex; layer++ )
				{
					foreach( var staticSprite in room.StaticSpriteList[layer] )
					{
						var texture = textureLoader( staticSprite.TextureFileName );
						if( texture == null )
						{
							continue;
						}

						var destRect = new RectangleF( staticSprite.X, staticSprite.Y, staticSprite.Width, staticSprite.Height );
						var srcRect = new RectangleF( 0.0f, 0.0f, texture.Width, texture.Height );
						graphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
					}
				}
			}

			return bitmap;
		}

		/// <summary>
		/// Resolves the screen position of every object sprite in the room:
		/// screen = classic object position * hdScale + sprite offset.
		/// Sprites whose group has no matching classic object are placed at their offset alone
		/// and flagged (HasClassicMatch = false) rather than dropped.
		/// </summary>
		/// <param name="room">The room whose sprite groups to resolve.</param>
		/// <param name="classicObjects">The room's classic objects keyed by object number; may be null.</param>
		/// <param name="hdScale">The classic-to-HD scale factor, normally <see cref="DefaultHdScale"/>.</param>
		/// <returns>The placements ordered by layer (stable within a layer).</returns>
		public static List<RoomSpritePlacement> ResolvePlacements( Room room, IReadOnlyDictionary<int, ClassicObject>? classicObjects, SizeF hdScale )
		{
			var placements = new List<RoomSpritePlacement>();
			if( room.SpriteHeaderList == null || room.SpriteGroupList == null )
			{
				return placements;
			}

			var groupCount = Math.Min( room.SpriteHeaderList.Count, room.SpriteGroupList.Count );
			for( var groupIndex = 0; groupIndex < groupCount; groupIndex++ )
			{
				var spriteHeader = room.SpriteHeaderList[groupIndex];
				var spriteGroup = room.SpriteGroupList[groupIndex];
				if( spriteGroup.SpriteList == null )
				{
					continue;
				}

				ClassicObject? classicObject = null;
				if( classicObjects != null )
				{
					ClassicObject found;
					if( classicObjects.TryGetValue( spriteHeader.Identifier, out found ) )
					{
						classicObject = found;
					}
				}

				var baseX = classicObject != null ? classicObject.X * hdScale.Width : 0.0f;
				var baseY = classicObject != null ? classicObject.Y * hdScale.Height : 0.0f;

				foreach( var sprite in spriteGroup.SpriteList )
				{
					var screenRect = new RectangleF(
						baseX + sprite.OffsetX,
						baseY + sprite.OffsetY,
						sprite.TextureWidth,
						sprite.TextureHeight
					);
					placements.Add( new RoomSpritePlacement(
						groupIndex: groupIndex,
						spriteHeader: spriteHeader,
						sprite: sprite,
						screenRect: screenRect,
						hasClassicMatch: classicObject != null,
						classicObject: classicObject
					) );
				}
			}

			return placements.OrderBy( p => p.Sprite.Layer ).ToList();
		}
	}
}
