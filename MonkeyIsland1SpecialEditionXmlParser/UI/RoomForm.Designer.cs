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
			this.spriteSetPreviewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.SpriteSetPreviewControl();
			this.menuStrip1.SuspendLayout();
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
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// toXMLFileToolStripMenuItem
			// 
			this.toXMLFileToolStripMenuItem.Name = "toXMLFileToolStripMenuItem";
			this.toXMLFileToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
			this.toXMLFileToolStripMenuItem.Text = "As &XML file...";
			// 
			// toPNGFilesToolStripMenuItem
			// 
			this.toPNGFilesToolStripMenuItem.Name = "toPNGFilesToolStripMenuItem";
			this.toPNGFilesToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
			this.toPNGFilesToolStripMenuItem.Text = "As PNG files...";
			// 
			// spriteSetPreviewControl
			// 
			this.spriteSetPreviewControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.spriteSetPreviewControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.spriteSetPreviewControl.Location = new System.Drawing.Point(0, 0);
			this.spriteSetPreviewControl.Name = "spriteSetPreviewControl";
			this.spriteSetPreviewControl.Size = new System.Drawing.Size(284, 262);
			this.spriteSetPreviewControl.Sprites = null;
			this.spriteSetPreviewControl.TabIndex = 5;
			this.spriteSetPreviewControl.Text = "spriteSetPreviewControl1";
			// 
			// RoomForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.spriteSetPreviewControl);
			this.Controls.Add(this.menuStrip1);
			this.Name = "RoomForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "RoomForm";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toXMLFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toPNGFilesToolStripMenuItem;
		private SpriteSetPreviewControl spriteSetPreviewControl;
	}
}