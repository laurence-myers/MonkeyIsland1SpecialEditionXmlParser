using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;
using CostumeRenderer = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Renderer;

namespace Tests
{
	/// <summary>
	/// The SE animation frame sequences mirror the classic animation command sequences
	/// verbatim (verified against the retail data, e.g. costume 41 HeadBonkFront limb 5), so
	/// a frame's raw sprite identifier is the classic cel index, and one-shot tracks hold
	/// their last frame like non-looping classic sequences.
	/// </summary>
	[TestFixture]
	public class CostumeFrameResolutionTests
	{
		private static Sprite BuildSprite( float screenX, float screenY, int width, int height )
		{
			return new Sprite(
				textureNumber: 0,
				textureX: 0,
				textureY: 0,
				textureWidth: width,
				textureHeight: height,
				screenX: screenX,
				screenY: screenY,
				moveX: 0,
				moveY: 0,
				pathPointIndex: -1
			);
		}

		private static Costume BuildCostume( SpriteGroup spriteGroup, Animation animation )
		{
			return new Costume(
				header: null!,
				textureHeaderList: new List<TextureHeader>(),
				animationHeaderList: new List<AnimationHeader>(),
				pathPointList: new List<PathPoint>(),
				spriteGroupHeaderList: new List<SpriteGroupHeader>(),
				textureFileNameList: new List<TextureFileName>(),
				animationList: new List<Animation> { animation },
				spriteGroupList: new List<SpriteGroup> { spriteGroup }
			);
		}

		private static Animation BuildAnimation( string name, int limbNumber, int playbackFlags, params int[] spriteIdentifiers )
		{
			var track = new AnimationFrame(
				index: 0,
				spriteGroupIdentifier: limbNumber,
				playbackFlags: playbackFlags,
				frameCount: spriteIdentifiers.Length,
				frameAddress: 0
			)
			{
				FrameList = spriteIdentifiers
					.Select( id => new Frame( spriteIdentifier: id, command: 0, soundName: null ) )
					.ToList(),
			};
			return new Animation( name, new List<AnimationFrame> { track } );
		}

		private static ClassicCostume BuildClassicCostume( ClassicLimb limb )
		{
			return new ClassicCostume(
				costumeId: 1,
				roomNumber: 1,
				maximumAnimationNumber: 0,
				format: 0x58,
				mirror: false,
				limbList: new List<ClassicLimb> { limb },
				animationList: new List<ClassicAnimation>()
			);
		}

		[Test]
		public void ResolveFramePlacements_MapsTheRawSpriteIdentifierToTheClassicCel()
		{
			// the SE group carries no sprites for the limb's first cel: sprite 0 is cel 1
			var group = new SpriteGroup(
				spriteList: new List<Sprite> { BuildSprite( 0, 0, 10, 10 ), BuildSprite( 0, 0, 12, 12 ) },
				identifier: 5,
				index: 0,
				firstSpriteIdentifier: 1
			);
			var limb = new ClassicLimb( 5, new List<ClassicCel?>
			{
				new ClassicCel( 0, 4, 4, 0, -4, 0, 0 ),
				new ClassicCel( 1, 5, 5, 0, -5, 0, 0 ),
				new ClassicCel( 2, 6, 6, 0, -6, 0, 0 ),
			} );
			var animation = BuildAnimation( "ChoreFront", limbNumber: 5, playbackFlags: 1, 1, 2 );
			var costume = BuildCostume( group, animation );

			var step0 = CostumeRenderer.ResolveFramePlacements( costume, animation, step: 0, BuildClassicCostume( limb ) ).Single();
			var step1 = CostumeRenderer.ResolveFramePlacements( costume, animation, step: 1, BuildClassicCostume( limb ) ).Single();

			Assert.That( step0.Sprite, Is.SameAs( group.SpriteList[0] ) );
			Assert.That( step0.ClassicCel!.Index, Is.EqualTo( 1 ), "raw identifier 1 is classic cel 1, not cel 0" );
			Assert.That( step1.Sprite, Is.SameAs( group.SpriteList[1] ) );
			Assert.That( step1.ClassicCel!.Index, Is.EqualTo( 2 ) );
		}

		[Test]
		public void ResolveFramePlacements_OneShotTrackHoldsItsLastFrame()
		{
			var group = new SpriteGroup(
				spriteList: new List<Sprite> { BuildSprite( 0, 0, 10, 10 ), BuildSprite( 0, 0, 12, 12 ) },
				identifier: 0,
				index: 0,
				firstSpriteIdentifier: 0
			);
			var oneShot = BuildAnimation( "ChoreFront", limbNumber: 0, playbackFlags: 1, 0, 1 );
			var looping = BuildAnimation( "StandFront", limbNumber: 0, playbackFlags: 3, 0, 1 );

			var heldSprite = CostumeRenderer.ResolveFramePlacements( BuildCostume( group, oneShot ), oneShot, step: 2, null ).Single().Sprite;
			var wrappedSprite = CostumeRenderer.ResolveFramePlacements( BuildCostume( group, looping ), looping, step: 2, null ).Single().Sprite;

			Assert.That( heldSprite, Is.SameAs( group.SpriteList[1] ), "a one-shot track holds its last frame" );
			Assert.That( wrappedSprite, Is.SameAs( group.SpriteList[0] ), "a looping track wraps around" );
		}

		[Test]
		public void ResolveFramePlacements_PlacesTheSpriteAtItsOwnScreenPosition()
		{
			// the sprite's ScreenX/ScreenY is the placement the engine honors, even when it
			// disagrees with the classic cel rectangle (the cel is a calibration reference)
			var group = new SpriteGroup(
				spriteList: new List<Sprite> { BuildSprite( -30, -200, 20, 100 ) },
				identifier: 0,
				index: 0,
				firstSpriteIdentifier: 0
			);
			var limb = new ClassicLimb( 0, new List<ClassicCel?>
			{
				new ClassicCel( 0, 20, 10, 5, -50, 0, 0 ),
			} );
			var animation = BuildAnimation( "ChoreFront", limbNumber: 0, playbackFlags: 1, 0 );
			var costume = BuildCostume( group, animation );

			var placement = CostumeRenderer.ResolveFramePlacements( costume, animation, step: 0, BuildClassicCostume( limb ) ).Single();

			Assert.That( placement.ScreenRect, Is.EqualTo( new System.Drawing.RectangleF( -30, -200, 20, 100 ) ) );
			Assert.That( placement.ClassicCel, Is.Not.Null, "the cel stays available as a calibration reference" );
		}
	}
}
