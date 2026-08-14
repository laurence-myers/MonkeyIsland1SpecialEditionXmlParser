using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomPreviewControlBandTests
	{
		[Test]
		public void SpriteBand_Layer0_IsBand0_UnderEveryForegroundLayer()
		{
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 4, 4 ) )
			{
				var sprite = RoomPreviewControlHitTestTests.MakeSprite( texture, 0, 0, layer: 0 );
				Assert.That( RoomPreviewControl.SpriteBand( sprite, foregroundLayerCount: 2 ), Is.EqualTo( 0 ) );
			}
		}

		[Test]
		public void SpriteBand_Layer1_DrawsAfterForegroundLayer1()
		{
			// room 27's banana picker: Layer 1 in a room with one foreground layer, so it
			// composites over that layer (the hut wall) rather than under it
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 4, 4 ) )
			{
				var sprite = RoomPreviewControlHitTestTests.MakeSprite( texture, 0, 0, layer: 1 );
				Assert.That( RoomPreviewControl.SpriteBand( sprite, foregroundLayerCount: 1 ), Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void SpriteBand_LayerPastTheLastForegroundLayer_ClampsToTheTopBand()
		{
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 4, 4 ) )
			{
				var sprite = RoomPreviewControlHitTestTests.MakeSprite( texture, 0, 0, layer: 5 );
				Assert.That( RoomPreviewControl.SpriteBand( sprite, foregroundLayerCount: 2 ), Is.EqualTo( 2 ) );
			}
		}

		[Test]
		public void SpriteBand_NoForegroundLayers_EverythingIsBand0()
		{
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 4, 4 ) )
			{
				var high = RoomPreviewControlHitTestTests.MakeSprite( texture, 0, 0, layer: 3 );
				Assert.That( RoomPreviewControl.SpriteBand( high, foregroundLayerCount: 0 ), Is.EqualTo( 0 ) );
			}
		}
	}
}
