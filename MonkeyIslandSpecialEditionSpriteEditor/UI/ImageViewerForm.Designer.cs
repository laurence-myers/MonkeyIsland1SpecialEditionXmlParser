namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	partial class ImageViewerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asPNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fromPNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panelPreview = new MonkeyIslandSpecialEditionSpriteEditor.UI.NonScrollingPanel();
			this.spriteSetPreviewControl = new MonkeyIslandSpecialEditionSpriteEditor.UI.SpriteSetPreviewControl();
			this.panelZoom = new System.Windows.Forms.Panel();
			this.trackBarZoom = new System.Windows.Forms.TrackBar();
			this.labelZoom = new System.Windows.Forms.Label();
			this.menuStrip1.SuspendLayout();
			this.panelPreview.SuspendLayout();
			this.panelZoom.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarZoom)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoEllipsis = true;
			this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.ForeColor = System.Drawing.SystemColors.ButtonFace;
			this.label1.Location = new System.Drawing.Point(0, 24);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(3);
			this.label1.Size = new System.Drawing.Size(595, 23);
			this.label1.TabIndex = 7;
			this.label1.Text = "Texture";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(595, 24);
			this.menuStrip1.TabIndex = 9;
			this.menuStrip1.Text = "menuStrip1";
			this.menuStrip1.Visible = false;
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportToolStripMenuItem,
            this.importToolStripMenuItem});
			this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(31, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.asPNGToolStripMenuItem});
			this.exportToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Replace;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			//
			// asPNGToolStripMenuItem
			//
			this.asPNGToolStripMenuItem.Name = "asPNGToolStripMenuItem";
			this.asPNGToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.asPNGToolStripMenuItem.Text = "As PNG...";
			this.asPNGToolStripMenuItem.Click += new System.EventHandler(this.ExportAsPng);
			//
			// importToolStripMenuItem
			//
			this.importToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fromPNGToolStripMenuItem});
			this.importToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.importToolStripMenuItem.MergeIndex = 3;
			this.importToolStripMenuItem.Name = "importToolStripMenuItem";
			this.importToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.importToolStripMenuItem.Text = "&Import";
			//
			// fromPNGToolStripMenuItem
			//
			this.fromPNGToolStripMenuItem.Name = "fromPNGToolStripMenuItem";
			this.fromPNGToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.fromPNGToolStripMenuItem.Text = "From PNG...";
			this.fromPNGToolStripMenuItem.Click += new System.EventHandler(this.ImportFromPng);
			//
			// panelPreview
			//
			this.panelPreview.AutoScroll = true;
			this.panelPreview.Controls.Add(this.spriteSetPreviewControl);
			this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPreview.Location = new System.Drawing.Point(0, 47);
			this.panelPreview.Name = "panelPreview";
			this.panelPreview.Size = new System.Drawing.Size(595, 390);
			this.panelPreview.TabIndex = 10;
			//
			// spriteSetPreviewControl
			//
			this.spriteSetPreviewControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.spriteSetPreviewControl.Location = new System.Drawing.Point(0, 0);
			this.spriteSetPreviewControl.Name = "spriteSetPreviewControl";
			this.spriteSetPreviewControl.Size = new System.Drawing.Size(50, 50);
			this.spriteSetPreviewControl.TabIndex = 8;
			this.spriteSetPreviewControl.Text = "spriteSetPreviewControl1";
			//
			// panelZoom
			//
			this.panelZoom.Controls.Add(this.labelZoom);
			this.panelZoom.Controls.Add(this.trackBarZoom);
			this.panelZoom.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelZoom.Location = new System.Drawing.Point(0, 437);
			this.panelZoom.Name = "panelZoom";
			this.panelZoom.Size = new System.Drawing.Size(595, 38);
			this.panelZoom.TabIndex = 11;
			//
			// trackBarZoom
			//
			this.trackBarZoom.AutoSize = false;
			this.trackBarZoom.LargeChange = 1;
			this.trackBarZoom.Location = new System.Drawing.Point(4, 3);
			this.trackBarZoom.Maximum = 5;
			this.trackBarZoom.Name = "trackBarZoom";
			this.trackBarZoom.Size = new System.Drawing.Size(240, 32);
			this.trackBarZoom.TabIndex = 0;
			this.trackBarZoom.TickStyle = System.Windows.Forms.TickStyle.BottomRight;
			this.trackBarZoom.Value = 3;
			//
			// labelZoom
			//
			this.labelZoom.AutoSize = true;
			this.labelZoom.Location = new System.Drawing.Point(252, 11);
			this.labelZoom.Name = "labelZoom";
			this.labelZoom.Size = new System.Drawing.Size(33, 13);
			this.labelZoom.TabIndex = 1;
			this.labelZoom.Text = "100%";
			//
			// ImageViewerForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(595, 475);
			this.Controls.Add(this.panelPreview);
			this.Controls.Add(this.panelZoom);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(380, 220);
			this.Name = "ImageViewerForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.panelPreview.ResumeLayout(false);
			this.panelZoom.ResumeLayout(false);
			this.panelZoom.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarZoom)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private NonScrollingPanel panelPreview;
		private SpriteSetPreviewControl spriteSetPreviewControl;
		private System.Windows.Forms.Panel panelZoom;
		private System.Windows.Forms.TrackBar trackBarZoom;
		private System.Windows.Forms.Label labelZoom;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem asPNGToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fromPNGToolStripMenuItem;
	}
}