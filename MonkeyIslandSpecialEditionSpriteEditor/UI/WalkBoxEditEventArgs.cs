using System;
using System.Collections.Generic;
using System.Drawing;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// One corner's move within a walkbox drag: which box and corner, and the point before and
	/// after. A drag can move several coincident corners at once, so a completed drag reports a
	/// list of these.
	/// </summary>
	public struct WalkBoxCornerChange( int boxIndex, int cornerIndex, Point oldPoint, Point newPoint )
	{
		public int BoxIndex
		{
			get;
		} = boxIndex;

		public int CornerIndex
		{
			get;
		} = cornerIndex;

		public Point OldPoint
		{
			get;
		} = oldPoint;

		public Point NewPoint
		{
			get;
		} = newPoint;
	}

	/// <summary>
	/// Reports a completed walkbox drag (corner or whole box) as the set of corner moves it made,
	/// so the editor can record one undoable edit for the whole gesture.
	/// </summary>
	public sealed class WalkBoxEditEventArgs( IReadOnlyList<WalkBoxCornerChange> changes ) : EventArgs
	{
		public IReadOnlyList<WalkBoxCornerChange> Changes
		{
			get;
		} = changes;
	}
}
