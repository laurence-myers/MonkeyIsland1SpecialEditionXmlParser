using MonkeyIsland1SpecialEditionXmlParser.Commands;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class Command
	{
		public static readonly AddRecentCostumeFileNameCommand AddRecentCostumeFileName = new AddRecentCostumeFileNameCommand();
		public static readonly AddRecentRoomFileNameCommand AddRecentRoomFileName = new AddRecentRoomFileNameCommand();
		public static readonly OpenCostumeFormCommand OpenCostumeForm = new OpenCostumeFormCommand();
		public static readonly OpenFileCommand OpenFile = new OpenFileCommand();
		public static readonly OpenFileWithDialogCommand OpenFileWithDialog = new OpenFileWithDialogCommand();
		public static readonly OpenImageExportDialogCommand OpenImageExportDialog = new OpenImageExportDialogCommand();
		public static readonly OpenRoomFormCommand OpenRoomForm = new OpenRoomFormCommand();
		public static readonly OpenXmlExportDialogCommand OpenXmlExportDialog = new OpenXmlExportDialogCommand();
		public static readonly SetMainFormTitleCommand SetMainFormTitle = new SetMainFormTitleCommand();
	}
}
