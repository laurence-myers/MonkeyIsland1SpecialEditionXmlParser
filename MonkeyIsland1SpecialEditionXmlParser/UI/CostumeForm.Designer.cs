namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class CostumeForm
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
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.animationColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.spritesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.framesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.exportButtonColumn = new System.Windows.Forms.DataGridViewButtonColumn();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toXMLFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toPNGFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
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
			this.dataGridView1.Size = new System.Drawing.Size(737, 545);
			this.dataGridView1.TabIndex = 2;
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
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(737, 24);
			this.menuStrip1.TabIndex = 3;
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
			this.toXMLFileToolStripMenuItem.Click += new System.EventHandler(this.ExportAsXmlFile);
			// 
			// toPNGFilesToolStripMenuItem
			// 
			this.toPNGFilesToolStripMenuItem.Name = "toPNGFilesToolStripMenuItem";
			this.toPNGFilesToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
			this.toPNGFilesToolStripMenuItem.Text = "As PNG files...";
			this.toPNGFilesToolStripMenuItem.Click += new System.EventHandler(this.ExportAllAsPngFiles);
			// 
			// CostumeForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(737, 569);
			this.Controls.Add(this.dataGridView1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "CostumeForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.DataGridViewTextBoxColumn animationColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn spritesColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn framesColumn;
		private System.Windows.Forms.DataGridViewButtonColumn exportButtonColumn;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toXMLFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toPNGFilesToolStripMenuItem;
	}
}