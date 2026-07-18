
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats
{
	/// <summary>
	/// An entity that draws from a texture referenced by its file name, as opposed to the
	/// costume sprites which reference a texture by index. Implemented by the room entities
	/// so the room editor can retarget any of them through one path.
	/// </summary>
	public interface ITextureReference
	{
		string? TextureFileName
		{
			get;
			set;
		}
	}
}
