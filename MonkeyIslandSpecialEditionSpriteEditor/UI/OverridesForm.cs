using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.PlayTest;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Shows every loose override the real game will pick up from the game folder - edited rooms
	/// and costumes, replaced and brand-new textures, the XML mirrors - and lets the modder revert
	/// any of them (delete the file, so the game falls back to the pak). These files change the
	/// installed game persistently, so this is the one place to see and undo all of them.
	/// </summary>
	public sealed class OverridesForm : Form
	{
		private readonly string pakDirectory;
		private readonly Func<string, bool>? isInPak;
		private readonly ListView list;
		private readonly Label summary;

		public OverridesForm( string pakDirectory, Func<string, bool>? isInPak )
		{
			this.pakDirectory = pakDirectory;
			this.isInPak = isInPak;

			this.Text = "Loose overrides - " + pakDirectory;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Size = new Size( 860, 480 );
			this.MinimumSize = new Size( 600, 300 );
			this.ShowIcon = false;
			this.MinimizeBox = false;

			this.list = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				HideSelection = false,
				MultiSelect = true,
			};
			this.list.Columns.Add( "File (relative to the game folder)", 430 );
			this.list.Columns.Add( "Kind", 90 );
			this.list.Columns.Add( "In pak", 70 );
			this.list.Columns.Add( "Size", 80, HorizontalAlignment.Right );
			this.list.Columns.Add( "Modified", 130 );

			var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 36, Padding = new Padding( 4 ) };
			var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
			var revert = new Button { Text = "Revert selected (delete)", AutoSize = true };
			revert.Click += delegate { this.RevertSelected(); };
			var refresh = new Button { Text = "Refresh", AutoSize = true };
			refresh.Click += delegate { this.Reload(); };
			var open = new Button { Text = "Open game folder", AutoSize = true };
			open.Click += delegate { Process.Start( new ProcessStartInfo( this.pakDirectory ) { UseShellExecute = true } )?.Dispose(); };
			buttons.Controls.Add( close );
			buttons.Controls.Add( revert );
			buttons.Controls.Add( refresh );
			buttons.Controls.Add( open );

			this.summary = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 40, Padding = new Padding( 6, 4, 6, 0 ) };

			this.Controls.Add( this.list );
			this.Controls.Add( buttons );
			this.Controls.Add( this.summary );
			this.CancelButton = close;

			this.Load += delegate { this.Reload(); };
		}

		private void Reload()
		{
			this.list.BeginUpdate();
			try
			{
				this.list.Items.Clear();
				var entries = OverrideScanner.Scan( this.pakDirectory, this.isInPak );
				foreach( var entry in entries )
				{
					var item = new ListViewItem( entry.RelativePath ) { Tag = entry };
					item.SubItems.Add( entry.Kind.ToString() );
					item.SubItems.Add( entry.InPak == null ? "" : entry.InPak.Value ? "replaces" : "NEW" );
					item.SubItems.Add( FormatSize( entry.Length ) );
					item.SubItems.Add( entry.LastWrite.ToString( "yyyy-MM-dd HH:mm" ) );
					if( entry.Kind == OverrideKind.Other )
					{
						item.ForeColor = SystemColors.GrayText;
					}

					this.list.Items.Add( item );
				}

				var live = entries.Count( e => e.Kind != OverrideKind.Other && e.Kind != OverrideKind.OverrideXml );
				this.summary.Text = entries.Count == 0
					? "No loose overrides - the game is running the pak untouched."
					: string.Concat( live, " override", live == 1 ? "" : "s", " the game loads (", entries.Count, " files in all). ",
						"The game re-reads a room's overrides each time the room is entered; deleting a file restores the pak's version." );
			}
			finally
			{
				this.list.EndUpdate();
			}
		}

		private void RevertSelected()
		{
			var selected = this.list.SelectedItems.Cast<ListViewItem>().Select( i => (LooseOverride)i.Tag ).ToList();
			if( selected.Count == 0 )
			{
				return;
			}

			var answer = MessageBox.Show(
				this,
				string.Concat( "Delete ", selected.Count, " file", selected.Count == 1 ? "" : "s", " so the game goes back to the pak's version?\r\n\r\n",
					string.Join( "\r\n", selected.Take( 12 ).Select( e => e.RelativePath ) ),
					selected.Count > 12 ? "\r\n..." : "" ),
				"Revert overrides",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning );
			if( answer != DialogResult.Yes )
			{
				return;
			}

			var errors = new List<string>();
			foreach( var entry in selected )
			{
				try
				{
					OverrideScanner.Revert( entry );
				}
				catch( Exception exception )
				{
					errors.Add( entry.RelativePath + ": " + exception.Message );
				}
			}

			if( errors.Count > 0 )
			{
				MessageBox.Show( this, string.Join( "\r\n", errors ), "Some files could not be deleted", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

			this.Reload();
		}

		private static string FormatSize( long bytes )
		{
			if( bytes >= 1024 * 1024 )
			{
				return ( bytes / ( 1024.0 * 1024.0 ) ).ToString( "0.0" ) + " MB";
			}

			return bytes >= 1024 ? ( bytes / 1024.0 ).ToString( "0" ) + " KB" : bytes + " B";
		}
	}
}
