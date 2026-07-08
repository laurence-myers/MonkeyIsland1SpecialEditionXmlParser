using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities
{
	public class Costume(
		Header header,
		List<TextureHeader> textureHeaderList,
		List<AnimationHeader> animationHeaderList,
		List<PathPoint> pathPointList,
		List<SpriteGroupHeader> spriteGroupHeaderList,
		List<TextureFileName> textureFileNameList,
		List<Animation> animationList,
		List<SpriteGroup> spriteGroupList
	)
	{
		// Required for serialization
		private Costume() : this(
			header: null!,
			textureHeaderList: null!,
			animationHeaderList: null!,
			pathPointList: null!,
			spriteGroupHeaderList: null!,
			textureFileNameList: null!,
			animationList: null!,
			spriteGroupList: null!
		) {}
		
		/// <summary>
		/// Gets or sets the header.
		/// </summary>
		public Header Header
		{
			get;
			set;
		} = header;

		/// <summary>
		/// Gets or sets the list of texture headers.
		/// </summary>
		public List<TextureHeader> TextureHeaderList
		{
			get;
			set;
		} = textureHeaderList;

		/// <summary>
		/// Gets or sets the list of animation headers.
		/// </summary>
		public List<AnimationHeader> AnimationHeaderList
		{
			get;
			set;
		} = animationHeaderList;

		/// <summary>
		/// Gets or sets the list of path points (attachment points referenced by sprites via
		/// <see cref="Sprite.PathPointIndex"/>).
		/// </summary>
		public List<PathPoint> PathPointList
		{
			get;
			set;
		} = pathPointList;

		/// <summary>
		/// Gets or sets the list of sprite group headers.
		/// </summary>
		public List<SpriteGroupHeader> SpriteGroupHeaderList
		{
			get;
			set;
		} = spriteGroupHeaderList;

		/// <summary>
		/// Gets or sets the list of texture file names.
		/// </summary>
		public List<TextureFileName> TextureFileNameList
		{
			get;
			set;
		} = textureFileNameList;

		/// <summary>
		/// Gets or sets the list of animations.
		/// </summary>
		public List<Animation> AnimationList
		{
			get;
			set;
		} = animationList;

		/// <summary>
		/// Gets or sets the list of sprite groups.
		/// </summary>
		public List<SpriteGroup> SpriteGroupList
		{
			get;
			set;
		} = spriteGroupList;

		public override string ToString()
		{
			return this.Header.Name;
		}
	}
}
