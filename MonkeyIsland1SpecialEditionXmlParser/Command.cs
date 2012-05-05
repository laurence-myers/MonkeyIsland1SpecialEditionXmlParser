using MonkeyIsland1SpecialEditionXmlParser.Commands;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class Command
	{
		public static readonly AddRecentCostumeFileNameCommand AddRecentCostumeFileName = new AddRecentCostumeFileNameCommand();
		public static readonly AddRecentLPAKFileNameCommand AddRecentLPAKFileName = new AddRecentLPAKFileNameCommand();
		public static readonly ExportRoomToMergedPngCommand ExportRoomToMergedPng = new ExportRoomToMergedPngCommand();
		public static readonly ExportRoomToPngCommand ExportRoomToPng = new ExportRoomToPngCommand();
		public static readonly ExportRoomToMergedPngWithDialogCommand ExportRoomToMergedPngWithDialog = new ExportRoomToMergedPngWithDialogCommand();
		public static readonly ExportRoomToPngWithDialogCommand ExportRoomToPngWithDialog = new ExportRoomToPngWithDialogCommand();
		public static readonly ExportToBinaryCommand ExportToBinary = new ExportToBinaryCommand();
		public static readonly ExportToBinaryWithDialogCommand ExportToBinaryWithDialog = new ExportToBinaryWithDialogCommand();
		public static readonly ExportToPngCommand ExportToPng = new ExportToPngCommand();
		public static readonly ExportToPngWithDialogCommand ExportToPngWithDialog = new ExportToPngWithDialogCommand();
		public static readonly ExportToXmlCommand ExportToXml = new ExportToXmlCommand();
		public static readonly OpenCostumeFormCommand OpenCostumeForm = new OpenCostumeFormCommand();
		public static readonly OpenFileCommand OpenFile = new OpenFileCommand();
		public static readonly OpenFileWithDialogCommand OpenFileWithDialog = new OpenFileWithDialogCommand();
		public static readonly OpenHexFormCommand OpenHexForm = new OpenHexFormCommand();
		public static readonly OpenImageExportDialogCommand OpenImageExportDialog = new OpenImageExportDialogCommand();
		public static readonly OpenImageFormCommand OpenImageForm = new OpenImageFormCommand();
		public static readonly OpenLPAKFormCommand OpenLPAKForm = new OpenLPAKFormCommand();
		public static readonly OpenQuickStartDialogCommand OpenQuickStartDialog = new OpenQuickStartDialogCommand();
		public static readonly OpenRoomFormCommand OpenRoomForm = new OpenRoomFormCommand();
		public static readonly OpenShaderFormCommand OpenShaderForm = new OpenShaderFormCommand();
	}
}
