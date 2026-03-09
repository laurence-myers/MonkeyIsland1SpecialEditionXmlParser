using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
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

		private string XsltFileName
		{
			get
			{
				return this.xmlExportDialog.XsltFileName;
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

		protected override bool InnerExecute()
		{
			if( this.xmlExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			var exportFileName = this.ExportFileName;
			var xsltFileName = this.XsltFileName;

			Helper.WriteObjectToFile( exportFileName, this.ObjectToExport );

			var isXsltFileNameValid
				= !string.IsNullOrWhiteSpace( xsltFileName )
				&& File.Exists( xsltFileName )
				;
			if( isXsltFileNameValid )
			{
				var xmlReaderSettings = new XmlReaderSettings()
				{
					DtdProcessing = DtdProcessing.Parse,
				};
				using( var reader = XmlReader.Create( xsltFileName, xmlReaderSettings ) )
				{
					var tempFileName = Path.GetTempFileName();
					var transform = new XslCompiledTransform();
					transform.Load( reader );
					transform.Transform( exportFileName, tempFileName );
					File.Copy( tempFileName, exportFileName, overwrite: true );
					File.Delete( tempFileName );
				}
			}
			
			return true;
		}
	}
}
