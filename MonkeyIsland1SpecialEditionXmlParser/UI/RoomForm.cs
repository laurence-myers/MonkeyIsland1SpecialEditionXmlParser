using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using System.Drawing;
using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class RoomForm : Form
	{
		private static readonly List<RoomForm> instances = new List<RoomForm>();
		private float scale = 0.5f;

		public Room Room
		{
			get;
			set;
		}

		public static RoomForm[] Instances
		{
			get
			{
				return RoomForm.instances.ToArray();
			}
		}

		public RoomForm()
		{
			RoomForm.instances.Add( this );
			this.FormClosed += delegate { RoomForm.instances.Remove( this ); };

			this.InitializeComponent();

			this.Load += delegate { this.RenderRoom(); };
		}

		private void RenderRoom()
		{
			if( this.Room == null )
			{
				return;
			}

			//var width = this.Room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.X + ss.Width ) );
			//var height = this.Room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.Y + ss.Height ) );

			this.spriteSetPreviewControl.Sprites = new List<SpriteSetPreviewControlSprite>();

			for( var index = 0; index < this.Room.StaticSpriteList.Count; index++ )
			{
				var staticSpriteList = this.Room.StaticSpriteList[index];
				var staticSprite = this.RenderStaticSpriteList( staticSpriteList );
				if( staticSprite == null )
				{
					continue;
				}

				this.spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite()
				{
					Image = staticSprite,
					Layer = index,
					Name = "StaticSprite" + index,
				} );
			}

			for( var index = 0; index < this.Room.SpriteGroupList.Count; index++ )
			{
				var spriteGroup = this.Room.SpriteGroupList[index];
				var image = this.RenderSpriteGroup( spriteGroup );
				if( image == null )
				{
					continue;
				}

				this.spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite()
				{
					Image = image,
					Layer = index * 100,
					Name = "Sprite" + index,
				} );

				return;
			}
		}

		private Bitmap RenderStaticSpriteList( List<StaticSprite> staticSpriteList )
		{
			var areAllFilesNotFound = staticSpriteList.All( ssl => !File.Exists( ssl.TextureFileName.Replace( ".dxt", ".png" ) ) );
			if( areAllFilesNotFound )
			{
				return null;
			}

			var width = staticSpriteList.Max( ss => ss.X + ss.Width );
			var height = staticSpriteList.Max( ss => ss.Y + ss.Height );

			var bitmap = new Bitmap( (int)( width * this.scale ), (int)( height * this.scale ) );
			var graphics = Graphics.FromImage( bitmap );
			graphics.Clear( Color.Transparent );

			foreach( var staticSprite in staticSpriteList )
			{
				var textureFileName = staticSprite.TextureFileName.Replace( ".dxt", ".png" );
				var texture = Image.FromFile( textureFileName );
				var destRect = new RectangleF( staticSprite.X * this.scale, staticSprite.Y * this.scale, staticSprite.Width * this.scale, staticSprite.Height * this.scale );
				var srcRect = new RectangleF( 0.0f, 0.0f, texture.Width, texture.Height );
				graphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
			}

			return bitmap;
		}

		private Bitmap RenderSpriteGroup(SpriteGroup spriteGroup)
		{
			if( spriteGroup.SpriteList.All( s => !File.Exists( s.TextureFileName.Replace( ".dxt", ".png" ) ) ) )
			{
				return null;
			}

			var maxWidth = spriteGroup.SpriteList.Max( s => s.TextureWidth );
			var maxHeight = spriteGroup.SpriteList.Max( s => s.TextureHeight );

			var width = maxWidth;
			var height = maxHeight * spriteGroup.SpriteList.Count;

			var bitmap = new Bitmap( (int)( width * this.scale ), (int)( height * this.scale ) );
			var graphics = Graphics.FromImage( bitmap );

			for( var index = 0; index < spriteGroup.SpriteList.Count; index++ )
			{
				var sprite = spriteGroup.SpriteList[index];
				var textureFileName = sprite.TextureFileName.Replace( ".dxt", ".png" );
				var texture = Image.FromFile( textureFileName );
				var destRect = new RectangleF( sprite.OffsetX * this.scale, index * maxHeight * this.scale + sprite.OffsetY, bitmap.Width, bitmap.Height );
				var srcRect = new RectangleF( sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight );
				graphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
			}

			return bitmap;
		}
	}
}
