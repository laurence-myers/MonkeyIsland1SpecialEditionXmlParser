using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class LPAKForm : Form
	{
		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}

		public LPAKForm()
		{
			this.InitializeComponent();
		}

		private void HandleFormLoad( object sender, EventArgs args )
		{
			var rootNode = new TreeNode()
			{
				Text = Path.GetFileName( this.FileName ),
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
			string languageCode = null;
			string language = null;

			fileName = Path.GetFileNameWithoutExtension( fileName );
			fileName = fileName.Substring( 0, fileName.LastIndexOf( '.' ) );

			if( fileName[2] == '.' )
			{
				languageCode = fileName.Substring( 0, 2 );
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
				builder.Append( " (", language, ")" );
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

		private void HandleMouseDoubleClick( object sender, TreeNodeMouseClickEventArgs args )
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

		private void HandleLevel1BeforeExpand( TreeNode node )
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
				if( string.IsNullOrWhiteSpace( fileName ) )
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
				// other folder
				else if( node.Text == "Other" && !fileName.EndsWith( ".dxt" ) && !fileName.EndsWith( ".png" ) )
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
					Command.OpenCostumeForm.LPAKFile = this.LPAKFile;
					Command.OpenCostumeForm.FileName = fileName;
					Command.OpenCostumeForm.FileIndex = fileIndex;
					Command.OpenCostumeForm.Execute();
					break;
				case "Rooms":
					Command.OpenRoomForm.LPAKFile = this.LPAKFile;
					Command.OpenRoomForm.FileName = fileName;
					Command.OpenRoomForm.FileIndex = fileIndex;
					Command.OpenRoomForm.Execute();
					break;
				case "Shaders":
					Command.OpenShaderForm.LPAKFile = this.LPAKFile;
					Command.OpenShaderForm.FileName = fileName;
					Command.OpenShaderForm.FileIndex = fileIndex;
					Command.OpenShaderForm.Execute();
					break;
				default:
					Command.OpenHexForm.LPAKFile = this.LPAKFile;
					Command.OpenHexForm.FileName = fileName;
					Command.OpenHexForm.FileIndex = fileIndex;
					Command.OpenHexForm.Execute();
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

		private void HandleBeforeExpand( object sender, TreeViewCancelEventArgs args )
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
	}
}
