using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms
{
	/// <summary>
	/// A composited room, ready to sit behind a costume in the costume editor: the parts that
	/// draw under the actor, the parts that draw over it, and where the actor's feet land.
	/// </summary>
	public class RoomBackdrop
	{
		/// <summary>
		/// Gets or sets the room imagery drawn under the actor (background, object sprites and -
		/// when the actor stands in front of the props - the static foreground). Null when the
		/// room has no static sprites.
		/// </summary>
		public Bitmap? Below
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the static foreground drawn over the actor (props it stands behind, such
		/// as a shop counter). Null when the actor draws in front of the foreground or the room
		/// has none.
		/// </summary>
		public Bitmap? Above
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the actor's feet position in HD room pixels: where the costume's origin
		/// should be placed over <see cref="Below"/>.
		/// </summary>
		public PointF ActorOriginHd
		{
			get;
			set;
		}
	}

	/// <summary>
	/// Composites a room the way <see cref="Renderer"/> and the room preview do, split into the
	/// layers below and above a costume actor placed at a classic script's actor position. Used
	/// by the costume editor to show a costume against the room it appears in.
	/// </summary>
	public static class BackdropRenderer
	{
		/// <summary>
		/// Renders the room split around an actor placed at <paramref name="placement"/>. The
		/// actor's z-plane (its walkbox mask) decides whether the static foreground draws under
		/// it (mask 0, e.g. the bar's pirate leaders whose hands rest on their table) or over it
		/// (masked box, e.g. the storekeeper behind his counter) - matching the room preview.
		/// </summary>
		/// <param name="room">The SE room supplying the HD imagery.</param>
		/// <param name="classicRoom">The matching classic room (object positions and walkboxes); may be null.</param>
		/// <param name="placement">The actor placement to anchor the backdrop to.</param>
		/// <param name="textureLoader">Loads a texture by file name; may return null for missing textures.</param>
		public static RoomBackdrop Render( Room room, ClassicRoom? classicRoom, ClassicActorPlacement placement, Func<string?, Image?> textureLoader )
		{
			var transform = Renderer.GetHdTransform( room, classicRoom );

			// mask 0 means the actor draws in front of the props, so the foreground belongs
			// under it; a masked box means it draws behind, so the foreground goes on top
			var drawsInFrontOfForeground = ( classicRoom?.GetBoxMaskAt( placement.X, placement.Y ) ?? 0 ) == 0;

			var below = ComposeBelow( room, classicRoom, transform, textureLoader, includeForeground: drawsInFrontOfForeground );
			var above = drawsInFrontOfForeground ? null : Renderer.RenderForeground( room, textureLoader );

			var originX = transform.Origin.X + placement.X * transform.Scale.Width;
			var originY = transform.Origin.Y + ( placement.Y - ( placement.Elevation ?? 0 ) ) * transform.Scale.Height;

			return new RoomBackdrop
			{
				Below = below,
				Above = above,
				ActorOriginHd = new PointF( originX, originY ),
			};
		}

		private static Bitmap? ComposeBelow( Room room, ClassicRoom? classicRoom, RoomHdTransform transform, Func<string?, Image?> textureLoader, bool includeForeground )
		{
			var size = Renderer.GetBackgroundSize( room );
			if( size.IsEmpty )
			{
				return null;
			}

			var bitmap = new Bitmap( size.Width, size.Height );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( Color.Transparent );
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;

				using( var background = Renderer.RenderBackground( room, textureLoader ) )
				{
					DrawLayer( graphics, background );
				}

				// object sprites at their resolved positions, in layer order
				var placements = Renderer.ResolvePlacements( room, classicRoom?.GetObjectsById(), transform );
				foreach( var placement in placements )
				{
					var texture = textureLoader( placement.Sprite.TextureFileName );
					if( texture == null )
					{
						continue;
					}
					var sourceRect = new RectangleF(
						placement.Sprite.TextureX,
						placement.Sprite.TextureY,
						placement.Sprite.TextureWidth,
						placement.Sprite.TextureHeight
					);
					graphics.DrawImage( texture, placement.ScreenRect, sourceRect, GraphicsUnit.Pixel );
				}

				if( includeForeground )
				{
					using( var foreground = Renderer.RenderForeground( room, textureLoader ) )
					{
						DrawLayer( graphics, foreground );
					}
				}
			}

			return bitmap;
		}

		private static void DrawLayer( Graphics graphics, Bitmap? layer )
		{
			if( layer == null )
			{
				return;
			}
			var rect = new RectangleF( 0, 0, layer.Width, layer.Height );
			graphics.DrawImage( layer, rect, rect, GraphicsUnit.Pixel );
		}
	}
}
