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
			this.toPNGFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.spriteSetPreviewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.SpriteSetPreviewControl();
			this.menuStrip1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(284, 24);
			this.menuStrip1.TabIndex = 4;
			this.menuStrip1.Text = "menuStrip1";
			this.menuStrip1.Visible = false;
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportToolStripMenuItem});
			this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(31, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toXMLFileToolStripMenuItem,
            this.toPNGFilesToolStripMenuItem});
			this.exportToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Replace;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// toXMLFileToolStripMenuItem
			// 
			this.toXMLFileToolStripMenuItem.Name = "toXMLFileToolStripMenuItem";
			this.toXMLFileToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.toXMLFileToolStripMenuItem.Text = "As &XML file...";
			this.toXMLFileToolStripMenuItem.Click += new System.EventHandler(this.ExportAsXml);
			// 
			// toPNGFilesToolStripMenuItem
			// 
			this.toPNGFilesToolStripMenuItem.Name = "toPNGFilesToolStripMenuItem";
			this.toPNGFilesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.toPNGFilesToolStripMenuItem.Text = "As PNG files...";
			this.toPNGFilesToolStripMenuItem.Click += new System.EventHandler(this.ExportAsPng);
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
			this.label1.Size = new System.Drawing.Size(284, 23);
			this.label1.TabIndex = 6;
			this.label1.Text = "Room";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.spriteSetPreviewControl);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 47);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(284, 215);
			this.panel1.TabIndex = 7;
			// 
			// spriteSetPreviewControl
			// 
			this.spriteSetPreviewControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.spriteSetPreviewControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.spriteSetPreviewControl.Location = new System.Drawing.Point(0, 0);
			this.spriteSetPreviewControl.Name = "spriteSetPreviewControl";
			this.spriteSetPreviewControl.Size = new System.Drawing.Size(282, 213);
			this.spriteSetPreviewControl.Sprites = null;
			this.spriteSetPreviewControl.TabIndex = 6;
			this.spriteSetPreviewControl.Text = "spriteSetPreviewControl1";
			// 
			// RoomForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.Name = "RoomForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "RoomForm";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toXMLFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toPNGFilesToolStripMenuItem;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel1;
		private SpriteSetPreviewControl spriteSetPreviewControl;
	}
}