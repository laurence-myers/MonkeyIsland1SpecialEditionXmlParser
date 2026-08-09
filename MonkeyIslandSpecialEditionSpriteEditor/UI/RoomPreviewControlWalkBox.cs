using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// A walkbox shown by the <see cref="RoomPreviewControl"/> overlay: a working-copy
	/// <see cref="ClassicBox"/> plus its index in the room's box list (its identity for the box
	/// matrix, so it never changes).
	/// </summary>
	public class RoomPreviewControlWalkBox( int index, ClassicBox box )
	{
		/// <summary>
		/// Gets the box's index in the room's walkbox list.
		/// </summary>
		public int Index
		{
			get;
		} = index;

		/// <summary>
		/// Gets the working-copy box this overlay draws and edits.
		/// </summary>
		public ClassicBox Box
		{
			get;
		} = box;
	}
}
