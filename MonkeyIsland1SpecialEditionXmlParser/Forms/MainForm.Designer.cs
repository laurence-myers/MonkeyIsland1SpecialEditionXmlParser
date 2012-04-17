namespace MonkeyIsland1SpecialEditionXmlParser.Forms
{
	partial class MainForm
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
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.costumeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asXMLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.asPNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.animationColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.spritesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.framesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.exportButtonColumn = new System.Windows.Forms.DataGridViewButtonColumn();
			this.exportAsPngFilesDialog = new System.Windows.Forms.SaveFileDialog();
			this.exportAsXmlFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.menuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(776, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripMenuItem2,
            this.exportToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(31, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.costumeToolStripMenuItem});
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.openToolStripMenuItem.Text = "&Open";
			// 
			// costumeToolStripMenuItem
			// 
			this.costumeToolStripMenuItem.Name = "costumeToolStripMenuItem";
			this.costumeToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
			this.costumeToolStripMenuItem.Text = "&Costume file...";
			this.costumeToolStripMenuItem.Click += new System.EventHandler(this.ShowOpenCostumeFileDialog);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(149, 6);
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.asXMLToolStripMenuItem,
            this.asPNGToolStripMenuItem});
			this.exportToolStripMenuItem.Enabled = false;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// asXMLToolStripMenuItem
			// 
			this.asXMLToolStripMenuItem.Enabled = false;
			this.asXMLToolStripMenuItem.Name = "asXMLToolStripMenuItem";
			this.asXMLToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.asXMLToolStripMenuItem.Text = "As &XML";
			this.asXMLToolStripMenuItem.Click += new System.EventHandler(this.ExportAsXmlFile);
			// 
			// asPNGToolStripMenuItem
			// 
			this.asPNGToolStripMenuItem.Enabled = false;
			this.asPNGToolStripMenuItem.Name = "asPNGToolStripMenuItem";
			this.asPNGToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.asPNGToolStripMenuItem.Text = "As &PNG";
			this.asPNGToolStripMenuItem.Click += new System.EventHandler(this.ExportAllAsPngFiles);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitApplication);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.AddExtension = false;
			this.openFileDialog1.DefaultExt = "xml";
			this.openFileDialog1.FileName = "openFileDialog1";
			this.openFileDialog1.Filter = "XML files|*.xml|All files|*.*";
			this.openFileDialog1.SupportMultiDottedExtensions = true;
			this.openFileDialog1.Title = "Open Costume file";
			// 
			// dataGridView1
			// 
			this.dataGridView1.AllowUserToAddRows = false;
			this.dataGridView1.AllowUserToDeleteRows = false;
			this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.animationColumn,
            this.spritesColumn,
            this.framesColumn,
            this.exportButtonColumn});
			this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridView1.Location = new System.Drawing.Point(0, 24);
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.ReadOnly = true;
			this.dataGridView1.Size = new System.Drawing.Size(776, 629);
			this.dataGridView1.TabIndex = 1;
			// 
			// animationColumn
			// 
			this.animationColumn.HeaderText = "Animation";
			this.animationColumn.Name = "animationColumn";
			this.animationColumn.ReadOnly = true;
			// 
			// spritesColumn
			// 
			this.spritesColumn.HeaderText = "Sprites";
			this.spritesColumn.Name = "spritesColumn";
			this.spritesColumn.ReadOnly = true;
			// 
			// framesColumn
			// 
			this.framesColumn.HeaderText = "Frames";
			this.framesColumn.Name = "framesColumn";
			this.framesColumn.ReadOnly = true;
			// 
			// exportButtonColumn
			// 
			this.exportButtonColumn.HeaderText = "Export";
			this.exportButtonColumn.Name = "exportButtonColumn";
			this.exportButtonColumn.ReadOnly = true;
			// 
			// exportAsPngFilesDialog
			// 
			this.exportAsPngFilesDialog.DefaultExt = "png";
			this.exportAsPngFilesDialog.Filter = "PNG files|*.png|All files|*.*";
			this.exportAsPngFilesDialog.SupportMultiDottedExtensions = true;
			this.exportAsPngFilesDialog.Title = "Export as PNG files";
			// 
			// exportAsXmlFileDialog
			// 
			this.exportAsXmlFileDialog.DefaultExt = "xml";
			this.exportAsXmlFileDialog.Filter = "XML files|*.xml|All files|*.*";
			this.exportAsXmlFileDialog.SupportMultiDottedExtensions = true;
			this.exportAsXmlFileDialog.Title = "Export as XML file";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(776, 653);
			this.Controls.Add(this.dataGridView1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "MainForm";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem costumeToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem asXMLToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.DataGridViewTextBoxColumn animationColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn spritesColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn framesColumn;
		private System.Windows.Forms.DataGridViewButtonColumn exportButtonColumn;
		private System.Windows.Forms.SaveFileDialog exportAsPngFilesDialog;
		private System.Windows.Forms.ToolStripMenuItem asPNGToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog exportAsXmlFileDialog;
	}
}