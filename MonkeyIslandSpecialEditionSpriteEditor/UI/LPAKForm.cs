using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class LPAKForm : Form
	{
		private LPAKFile LPAKFile
		{
			get;
			set;
		}

		private string FileName
		{
			get;
			set;
		}

		public LPAKForm( string fileName, Form mdiParent, LPAKFile lpakFile, FormWindowState windowState )
		{
			this.InitializeComponent();
			this.FileName = fileName;
			this.MdiParent = mdiParent;
			this.LPAKFile = lpakFile;
			this.WindowState = windowState;
		}

		private void HandleFormLoad( object sender, EventArgs args )
		{
			var rootNode = new TreeNode()
			{
				Text = Path.GetFileName( this.FileName ) ?? "",
			};
			this.treeView1.Nodes.Add( rootNode );

			//-------------------------------------------
			// costumes folder
			var costumesNode = new TreeNode()
			{
				Text = "Costumes",
			};
			costumesNode.Nodes.Add( "---" );
			rootNode.Nodes.Add( costumesNode );

			//-------------------------------------------
			// others folder
			var othersNode = new TreeNode()
			{
				Text = "Other",
			};
			othersNode.Nodes.Add( "---" );
			rootNode.Nodes.Add( othersNode );

			//-------------------------------------------
			// rooms folder
			var roomsNode = new TreeNode()
			{
				Text = "Rooms",
			};
			roomsNode.Nodes.Add( "---" );
			rootNode.Nodes.Add( roomsNode );

			//-------------------------------------------
			// shaders folder
			var shadersNode = new TreeNode()
			{
				Text = "Shaders",
			};
			shadersNode.Nodes.Add( "---" );
			rootNode.Nodes.Add( shadersNode );

			//-------------------------------------------
			// textures folder
			var texturesNode = new TreeNode()
			{
				Text = "Textures",
			};
			texturesNode.Nodes.Add( "---" );
			rootNode.Nodes.Add( texturesNode );

			this.treeView1.Sort();
			rootNode.Expand();
		}

		private string FileNameToDisplayName( string fileName )
		{
			string? language = null;

			fileName = Path.GetFileNameWithoutExtension( fileName );
			fileName = fileName.Substring( 0, fileName.LastIndexOf( '.' ) );

			if( fileName[2] == '.' )
			{
				var languageCode = fileName.Substring( 0, 2 );
				language = this.LanguageCodeToDisplayName( languageCode );
				fileName = fileName.Substring( 3 );
			}

			var length = fileName.IndexOf( '_' );
			var identifierString = fileName.Substring( 0, length );
			var identifier = int.Parse( identifierString );
			fileName = fileName.Substring( length + 1 );

			var builder = new StringBuilder();
			builder.Append( identifier.ToString( "000" ), " - ", fileName );
			if( !string.IsNullOrEmpty( language ) )
			{
				builder.Append( " (", language!, ")" );
			}

			return builder.ToString();
		}

		private string LanguageCodeToDisplayName( string languageCode )
		{
			switch( languageCode )
			{
				case "ge":
					return "German";
				case "fr":
					return "French";
				case "sp":
					return "Spanish";
				case "it":
					return "Italian";
				default:
					throw new NotSupportedException( string.Concat( "Language code ", languageCode, " not supported." ) );
			}
		}

		private void HandleMouseDoubleClick( object sender, TreeNodeMouseClickEventArgs? args )
		{
			if( args == null )
			{
				return;
			}

			var node = args.Node;
			if( node == null )
			{
				return;
			}

			switch( node.Level )
			{
				case 2:
					this.HandleLevel2DoubleClick( node );
					break;
				default:
					// do nothing
					break;
			}
		}

		private void HandleLevel1BeforeExpand( TreeNode? node )
		{
			if( node == null )
			{
				return;
			}
			if( node.Nodes.Count != 1 || node.Nodes[0].Text != "---" )
			{
				return;
			}

			node.Nodes.Clear();

			for( var index = 0; index < this.LPAKFile.PakFileNames.Length; index++ )
			{
				var fileName = this.LPAKFile.PakFileNames[index].FileName;
				if( string.IsNullOrWhiteSpace( fileName ) || fileName is null )
				{
					continue;
				}

				//-------------------------------------------
				// costume folder
				if( node.Text == "Costumes" && fileName.EndsWith( ".costume.xml" ) )
				{
					var costumeNode = new TreeNode()
					{
						Text = this.FileNameToDisplayName( fileName ),
						Tag = index,
					};
					node.Nodes.Add( costumeNode );
				}

				//-------------------------------------------
				// room folder
				else if( node.Text == "Rooms" && fileName.EndsWith( ".room.xml" ) )
				{
					var roomNode = new TreeNode()
					{
						Text = this.FileNameToDisplayName( fileName ),
						Tag = index,
					};
					node.Nodes.Add( roomNode );
				}

				//-------------------------------------------
				// shader folder
				else if( node.Text == "Shaders" && fileName.EndsWith( ".fx" ) )
				{
					var shaderNode = new TreeNode()
					{
						Text = Path.GetFileNameWithoutExtension( fileName ),
						Tag = index,
					};
					node.Nodes.Add( shaderNode );
				}
				//-------------------------------------------
				// texture folder
				else if( node.Text == "Textures" && fileName.EndsWith( ".dxt" ) )
				{
					var textureNode = new TreeNode()
					{
						Text = fileName,
						Tag = index,
					};
					node.Nodes.Add( textureNode );
				}

				//-------------------------------------------
				// other folder: anything not shown in the sections above
				else if( node.Text == "Other"
					&& !fileName.EndsWith( ".costume.xml" )
					&& !fileName.EndsWith( ".room.xml" )
					&& !fileName.EndsWith( ".fx" )
					&& !fileName.EndsWith( ".dxt" )
					&& !fileName.EndsWith( ".png" ) )
				{
					var otherNode = new TreeNode()
					{
						Text = fileName,
						Tag = index,
					};
					node.Nodes.Add( otherNode );
				}
			}
		}

		private void HandleLevel2DoubleClick( TreeNode node )
		{
			var fileIndex = (int)node.Tag;
			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;

			switch( node.Parent.Text )
			{
				case "Costumes":
					new OpenCostumeSpriteSheetEditorCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Rooms":
					new OpenSpriteSheetEditorCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Shaders":
					new OpenShaderFormCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Textures":
					new OpenImageFormCommand( this.LPAKFile, fileName ).Execute();
					break;
				default:
					new OpenHexFormCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
			}
		}

		private void ApplyFilter( object sender, EventArgs args )
		{
			var input = this.textBox1.Text;
			if( string.IsNullOrWhiteSpace( input ) )
			{
				this.RecursiveForEach( this.treeView1.Nodes, n =>
				{
					n.BackColor = Color.Transparent;
					n.ForeColor = Color.Black;
				} );
				return;
			}

			var words = input.Split( ' ' );
			this.RecursiveForEach( this.treeView1.Nodes, n =>
			{
				if( n.Level != 2 )
				{
					return;
				}
				if( words.All( w => n.Text.Contains( w ) ) )
				{
					n.BackColor = Color.YellowGreen;
					n.ForeColor = Color.Black;
				}
				else
				{
					n.BackColor = Color.Transparent;
					n.ForeColor = Color.Black;
				}
			} );
		}

		private void RecursiveForEach( TreeNodeCollection nodes, Action<TreeNode> action )
		{
			foreach( TreeNode node in nodes )
			{
				action( node );
				this.RecursiveForEach( node.Nodes, action );
			}
		}

		private void HandleBeforeExpand( object sender, TreeViewCancelEventArgs? args )
		{
			if( args == null )
			{
				return;
			}

			var node = args.Node;
			if( node == null )
			{
				return;
			}

			switch( node.Level )
			{
				case 1:
					this.HandleLevel1BeforeExpand( node );
					break;
			}
		}

		private void OpenInHexView( object sender, EventArgs args )
		{
			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return;
			}

			new OpenHexFormCommand( this.LPAKFile, fileName, fileIndex ).Execute();
		}

		private void ShowContextMenu( object sender, TreeNodeMouseClickEventArgs args )
		{
			var node = args.Node;
			if( node == null || args.Button != MouseButtons.Right )
			{
				return;
			}

			var isResource = node.Level == 2;
			var section = isResource ? node.Parent.Text : "";
			var isCostume = section == "Costumes";
			var isRoom = section == "Rooms";
			var isShader = section == "Shaders";
			var isTexture = section == "Textures";
			var isKnownFormat = isCostume || isRoom || isShader || isTexture;

			this.openViewerToolStripMenuItem.Visible = isKnownFormat;
			// for costumes and rooms, the viewer IS the spritesheet editor
			this.openViewerToolStripMenuItem.Text = isCostume || isRoom
				? "Open in Spritesheet Editor"
				: "Open in Viewer";
			this.viewAsHEXToolStripMenuItem.Visible = isResource && !isKnownFormat;
			this.saveAsToolStripMenuItem.Visible = isResource;
			this.overridesToolStripMenuItem.Visible = isCostume || isRoom;
			this.exportTexturePngToolStripMenuItem.Visible = isTexture;
			this.importTexturePngToolStripMenuItem.Visible = isTexture;

			// batch export/import lives on the "Textures" folder itself
			var isTexturesFolder = node.Level == 1 && node.Text == "Textures";
			this.exportAllTexturesToolStripMenuItem.Visible = isTexturesFolder;
			this.importAllTexturesToolStripMenuItem.Visible = isTexturesFolder;

			if( !isResource && !isTexturesFolder )
			{
				return;
			}

			this.contextMenuStrip.Tag = node;
			this.contextMenuStrip.Show( this.treeView1.PointToScreen( args.Location ) );
		}

		/// <summary>
		/// Resolves the file entry of the tree node the context menu was opened on.
		/// </summary>
		private bool TryGetContextResource( out int fileIndex, out string fileName )
		{
			fileIndex = -1;
			fileName = "";

			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null || !( selectedNode.Tag is int ) )
			{
				return false;
			}

			fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return false;
			}

			var name = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( name ) || name is null )
			{
				return false;
			}

			fileName = name;
			return true;
		}

		private void OpenInViewer( object sender, EventArgs args )
		{
			int fileIndex;
			string fileName;
			if( !this.TryGetContextResource( out fileIndex, out fileName ) )
			{
				return;
			}

			var selectedNode = (TreeNode)this.contextMenuStrip.Tag;
			switch( selectedNode.Parent.Text )
			{
				case "Costumes":
					new OpenCostumeSpriteSheetEditorCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Rooms":
					new OpenSpriteSheetEditorCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Shaders":
					new OpenShaderFormCommand( this.LPAKFile, fileName, fileIndex ).Execute();
					break;
				case "Textures":
					new OpenImageFormCommand( this.LPAKFile, fileName ).Execute();
					break;
			}
		}

		private void ExportTexturePng( object sender, EventArgs args )
		{
			int fileIndex;
			string fileName;
			if( !this.TryGetContextResource( out fileIndex, out fileName ) )
			{
				return;
			}

			var bitmap = this.LPAKFile.LoadImage( fileName ) as Bitmap;
			if( bitmap == null )
			{
				MessageBox.Show( this, "The texture could not be loaded.", "Export texture", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			using( var dialog = new SaveFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = "Export texture as PNG";
				dialog.FileName = Path.GetFileNameWithoutExtension( fileName.Replace( '/', '_' ) ) + ".png";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}
				new ExportToPngCommand( bitmap, dialog.FileName ).Execute();
			}
		}

		private void ExportAllTextures( object sender, EventArgs args )
		{
			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to export all textures into (subfolders mirror the resource paths, e.g. art\\...).";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				Cursor.Current = Cursors.WaitCursor;
				try
				{
					var result = new BatchExportTexturesPngCommand( this.LPAKFile, dialog.SelectedPath, resourcePaths: null ).Execute();
					if( !result.IsSuccess )
					{
						MessageBox.Show( this, result.Error, "Export all textures", MessageBoxButtons.OK, MessageBoxIcon.Warning );
					}
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ImportAllTextures( object sender, EventArgs args )
		{
			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to import texture PNGs from (subfolders must mirror the resource paths, as written by the batch export).";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				Cursor.Current = Cursors.WaitCursor;
				try
				{
					var result = new BatchImportTexturesPngCommand( this.LPAKFile, dialog.SelectedPath, resourcePaths: null ).Execute();
					if( !result.IsSuccess )
					{
						MessageBox.Show( this, result.Error, "Import all textures", MessageBoxButtons.OK, MessageBoxIcon.Warning );
					}
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ImportTexturePng( object sender, EventArgs args )
		{
			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return;
			}

			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = string.Concat( "Import PNG as ", fileName );
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				new ImportTexturePngCommand( this.LPAKFile, fileName, dialog.FileName ).Execute();
			}
		}

		private void SaveAs( object sender, EventArgs args )
		{
			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return;
			}

			var entry = this.LPAKFile.PakFileEntries[fileIndex];
			if( entry == null )
			{
				return;
			}

			var bytes = new byte[entry.SizeOfData1];
			Helper.ReadBinaryFile( this.LPAKFile.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = entry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
				bytes = reader.ReadBytes( entry.SizeOfData1 );
			} );

			new ExportToBinaryWithDialogCommand( bytes ).Execute();
		}
		
		private void ExportOverrideXML( object sender, EventArgs args )
		{
			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName )
			   || fileName is null
			   )
			{
				return;
			}

			var isCostume = fileName.EndsWith( ".costume.xml" );
			var isRoom = fileName.EndsWith( ".room.xml" );

			if( !isCostume && !isRoom )
			{
				return;
			}

			var entry = this.LPAKFile.PakFileEntries[fileIndex];
			if( entry == null )
			{
				return;
			}

			object? data = null;
			if( isCostume )
			{
				Helper.ReadBinaryFile( this.LPAKFile.FileNameOnDisk, reader =>
				{
					reader.BaseStream.Position = entry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
					data = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Parser.ReadCostume( reader );
				} );
			} else if( isRoom)
			{
				Helper.ReadBinaryFile( this.LPAKFile.FileNameOnDisk, reader =>
				{
					reader.BaseStream.Position = entry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
					data = MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser.ReadRoom( reader );
				} );
			}

			if( data == null )
			{
				return;
			}

			new ExportToOverrideXmlCommand( this.LPAKFile.FileNameOnDisk, fileName, data ).Execute();
		}

		private void ApplyOverride( object sender, EventArgs args )
		{
			var selectedNode = this.contextMenuStrip.Tag as TreeNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileIndex = (int)selectedNode.Tag;
			if( fileIndex < 0 || fileIndex >= this.LPAKFile.PakFileNames.Length )
			{
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName )
			   || fileName is null
			  )
			{
				return;
			}

			var isCostume = fileName.EndsWith( ".costume.xml" );
			var isRoom = fileName.EndsWith( ".room.xml" );

			if( !isCostume && !isRoom )
			{
				return;
			}

			new ApplyOverrideCommand( this.LPAKFile.FileNameOnDisk, fileName ).Execute();
		}
	}
}
