
using System;
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	public class Unknown6Header(
		byte unkn1,
		byte unkn2,
		byte unkn3,
		byte unkn4,
		int unkn5,
		int unknown6Count,
		int unknown6Address
	)
	{
		private Unknown6Header() : this(
			unkn1: 0,
			unkn2: 0,
			unkn3: 0,
			unkn4: 0,
			unkn5: 0,
			unknown6Count: 0,
			unknown6Address: 0
		) {}

		public byte Unkn1
		{
			get;
			set;
		} = unkn1;

		public byte Unkn2
		{
			get;
			set;
		} = unkn2;

		public byte Unkn3
		{
			get;
			set;
		} = unkn3;

		public byte Unkn4
		{
			get;
			set;
		} = unkn4;

		public int Unkn5
		{
			get;
			set;
		} = unkn5;

		public int Unknown6Count
		{
			get;
			set;
		} = unknown6Count;

		public int Unknown6Address
		{
			get;
			set;
		} = unknown6Address;

		public override string ToString()
		{
			return string.Concat(
				this.Unkn1, "; ",
				this.Unkn2, "; ",
				this.Unkn3, "; ",
				this.Unkn4, "; ",
				this.Unkn5, "; ",
				this.Unknown6Count, "; ",
				this.Unknown6Address
				);
		}
	}
}
