
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	/// <summary>
	/// A named room object ("Chandelier", "Water", "Lava", ...), used by the game to attach
	/// dynamic overlays to a room. Each header owns a list of <see cref="RoomObject"/> instances.
	/// </summary>
	public class RoomObjectHeader(
		int nameAddress,
		int roomObjectCount,
		int roomObjectAddress
	)
	{
		private RoomObjectHeader() : this(
			nameAddress: 0,
			roomObjectCount: 0,
			roomObjectAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the byte address of the object's name.
		/// </summary>
		public int NameAddress
		{
			get;
			set;
		} = nameAddress;

		/// <summary>
		/// Gets or sets the object's name (e.g. "Chandelier"). Never empty in the original
		/// game data; the game looks objects up by name.
		/// </summary>
		public string? Name
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of room object instances.
		/// </summary>
		public int RoomObjectCount
		{
			get;
			set;
		} = roomObjectCount;

		/// <summary>
		/// Gets or sets the byte address of the first room object instance.
		/// </summary>
		public int RoomObjectAddress
		{
			get;
			set;
		} = roomObjectAddress;

		public override string ToString()
		{
			return string.Concat(
				this.Name, "; ",
				this.RoomObjectCount, "; ",
				this.RoomObjectAddress
				);
		}
	}
}
