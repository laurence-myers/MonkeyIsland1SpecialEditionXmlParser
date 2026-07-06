
namespace MonkeyIsland1SpecialEditionXmlParser.Formats
{
	/// <summary>
	/// A sprite with a source rectangle on a spritesheet texture, as edited by
	/// <see cref="UI.AtlasViewControl"/>. Implemented by both the room and the costume
	/// sprite entities.
	/// </summary>
	public interface IAtlasSprite
	{
		int TextureX
		{
			get;
			set;
		}

		int TextureY
		{
			get;
			set;
		}

		int TextureWidth
		{
			get;
			set;
		}

		int TextureHeight
		{
			get;
			set;
		}
	}
}
