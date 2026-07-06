namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class RoomForm
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toXMLFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asPNGFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toPNGFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openSpriteSheetEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.calibrationOverlayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.warningLabel = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panelGroups = new System.Windows.Forms.Panel();
			this.checkedListBoxGroups = new System.Windows.Forms.CheckedListBox();
			this.labelGroups = new System.Windows.Forms.Label();
			this.roomPreviewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.RoomPreviewControl();
			this.menuStrip1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panelGroups.SuspendLayout();
			this.SuspendLayout();
			//
			// menuStrip1
			//
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(584, 24);
			this.menuStrip1.TabIndex = 4;
			this.menuStrip1.Text = "menuStrip1";
			this.menuStrip1.Visible = false;
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportToolStripMenuItem,
            this.openSpriteSheetEditorToolStripMenuItem});
			this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			//
			// exportToolStripMenuItem
			//
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toXMLFileToolStripMenuItem,
            this.asPNGFilesToolStripMenuItem,
            this.toPNGFilesToolStripMenuItem});
			this.exportToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Replace;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			//
			// toXMLFileToolStripMenuItem
			//
			this.toXMLFileToolStripMenuItem.Name = "toXMLFileToolStripMenuItem";
			this.toXMLFileToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.toXMLFileToolStripMenuItem.Text = "As &XML file...";
			this.toXMLFileToolStripMenuItem.Click += new System.EventHandler(this.ExportAsXml);
			//
			// asPNGFilesToolStripMenuItem
			//
			this.asPNGFilesToolStripMenuItem.Name = "asPNGFilesToolStripMenuItem";
			this.asPNGFilesToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.asPNGFilesToolStripMenuItem.Text = "As PNG files...";
			this.asPNGFilesToolStripMenuItem.Click += new System.EventHandler(this.ExportAsPng);
			//
			// toPNGFilesToolStripMenuItem
			//
			this.toPNGFilesToolStripMenuItem.Name = "toPNGFilesToolStripMenuItem";
			this.toPNGFilesToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.toPNGFilesToolStripMenuItem.Text = "As merged PNG file...";
			this.toPNGFilesToolStripMenuItem.Click += new System.EventHandler(this.ExportAsMergedPng);
			//
			// openSpriteSheetEditorToolStripMenuItem
			//
			this.openSpriteSheetEditorToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.openSpriteSheetEditorToolStripMenuItem.MergeIndex = 1;
			this.openSpriteSheetEditorToolStripMenuItem.Name = "openSpriteSheetEditorToolStripMenuItem";
			this.openSpriteSheetEditorToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.openSpriteSheetEditorToolStripMenuItem.Text = "Open in Spritesheet &Editor";
			this.openSpriteSheetEditorToolStripMenuItem.Click += new System.EventHandler(this.OpenSpriteSheetEditor);
			//
			// viewToolStripMenuItem
			//
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calibrationOverlayToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.viewToolStripMenuItem.Text = "&View";
			//
			// calibrationOverlayToolStripMenuItem
			//
			this.calibrationOverlayToolStripMenuItem.CheckOnClick = true;
			this.calibrationOverlayToolStripMenuItem.Name = "calibrationOverlayToolStripMenuItem";
			this.calibrationOverlayToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.calibrationOverlayToolStripMenuItem.Text = "&Calibration overlay";
			this.calibrationOverlayToolStripMenuItem.Click += new System.EventHandler(this.ToggleCalibrationOverlay);
			//
			// label1
			//
			this.label1.AutoEllipsis = true;
			this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.ForeColor = System.Drawing.SystemColors.ButtonFace;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(3);
			this.label1.Size = new System.Drawing.Size(584, 23);
			this.label1.TabIndex = 6;
			this.label1.Text = "Room";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// warningLabel
			//
			this.warningLabel.AutoEllipsis = true;
			this.warningLabel.BackColor = System.Drawing.Color.Goldenrod;
			this.warningLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this.warningLabel.ForeColor = System.Drawing.Color.Black;
			this.warningLabel.Location = new System.Drawing.Point(0, 23);
			this.warningLabel.Name = "warningLabel";
			this.warningLabel.Padding = new System.Windows.Forms.Padding(3);
			this.warningLabel.Size = new System.Drawing.Size(584, 23);
			this.warningLabel.TabIndex = 8;
			this.warningLabel.Text = "Warning";
			this.warningLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.warningLabel.Visible = false;
			//
			// panel1
			//
			this.panel1.AutoScroll = true;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.roomPreviewControl);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(160, 46);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(424, 216);
			this.panel1.TabIndex = 7;
			//
			// panelGroups
			//
			this.panelGroups.Controls.Add(this.checkedListBoxGroups);
			this.panelGroups.Controls.Add(this.labelGroups);
			this.panelGroups.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelGroups.Location = new System.Drawing.Point(0, 46);
			this.panelGroups.Name = "panelGroups";
			this.panelGroups.Size = new System.Drawing.Size(160, 216);
			this.panelGroups.TabIndex = 9;
			//
			// checkedListBoxGroups
			//
			this.checkedListBoxGroups.CheckOnClick = true;
			this.checkedListBoxGroups.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkedListBoxGroups.IntegralHeight = false;
			this.checkedListBoxGroups.Location = new System.Drawing.Point(0, 18);
			this.checkedListBoxGroups.Name = "checkedListBoxGroups";
			this.checkedListBoxGroups.Size = new System.Drawing.Size(160, 198);
			this.checkedListBoxGroups.TabIndex = 0;
			this.checkedListBoxGroups.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.HandleGroupItemCheck);
			//
			// labelGroups
			//
			this.labelGroups.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelGroups.Location = new System.Drawing.Point(0, 0);
			this.labelGroups.Name = "labelGroups";
			this.labelGroups.Padding = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.labelGroups.Size = new System.Drawing.Size(160, 18);
			this.labelGroups.TabIndex = 1;
			this.labelGroups.Text = "Sprite groups";
			//
			// roomPreviewControl
			//
			this.roomPreviewControl.Location = new System.Drawing.Point(0, 0);
			this.roomPreviewControl.Name = "roomPreviewControl";
			this.roomPreviewControl.Size = new System.Drawing.Size(282, 214);
			this.roomPreviewControl.TabIndex = 6;
			this.roomPreviewControl.Text = "roomPreviewControl";
			//
			// RoomForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 262);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panelGroups);
			this.Controls.Add(this.warningLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "RoomForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "RoomForm";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panelGroups.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toXMLFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toPNGFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openSpriteSheetEditorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem calibrationOverlayToolStripMenuItem;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label warningLabel;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panelGroups;
		private System.Windows.Forms.CheckedListBox checkedListBoxGroups;
		private System.Windows.Forms.Label labelGroups;
		private RoomPreviewControl roomPreviewControl;
		private System.Windows.Forms.ToolStripMenuItem asPNGFilesToolStripMenuItem;
	}
}
