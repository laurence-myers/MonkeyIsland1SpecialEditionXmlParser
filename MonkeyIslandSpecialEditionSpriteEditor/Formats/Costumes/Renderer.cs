using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes
{
	/// <summary>
	/// One track's resolved sprite for a single step of an animation, positioned relative to
	/// the actor origin in HD pixels.
	/// </summary>
	public class CostumeSpritePlacement(
		int trackIndex,
		SpriteGroup spriteGroup,
		Sprite sprite,
		int spriteIndex,
		RectangleF screenRect,
		bool flipped,
		ClassicCel? classicCel )
	{
		public int TrackIndex
		{
			get;
			set;
		} = trackIndex;

		public SpriteGroup SpriteGroup
		{
			get;
			set;
		} = spriteGroup;

		public Sprite Sprite
		{
			get;
			set;
		} = sprite;

		/// <summary>
		/// Gets or sets the sprite's index within its group's sprite list.
		/// </summary>
		public int SpriteIndex
		{
			get;
			set;
		} = spriteIndex;

		/// <summary>
		/// Gets or sets the draw rectangle relative to the actor origin, in HD pixels, with
		/// any left-facing mirroring already applied.
		/// </summary>
		public RectangleF ScreenRect
		{
			get;
			set;
		} = screenRect;

		/// <summary>
		/// Gets or sets whether the sprite is drawn horizontally mirrored (left facing
		/// animations reuse the right facing sprites).
		/// </summary>
		public bool Flipped
		{
			get;
			set;
		} = flipped;

		/// <summary>
		/// Gets or sets the matching classic costume cel, when classic data is available.
		/// The frame's raw sprite identifier is the classic cel index (a group whose
		/// FirstSpriteIdentifier is non-zero simply has no SE sprites for the leading cels).
		/// The cel is a calibration reference; the engine positions the sprite by its own
		/// ScreenX/ScreenY.
		/// </summary>
		public ClassicCel? ClassicCel
		{
			get;
			set;
		} = classicCel;
	}

	public static class Renderer
	{
		/// <summary>
		/// The scale from classic 320x200 pixels to the Special Edition's HD costume
		/// coordinates. Derived from the retail data: every sprite's MoveX/MoveY is an exact
		/// multiple of these values, and the median ScreenX/RelX ratio across all costumes
		/// matches exactly. Note this differs slightly from the room renderer's 6.0 x 7.2.
		/// </summary>
		public static readonly SizeF DefaultHdScale = new SizeF( 6.00115741f, 7.20138889f );

		/// <summary>
		/// Returns the longest frame list length of the animation - the number of steps a
		/// preview can show.
		/// </summary>
		public static int GetStepCount( Animation animation )
		{
			var count = 0;
			foreach( var track in animation.AnimationFrameList )
			{
				if( track.FrameList != null && track.FrameList.Count > count )
				{
					count = track.FrameList.Count;
				}
			}
			return count;
		}

		/// <summary>
		/// Resolves the sprites every track of an animation shows at the given step, with
		/// screen rectangles relative to the actor origin in HD pixels. A looping track
		/// (playback flag bit 1, like the classic engine's looping limb sequences) wraps when
		/// the step passes its end; a one-shot chore track holds its last frame. Command
		/// frames resolve to no sprite and are skipped.
		/// </summary>
		/// <param name="costume">The costume.</param>
		/// <param name="animation">The animation to resolve.</param>
		/// <param name="step">The zero-based step (frame) of the animation.</param>
		/// <param name="classicCostume">The matching classic costume, or null.</param>
		public static List<CostumeSpritePlacement> ResolveFramePlacements(
			Costume costume,
			Animation animation,
			int step,
			ClassicCostume? classicCostume )
		{
			var placements = new List<CostumeSpritePlacement>();

			// left facing animations reuse the right facing sprites, mirrored around the
			// actor origin (matching the game and the PNG exporter)
			var flipped = animation.Name.EndsWith( "Left" );

			for( var trackIndex = 0; trackIndex < animation.AnimationFrameList.Count; trackIndex++ )
			{
				var track = animation.AnimationFrameList[trackIndex];
				var frameList = track.FrameList;
				if( frameList == null || frameList.Count == 0 )
				{
					continue;
				}

				var loop = ( track.PlaybackFlags & 2 ) != 0;
				var frame = frameList[loop ? step % frameList.Count : Math.Min( step, frameList.Count - 1 )];
				var spriteGroup = costume.SpriteGroupList.FirstOrDefault( sg => sg.Identifier == track.SpriteGroupIdentifier );
				var sprite = spriteGroup?.ResolveSprite( frame );
				if( spriteGroup == null || sprite == null )
				{
					continue;
				}

				var screenRect = flipped
					? new RectangleF( -( sprite.ScreenX + sprite.TextureWidth ), sprite.ScreenY, sprite.TextureWidth, sprite.TextureHeight )
					: new RectangleF( sprite.ScreenX, sprite.ScreenY, sprite.TextureWidth, sprite.TextureHeight );

				var spriteIndex = frame.SpriteIdentifier - spriteGroup.FirstSpriteIdentifier;

				// the frame's raw sprite identifier is the classic cel index: the SE frame
				// sequences mirror the classic animation command sequences verbatim (verified
				// against the retail data, e.g. costume 41 HeadBonkFront limb 5), so a group
				// with a non-zero FirstSpriteIdentifier just lacks sprites for the leading cels
				var classicLimb = classicCostume?.FindLimb( spriteGroup.Identifier );
				var classicCel = FindCel( classicLimb, frame.SpriteIdentifier );

				placements.Add( new CostumeSpritePlacement(
					trackIndex: trackIndex,
					spriteGroup: spriteGroup,
					sprite: sprite,
					spriteIndex: spriteIndex,
					screenRect: screenRect,
					flipped: flipped,
					classicCel: classicCel
				) );
			}

			return placements;
		}

		/// <summary>
		/// The classic cel a raw SE sprite identifier (= classic cel index) refers to, or null
		/// when the limb or cel is not present in the classic data.
		/// </summary>
		public static ClassicCel? FindCel( ClassicLimb? classicLimb, int celIndex )
		{
			return classicLimb != null && celIndex >= 0 && celIndex < classicLimb.CelList.Count
				? classicLimb.CelList[celIndex]
				: null;
		}

		/// <summary>
		/// Finds the animation that best represents the costume standing still and facing
		/// the given direction ("Left", "Right", "Front" or "Back"): the stand animation,
		/// then the init animation, then any direction, then whatever comes first.
		/// </summary>
		public static Animation? FindStandingAnimation( Costume costume, string directionName )
		{
			var nameCandidates = new[]
			{
				"Stand" + directionName,
				"Init" + directionName,
				"StandFront",
				"InitFront",
			};
			foreach( var name in nameCandidates )
			{
				var animation = costume.AnimationList.FirstOrDefault(
					a => string.Equals( a.Name, name, StringComparison.OrdinalIgnoreCase ) );
				if( animation != null && GetStepCount( animation ) > 0 )
				{
					return animation;
				}
			}
			return costume.AnimationList.FirstOrDefault( a => GetStepCount( a ) > 0 );
		}

		/// <summary>
		/// Renders the first step of the costume's standing animation into a bitmap, the way
		/// the room preview shows an idle actor. Sprites draw at their own ScreenX/ScreenY -
		/// the placement the engine honors (per the retail data and the project owner's
		/// direction); the classic cels are only a calibration reference.
		/// </summary>
		/// <param name="costume">The costume to render.</param>
		/// <param name="directionName">The facing direction ("Left", "Right", "Front", "Back").</param>
		/// <param name="textureLoader">Loads a texture by file name; may return null for missing textures.</param>
		/// <param name="classicCostume">The matching classic costume, or null.</param>
		/// <param name="origin">Receives the actor origin (the feet position) within the returned bitmap.</param>
		/// <returns>The composited actor, or null when nothing is drawable.</returns>
		public static Bitmap? RenderStandingActor( Costume costume, string directionName, Func<string?, Image?> textureLoader, ClassicCostume? classicCostume, out PointF origin )
		{
			origin = PointF.Empty;

			var animation = FindStandingAnimation( costume, directionName );
			if( animation == null )
			{
				return null;
			}

			var placements = ResolveFramePlacements( costume, animation, step: 0, classicCostume: classicCostume )
				.Where( p => p.Sprite.TextureNumber >= 0 && p.Sprite.TextureNumber < costume.TextureFileNameList.Count )
				.ToList();
			if( placements.Count == 0 )
			{
				return null;
			}

			var screenRects = placements
				.Select( p => p.ScreenRect )
				.ToList();
			var bounds = screenRects[0];
			foreach( var screenRect in screenRects )
			{
				bounds = RectangleF.Union( bounds, screenRect );
			}
			var width = (int)Math.Ceiling( bounds.Width );
			var height = (int)Math.Ceiling( bounds.Height );
			if( width <= 0 || height <= 0 )
			{
				return null;
			}

			var bitmap = new Bitmap( width, height );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( Color.Transparent );
				for( var index = 0; index < placements.Count; index++ )
				{
					var placement = placements[index];
					var texture = textureLoader( costume.TextureFileNameList[placement.Sprite.TextureNumber].Path );
					if( texture == null )
					{
						continue;
					}

					var destRect = new RectangleF(
						screenRects[index].X - bounds.X,
						screenRects[index].Y - bounds.Y,
						screenRects[index].Width,
						screenRects[index].Height
					);
					var sourceRect = new RectangleF(
						placement.Sprite.TextureX,
						placement.Sprite.TextureY,
						placement.Sprite.TextureWidth,
						placement.Sprite.TextureHeight
					);
					if( placement.Flipped )
					{
						// draw mirrored by swapping the destination corners
						var destPoints = new[]
						{
							new PointF( destRect.Right, destRect.Top ),
							new PointF( destRect.Left, destRect.Top ),
							new PointF( destRect.Right, destRect.Bottom ),
						};
						graphics.DrawImage( texture, destPoints, sourceRect, GraphicsUnit.Pixel );
					}
					else
					{
						graphics.DrawImage( texture, destRect, sourceRect, GraphicsUnit.Pixel );
					}
				}
			}

			origin = new PointF( -bounds.X, -bounds.Y );
			return bitmap;
		}

		/// <summary>
		/// Returns the classic cel's draw rectangle relative to the actor origin, scaled to
		/// HD pixels, with the same mirroring the sprite placement uses.
		/// </summary>
		public static RectangleF GetClassicScreenRect( ClassicCel cel, bool flipped, SizeF hdScale )
		{
			var x = flipped ? -( cel.RelX + cel.Width ) : cel.RelX;
			return new RectangleF(
				x * hdScale.Width,
				cel.RelY * hdScale.Height,
				cel.Width * hdScale.Width,
				cel.Height * hdScale.Height
			);
		}
		public static void Render( Costume? costume, string animationName, string directory, string filePrefix, Padding spritePadding, LPAKFile lpakFile )
		{
			var animation = costume?.AnimationList.FirstOrDefault( a => a.Name == animationName );
			if( animation == null )
			{
				throw new Exception( "Animation not found" );
			}

			var textures = costume?.TextureFileNameList.Select( f => lpakFile.LoadImage( f.Path ) ).ToArray();
			if( textures?.Any( t => t == null ) ?? false )
			{
				throw new Exception( "Unable to load one or more textures" );
			}

			var flip = animationName.EndsWith( "Left" );

			foreach( var animationFrame in animation.AnimationFrameList )
			{
				var spriteGroup = costume?.SpriteGroupList.FirstOrDefault( sg => sg.Identifier == animationFrame.SpriteGroupIdentifier );
				if( spriteGroup == null )
				{
					continue;
				}

				if( animationFrame.FrameList is null)
				{
					continue;
				}
				var sprites = animationFrame.FrameList
					.Select( spriteGroup.ResolveSprite )
					.Where( s => s != null )
					.Cast<Sprite>()
					.ToArray()
					;

				if( sprites.Length == 0 )
				{
					continue;
				}

				var width = sprites.Max( s => s.TextureWidth ) + spritePadding.Horizontal;
				var height = sprites.Sum( s => s.TextureHeight ) + sprites.Length * spritePadding.Vertical;

				var image = new Bitmap( width, height );
				var imageGraphics = Graphics.FromImage( image );
				imageGraphics.Clear( Color.Transparent );

				var y = 0;

				foreach( var sprite in sprites )
				{
					if( sprite.TextureNumber < 0 || textures is null)
					{
						continue;
					}

					var texture = textures[sprite.TextureNumber];
					if( texture is null)
					{
						continue;
					}
					var destPoints
						= flip
						? new[] { new Point( sprite.TextureWidth + spritePadding.Right, y + spritePadding.Top ), new Point( 0 + spritePadding.Left, y + spritePadding.Top ), new Point( sprite.TextureWidth + spritePadding.Right, y + sprite.TextureHeight + spritePadding.Top ) }
						: new[] { new Point( 0 + spritePadding.Left, y + spritePadding.Top ), new Point( sprite.TextureWidth + spritePadding.Right, y + spritePadding.Top ), new Point( 0 + spritePadding.Left, y + sprite.TextureHeight + spritePadding.Top ) }
						;
					var srcRect = new Rectangle(sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight);
					imageGraphics.DrawImage( texture, destPoints, srcRect, GraphicsUnit.Pixel );
					y += srcRect.Height + spritePadding.Vertical;
				}

				if( !Directory.Exists( directory ) )
				{
					Directory.CreateDirectory( directory );
				}

				var currentFileName = Path.Combine(
					directory,
					string.Concat( filePrefix, "-", animation.Name, "-", animationFrame.Index, ".png" )
					);

				image.Save( currentFileName, ImageFormat.Png );
				image.Dispose();
			}
		}
	}
}
