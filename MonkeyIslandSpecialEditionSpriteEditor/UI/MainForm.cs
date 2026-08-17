using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class MainForm : System.Windows.Forms.Form, IDisposable
	{
		private static MainForm? _instance;
		private ToolStripMenuItem? autoWriteMenuItem;

		public static MainForm Instance
		{
			get
			{
				MainForm._instance ??= new MainForm();
				return MainForm._instance;
			}
			private set
			{
				MainForm._instance = value;
			}
		}

		private MainForm()
		{
			this.InitializeComponent();
			this.InitializeGameMenu();

			if( !UserSettings.Instance.DontShowQuickStart )
			{
				this.Paint += this.OpenQuickStartDialog;
			}
		}

		//-------------------------------------------
		// play-test loop (the real Special Edition game + loose overrides)

		/// <summary>
		/// Builds the "Game" menu: Test in game (F5) writes every unsaved edit as a loose override and
		/// launches or focuses the SE, which re-reads a room's overrides whenever the room is entered;
		/// an auto-write toggle makes every edit land on disk by itself; and the overrides manager
		/// lists and reverts what the game is loading. Built in code so the designer stays untouched.
		/// </summary>
		private void InitializeGameMenu()
		{
			var testItem = new ToolStripMenuItem( "&Test in game" )
			{
				ShortcutKeys = Keys.F5,
				ShowShortcutKeys = true,
				ToolTipText = "Write unsaved room/costume edits as overrides, then start or switch to the game. In game, leave the room and come back to see them.",
			};
			testItem.Click += delegate { new TestInGameCommand( this.ResolvePakDirectory() ).Execute(); };

			this.autoWriteMenuItem = new ToolStripMenuItem( "&Auto-write overrides on edit" )
			{
				CheckOnClick = true,
				Checked = UserSettings.Instance.AutoWriteOverrides,
				ToolTipText = "Every edit is written to the game folder shortly after you make it - just re-enter the room in game.",
			};
			this.autoWriteMenuItem.CheckedChanged += delegate
			{
				UserSettings.Instance.AutoWriteOverrides = this.autoWriteMenuItem.Checked;
				UserSettings.Instance.Save();
				this.SetStatusText( this.autoWriteMenuItem.Checked
					? "Auto-write on: edits are written to the game folder as you make them."
					: "Auto-write off: use Save override or Test in game (F5) to write edits." );
			};

			var overridesItem = new ToolStripMenuItem( "&Loose overrides..." )
			{
				ToolTipText = "See and revert every loose override file the game loads instead of the pak.",
			};
			overridesItem.Click += delegate { this.ShowOverrides(); };

			var gameMenu = new ToolStripMenuItem( "&Game" );
			gameMenu.DropDownItems.Add( testItem );
			gameMenu.DropDownItems.Add( this.autoWriteMenuItem );
			gameMenu.DropDownItems.Add( new ToolStripSeparator() );
			gameMenu.DropDownItems.Add( overridesItem );

			// between File and Window
			this.menuStrip1.Items.Insert( 1, gameMenu );
		}

		private void ShowOverrides()
		{
			var lpak = this.ResolveLpak();
			var pakDirectory = this.ResolvePakDirectory();
			if( string.IsNullOrEmpty( pakDirectory ) )
			{
				this.SetStatusText( "Open a pak first - the overrides live in its game folder." );
				return;
			}

			Func<string, bool>? isInPak = null;
			if( lpak != null )
			{
				var names = new System.Collections.Generic.HashSet<string>(
					lpak.PakFileNames.Select( n => ( n.FileName ?? "" ).Replace( '\\', '/' ) ), StringComparer.OrdinalIgnoreCase );
				isInPak = names.Contains;
			}

			using( var form = new OverridesForm( pakDirectory!, isInPak ) )
			{
				form.ShowDialog( this );
			}
		}

		/// <summary>The pak of the active window, else of any open window, else null.</summary>
		private LPAKFile? ResolveLpak()
		{
			LPAKFile? FromForm( Form? form )
			{
				switch( form )
				{
					case LPAKForm explorer:
						return explorer.LPAKFile;
					case SpriteSheetEditorForm room:
						return room.LPAKFile;
					case CostumeSpriteSheetEditorForm costume:
						return costume.LPAKFile;
					default:
						return null;
				}
			}

			return FromForm( this.ActiveMdiChild ) ?? this.MdiChildren.Select( FromForm ).FirstOrDefault( l => l != null );
		}

		/// <summary>
		/// The game folder the overrides go to: the open pak's folder, else the folder of the most
		/// recently opened pak (so F5 works even before a window is open), else null.
		/// </summary>
		private string? ResolvePakDirectory()
		{
			var lpak = this.ResolveLpak();
			var path = lpak?.FileNameOnDisk ?? UserSettings.Instance.RecentLPAKFileNames?.FirstOrDefault();
			return string.IsNullOrEmpty( path ) ? null : Path.GetDirectoryName( path );
		}

		public new void Dispose()
		{
			base.Dispose();
			MainForm._instance = null;
		}

		private void OpenQuickStartDialog( object sender, EventArgs args )
		{
			this.Paint -= this.OpenQuickStartDialog;
			new OpenQuickStartDialogCommand().Execute();
		}

		private void OpenFileWithDialog( object sender, EventArgs e )
		{
			new OpenFileWithDialogCommand().Execute();
		}

		private void ExitApplication( object sender, EventArgs e )
		{
			Application.Exit();
		}

		private void ShowAboutForm( object sender, EventArgs e )
		{
			using( var aboutForm = new AboutBox() )
			{
				aboutForm.ShowDialog( this );
			}
		}

		private void UpdateRecentMenuItems( object sender, EventArgs args )
		{
			this.recentToolStripMenuItem.DropDownItems.Clear();
			if( UserSettings.Instance.RecentLPAKFileNames != null )
			{
				foreach( var lpakFileName in UserSettings.Instance.RecentLPAKFileNames )
				{
					var item = new ToolStripMenuItem()
					{
						Text = lpakFileName,
						Tag = "recent",
					};
					item.Click += delegate
					{
						new OpenFileCommand( item.Text ).Execute();
					};
					this.recentToolStripMenuItem.DropDownItems.Add( item );
				}
			}
			else
			{
				this.recentToolStripMenuItem.DropDownItems.Add( "dummy" );
			}
		}

		public void SetStatusText( string text )
		{
			this.toolStripStatusLabel.Text = text;
		}
	}
}
