
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Sprite( int textureNumber, int textureX, int textureY, int textureWidth, int textureHeight, float screenX, float screenY, int unknownInteger1, int unknownInteger2, int unknownInteger3 )
	{
		private Sprite() : this(
			textureNumber: 0,
			textureX: 0,
			textureY: 0,
			textureWidth: 0,
			textureHeight: 0,
			screenX: 0,
			screenY: 0,
			unknownInteger1: 0,
			unknownInteger2: 0,
			unknownInteger3: 0
		) {}

		public int TextureNumber
		{
			get;
			set;
		} = textureNumber;

		public int TextureX
		{
			get;
			set;
		} = textureX;

		public int TextureY
		{
			get;
			set;
		} = textureY;

		public int TextureWidth
		{
			get;
			set;
		} = textureWidth;

		public int TextureHeight
		{
			get;
			set;
		} = textureHeight;

		public float ScreenX
		{
			get;
			set;
		} = screenX;

		public float ScreenY
		{
			get;
			set;
		} = screenY;

		public int UnknownInteger1
		{
			get;
			set;
		} = unknownInteger1;

		public int UnknownInteger2
		{
			get;
			set;
		} = unknownInteger2;

		public int UnknownInteger3
		{
			get;
			set;
		} = unknownInteger3;
	}
}
