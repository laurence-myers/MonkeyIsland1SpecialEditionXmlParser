
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes
{
	/// <summary>
	/// Helpers for pointing a costume sprite at a texture. Costume sprites reference a texture
	/// by an index into <see cref="Costume.TextureFileNameList"/>, so retargeting one to a
	/// texture the costume does not list yet means appending an entry first.
	/// </summary>
	public static class TextureAssignment
	{
		/// <summary>
		/// Returns the index of <paramref name="path"/> in the costume's texture file name list,
		/// appending a new entry when the path is not already listed. The returned index is a
		/// valid <c>Sprite.TextureNumber</c>: the packer derives the texture count, the paired
		/// texture headers and the per-texture sprite counts entirely from the list and the
		/// sprites, so an appended entry round-trips with no other edits.
		/// </summary>
		public static int GetOrAddTextureIndex( Costume costume, string path )
		{
			var existingIndex = costume.TextureFileNameList.FindIndex( t => t.Path == path );
			if( existingIndex >= 0 )
			{
				return existingIndex;
			}

			var newIndex = costume.TextureFileNameList.Count;
			costume.TextureFileNameList.Add( new TextureFileName( path, newIndex ) );
			return newIndex;
		}
	}
}
