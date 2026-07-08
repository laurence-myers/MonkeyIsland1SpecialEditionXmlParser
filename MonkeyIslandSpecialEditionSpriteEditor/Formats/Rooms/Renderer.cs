using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms
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
	/// Maps classic room coordinates to HD room pixels: hd = origin + classic * scale.
	/// The origin is non-zero for fullscreen rooms, whose art carries extra widescreen
	/// scenery left and right of the centered classic 4:3 view.
	/// </summary>
	public class RoomHdTransform(
		SizeF scale,
		PointF origin
	)
	{
		/// <summary>
		/// Gets or sets the classic-to-HD scale factor.
		/// </summary>
		public SizeF Scale
		{
			get;
			set;
		} = scale;

		/// <summary>
		/// Gets or sets the HD position of the classic point (0, 0).
		/// </summary>
		public PointF Origin
		{
			get;
			set;
		} = origin;

		public override string ToString()
		{
			return string.Concat( "scale ", this.Scale, " origin ", this.Origin );
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
		/// Only correct for the usual 144-line rooms; prefer <see cref="GetHdTransform"/>.
		/// </summary>
		public static readonly SizeF DefaultHdScale = new SizeF( 6.0f, 7.2f );

		/// <summary>
		/// The classic art was drawn for non-square VGA pixels 1.2 times taller than wide;
		/// the HD art uses square pixels, so its X scale is the Y scale divided by this.
		/// </summary>
		public const float VgaPixelAspect = 1.2f;

		/// <summary>
		/// Computes the classic-to-HD transform of a room from its own dimensions.
		/// The Y scale is the SE header height over the classic RMHD height: every SE room is
		/// 1037 pixels tall, but the usual game rooms show 144 classic lines (1037/144 = the
		/// familiar 7.2) while fullscreen rooms - the island maps, close-ups, copy protection -
		/// show all 200 lines and scale by only 1037/200 = 5.185. The X scale is the Y scale
		/// divided by the 1.2 VGA pixel aspect (giving 6.0012 and 4.3208 respectively), and the
		/// classic 4:3 view is centered horizontally in the room: scrolling rooms are authored
		/// exactly as wide as the classic art (origin ~0) but fullscreen rooms carry ~268 px of
		/// extra widescreen scenery on each side, which shifts every classic position right.
		/// Verified against the retail art: the map rooms' river sprites, the navigator head's
		/// eyes and the copy protection dial all sit pixel-exact under this transform.
		/// </summary>
		/// <param name="room">The SE room (its header carries the HD pixel size).</param>
		/// <param name="classicRoom">The matching classic room; null falls back to <see cref="DefaultHdScale"/> with no origin shift.</param>
		public static RoomHdTransform GetHdTransform( Room room, ClassicRoom? classicRoom )
		{
			if( classicRoom == null || classicRoom.Width <= 0 || classicRoom.Height <= 0
				|| room.Header.Width <= 0 || room.Header.Height <= 0 )
			{
				return new RoomHdTransform( DefaultHdScale, PointF.Empty );
			}

			var scaleY = (float)room.Header.Height / classicRoom.Height;
			var scaleX = scaleY / VgaPixelAspect;
			var origin = new PointF(
				( room.Header.Width - classicRoom.Width * scaleX ) / 2.0f,
				( room.Header.Height - classicRoom.Height * scaleY ) / 2.0f
			);
			return new RoomHdTransform( new SizeF( scaleX, scaleY ), origin );
		}

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
		/// screen = transform origin + classic object position * scale + sprite offset.
		/// Sprites whose group has no matching classic object are placed at their offset alone
		/// and flagged (HasClassicMatch = false) rather than dropped.
		/// </summary>
		/// <param name="room">The room whose sprite groups to resolve.</param>
		/// <param name="classicObjects">The room's classic objects keyed by object number; may be null.</param>
		/// <param name="hdTransform">The classic-to-HD transform, normally from <see cref="GetHdTransform"/>.</param>
		/// <returns>The placements ordered by layer (stable within a layer).</returns>
		public static List<RoomSpritePlacement> ResolvePlacements( Room room, IReadOnlyDictionary<int, ClassicObject>? classicObjects, RoomHdTransform hdTransform )
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

				var baseX = classicObject != null ? hdTransform.Origin.X + classicObject.X * hdTransform.Scale.Width : 0.0f;
				var baseY = classicObject != null ? hdTransform.Origin.Y + classicObject.Y * hdTransform.Scale.Height : 0.0f;

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
