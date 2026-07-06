using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class SpriteGroup( List<Sprite> spriteList, int identifier, int index, int firstSpriteIdentifier )
	{
		private SpriteGroup() : this(
			spriteList: null!,
			identifier: 0,
			index: 0,
			firstSpriteIdentifier: 0
		) {}

		/// <summary>
		/// Gets or sets the index of the sprite group.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the sprite group identifier (the classic SCUMM limb number).
		/// </summary>
		public int Identifier
		{
			get;
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the identifier of the first sprite in the group.
		/// <see cref="Frame.SpriteIdentifier"/> values are resolved against this base:
		/// the sprite drawn is <see cref="SpriteList"/>[Frame.SpriteIdentifier - FirstSpriteIdentifier].
		/// </summary>
		public int FirstSpriteIdentifier
		{
			get;
			set;
		} = firstSpriteIdentifier;

		/// <summary>
		/// Gets or sets the list of sprites.
		/// </summary>
		public List<Sprite> SpriteList
		{
			get;
			set;
		} = spriteList;

		/// <summary>
		/// Returns the sprite shown by the frame, resolving the frame's sprite identifier
		/// against <see cref="FirstSpriteIdentifier"/>, or null for command frames and
		/// out-of-range identifiers.
		/// </summary>
		public Sprite? ResolveSprite( Frame frame )
		{
			var index = frame.SpriteIdentifier - this.FirstSpriteIdentifier;
			if( frame.SpriteIdentifier < 0 || index < 0 || index >= this.SpriteList.Count )
			{
				return null;
			}
			return this.SpriteList[index];
		}

		public override string ToString()
		{
			return string.Concat( "SpriteGroup [Identifier=", this.Identifier, "; Sprites=", this.SpriteList.Count, "]" );
		}
	}
}
