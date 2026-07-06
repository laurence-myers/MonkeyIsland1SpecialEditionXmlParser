using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	/// <summary>
	/// A room object drawn as a chunked image, used for room-sized overlays such as the
	/// animated "extra_*" images (lava, water, clouds).
	/// </summary>
	public class RoomObjectImage(
		int sourceWidth,
		int sourceHeight,
		int chunkAddress
	)
	{
		private RoomObjectImage() : this(
			sourceWidth: 0,
			sourceHeight: 0,
			chunkAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the width of the image in source art pixels (the hi-res canvas the
		/// art was authored at, roughly 2.3-2.5x the screen-space room size; same unit as
		/// <see cref="StaticSpriteHeader.SourceWidth"/>).
		/// </summary>
		public int SourceWidth
		{
			get;
			set;
		} = sourceWidth;

		/// <summary>
		/// Gets or sets the height of the image in source art pixels.
		/// </summary>
		public int SourceHeight
		{
			get;
			set;
		} = sourceHeight;

		/// <summary>
		/// Gets or sets the byte address of the first chunk record.
		/// </summary>
		public int ChunkAddress
		{
			get;
			set;
		} = chunkAddress;

		public List<RoomObjectImageChunk> ChunkList
		{
			get;
			set;
		} = [];

		public override string ToString()
		{
			return string.Concat(
				this.SourceWidth, "; ",
				this.SourceHeight, "; ",
				this.ChunkList.Count
				);
		}
	}
}
