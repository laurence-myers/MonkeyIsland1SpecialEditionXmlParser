using System;
using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Reports a move of a sprite's source rectangle in the <see cref="AtlasViewControl"/>,
	/// carrying the location before and after the move (so the edit can be undone) and whether it
	/// is part of an ongoing drag (so a whole drag coalesces into one undo step).
	/// </summary>
	public sealed class SpriteRectChangedEventArgs( IAtlasSprite sprite, Point oldLocation, Point newLocation, bool dragging ) : EventArgs
	{
		/// <summary>
		/// Gets the sprite whose rectangle moved.
		/// </summary>
		public IAtlasSprite Sprite
		{
			get;
		} = sprite;

		/// <summary>
		/// Gets the sprite's texture rectangle location before this move.
		/// </summary>
		public Point OldLocation
		{
			get;
		} = oldLocation;

		/// <summary>
		/// Gets the sprite's texture rectangle location after this move.
		/// </summary>
		public Point NewLocation
		{
			get;
		} = newLocation;

		/// <summary>
		/// Gets whether the move is part of an in-progress mouse drag (versus a discrete
		/// arrow-key nudge).
		/// </summary>
		public bool Dragging
		{
			get;
		} = dragging;
	}
}
