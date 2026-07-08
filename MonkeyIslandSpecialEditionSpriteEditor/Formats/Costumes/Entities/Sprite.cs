
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities
{
	public class Sprite( int textureNumber, int textureX, int textureY, int textureWidth, int textureHeight, float screenX, float screenY, float moveX, float moveY, int pathPointIndex ) : IAtlasSprite
	{
		private Sprite() : this(
			textureNumber: 0,
			textureX: 0,
			textureY: 0,
			textureWidth: 0,
			textureHeight: 0,
			screenX: 0,
			screenY: 0,
			moveX: 0,
			moveY: 0,
			pathPointIndex: -1
		) {}

		/// <summary>
		/// Gets or sets the index into the costume's texture file name list, or -1 for no texture.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the horizontal draw position relative to the actor origin, in HD pixels.
		/// Corresponds to the classic costume cel's relX scaled by the HD scale factor.
		/// </summary>
		public float ScreenX
		{
			get;
			set;
		} = screenX;

		/// <summary>
		/// Gets or sets the vertical draw position relative to the actor origin, in HD pixels.
		/// Corresponds to the classic costume cel's relY scaled by the HD scale factor.
		/// </summary>
		public float ScreenY
		{
			get;
			set;
		} = screenY;

		/// <summary>
		/// Gets or sets how far the actor moves horizontally when this sprite is shown, in HD
		/// pixels. Corresponds to the classic costume cel's moveX scaled by the HD scale factor.
		/// </summary>
		public float MoveX
		{
			get;
			set;
		} = moveX;

		/// <summary>
		/// Gets or sets how far the actor moves vertically when this sprite is shown, in HD
		/// pixels. Corresponds to the classic costume cel's moveY scaled by the HD scale factor.
		/// </summary>
		public float MoveY
		{
			get;
			set;
		} = moveY;

		/// <summary>
		/// Gets or sets the index into the costume's path point list (attachment points used by
		/// e.g. sword fighting costumes), or -1 for none.
		/// </summary>
		public int PathPointIndex
		{
			get;
			set;
		} = pathPointIndex;
	}
}
