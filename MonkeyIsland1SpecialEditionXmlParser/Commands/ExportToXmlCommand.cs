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
		private XmlExportDialog xmlExportDialog;

		public object ObjectToExport
		{
			get;
			set;
		}

		public string ExportFileName
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

		public string XsltFileName
		{
			get
			{
				return this.xmlExportDialog.XsltFileName;
			}
		}

		public ExportToXmlCommand()
		{
			this.xmlExportDialog = new XmlExportDialog()
			{
				Text = "XML Export",
			};
		}

		protected override bool InnerExecute()
		{
			if( this.xmlExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			var exportFileName = Command.ExportToXml.ExportFileName;
			var xsltFileName = Command.ExportToXml.XsltFileName;

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
