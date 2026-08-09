using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomPreviewControlActorTests
	{
		[Test]
		public void ScreenRect_DefaultScale_IsThePositionAndNativeImageSize()
		{
			using( var image = new Bitmap( 100, 50 ) )
			{
				var actor = new RoomPreviewControlActor( MakePlacement(), image, PointF.Empty, "a" )
				{
					Position = new PointF( 10, 20 ),
				};

				Assert.That( actor.Scale, Is.EqualTo( 1.0f ) );
				Assert.That( actor.ScreenRect, Is.EqualTo( new RectangleF( 10, 20, 100, 50 ) ) );
			}
		}

		[Test]
		public void ScreenRect_ScaledDown_ShrinksTheDrawnSizeFromThePosition()
		{
			// a fullscreen room scales the actor below 1; the form sets Position from the scaled
			// origin so the feet stay anchored, and ScreenRect reports the scaled draw size
			using( var image = new Bitmap( 100, 50 ) )
			{
				var actor = new RoomPreviewControlActor( MakePlacement(), image, PointF.Empty, "a" )
				{
					Position = new PointF( 10, 20 ),
					Scale = 0.5f,
				};

				Assert.That( actor.ScreenRect, Is.EqualTo( new RectangleF( 10, 20, 50, 25 ) ) );
			}
		}

		[Test]
		public void ScreenRect_NoImage_IsEmptySize()
		{
			var actor = new RoomPreviewControlActor( MakePlacement(), null, PointF.Empty, "a" )
			{
				Position = new PointF( 5, 5 ),
				Scale = 0.5f,
			};

			Assert.That( actor.ScreenRect.Size, Is.EqualTo( SizeF.Empty ) );
		}

		private static ClassicActorPlacement MakePlacement()
		{
			return new ClassicActorPlacement(
				actorNumber: 1,
				x: 0,
				y: 0,
				costumeId: null,
				costumeInferred: false,
				direction: null,
				elevation: null,
				source: "test"
			);
		}
	}
}
