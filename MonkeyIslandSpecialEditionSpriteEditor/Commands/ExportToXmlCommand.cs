using System;
using System.IO;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class ExportToXmlCommand : BaseCommand
	{
		private readonly XmlExportDialog xmlExportDialog;

		private object ObjectToExport { get; }

		private string ExportFileName
		{
			get
			{
				return this.xmlExportDialog.ExportFileName;
			}
			set
			{
				if( string.IsNullOrWhiteSpace( value ) || value.Contains( Path.DirectorySeparatorChar.ToString() ) )
				{
					this.xmlExportDialog.ExportFileName = value;
					return;
				}
				if( string.IsNullOrWhiteSpace( this.xmlExportDialog.ExportFileName ) )
				{
					this.xmlExportDialog.ExportFileName = Path.Combine( Environment.CurrentDirectory, value );
					return;
				}
				var directoryName = Path.GetDirectoryName( this.xmlExportDialog.ExportFileName );
				this.xmlExportDialog.ExportFileName = Path.Combine( directoryName, value );
			}
		}

		public ExportToXmlCommand(object objectToExport, string exportFileName)
		{
			this.ObjectToExport = objectToExport;

			this.xmlExportDialog = new XmlExportDialog()
			{
				Text = "XML Export",
			};

			this.ExportFileName = exportFileName;
		}

		protected override CommandResult InnerExecute()
		{
			if( this.xmlExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( string.Empty );
			}

			var exportFileName = this.ExportFileName;

			Helper.WriteObjectToFile( exportFileName, this.ObjectToExport );

			return CommandResult.Success($"Exported to XML: {exportFileName}");
		}
	}
}
