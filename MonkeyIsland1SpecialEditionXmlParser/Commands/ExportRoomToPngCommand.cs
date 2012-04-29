using System;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngCommand : BaseCommand
	{
		public string ExportFileName
		{
			get;
			set;
		}

		public string LPAKFileName
		{
			get;
			set;
		}

		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public Room Room
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			throw new NotImplementedException();
		}
	}
}
