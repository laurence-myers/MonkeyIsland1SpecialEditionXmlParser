namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class XmlExportDialog
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
			this.components = new System.ComponentModel.Container();
			this.panel1 = new System.Windows.Forms.Panel();
			this.exportFileNameTextBox = new System.Windows.Forms.TextBox();
			this.exportFileNameButton = new System.Windows.Forms.Button();
			this.exportFileNameListButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.xsltFileNameTextBox = new System.Windows.Forms.TextBox();
			this.xsltFileNameButton = new System.Windows.Forms.Button();
			this.xsltListButton = new System.Windows.Forms.Button();
			this.panel4 = new System.Windows.Forms.Panel();
			this.exportButton = new System.Windows.Forms.Button();
			this.panel5 = new System.Windows.Forms.Panel();
			this.cancelButton = new System.Windows.Forms.Button();
			this.ExportFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.XsltFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.xsltContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cleanupxsltToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportFileNameContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel4.SuspendLayout();
			this.xsltContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.exportFileNameTextBox);
			this.panel1.Controls.Add(this.exportFileNameButton);
			this.panel1.Controls.Add(this.exportFileNameListButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(10, 23);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(3);
			this.panel1.Size = new System.Drawing.Size(814, 26);
			this.panel1.TabIndex = 0;
			// 
			// exportFileNameTextBox
			// 
			this.exportFileNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.exportFileNameTextBox.Location = new System.Drawing.Point(3, 3);
			this.exportFileNameTextBox.Name = "exportFileNameTextBox";
			this.exportFileNameTextBox.Size = new System.Drawing.Size(736, 20);
			this.exportFileNameTextBox.TabIndex = 3;
			this.exportFileNameTextBox.TextChanged += new System.EventHandler(this.ExportFileNameChanged);
			// 
			// exportFileNameButton
			// 
			this.exportFileNameButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.exportFileNameButton.Location = new System.Drawing.Point(739, 3);
			this.exportFileNameButton.Name = "exportFileNameButton";
			this.exportFileNameButton.Size = new System.Drawing.Size(50, 20);
			this.exportFileNameButton.TabIndex = 0;
			this.exportFileNameButton.Text = "...";
			this.exportFileNameButton.UseVisualStyleBackColor = true;
			this.exportFileNameButton.Click += new System.EventHandler(this.SelectExportFileName);
			// 
			// exportFileNameListButton
			// 
			this.exportFileNameListButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.exportFileNameListButton.Font = new System.Drawing.Font("Webdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
			this.exportFileNameListButton.Location = new System.Drawing.Point(789, 3);
			this.exportFileNameListButton.Name = "exportFileNameListButton";
			this.exportFileNameListButton.Size = new System.Drawing.Size(22, 20);
			this.exportFileNameListButton.TabIndex = 5;
			this.exportFileNameListButton.Text = "6";
			this.exportFileNameListButton.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			this.exportFileNameListButton.UseVisualStyleBackColor = true;
			this.exportFileNameListButton.Click += new System.EventHandler(this.ShowExportFileNameList);
			// 
			// label1
			// 
			this.label1.AutoEllipsis = true;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Location = new System.Drawing.Point(10, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(814, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Export file name";
			// 
			// panel3
			// 
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(10, 49);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(814, 7);
			this.panel3.TabIndex = 6;
			// 
			// label2
			// 
			this.label2.AutoEllipsis = true;
			this.label2.Dock = System.Windows.Forms.DockStyle.Top;
			this.label2.Location = new System.Drawing.Point(10, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(814, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "XSLT file name (optional)";
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.xsltFileNameTextBox);
			this.panel2.Controls.Add(this.xsltFileNameButton);
			this.panel2.Controls.Add(this.xsltListButton);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(10, 69);
			this.panel2.Name = "panel2";
			this.panel2.Padding = new System.Windows.Forms.Padding(3);
			this.panel2.Size = new System.Drawing.Size(814, 26);
			this.panel2.TabIndex = 8;
			// 
			// xsltFileNameTextBox
			// 
			this.xsltFileNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.xsltFileNameTextBox.Location = new System.Drawing.Point(3, 3);
			this.xsltFileNameTextBox.Name = "xsltFileNameTextBox";
			this.xsltFileNameTextBox.Size = new System.Drawing.Size(736, 20);
			this.xsltFileNameTextBox.TabIndex = 3;
			this.xsltFileNameTextBox.TextChanged += new System.EventHandler(this.XsltFileNameChanged);
			// 
			// xsltFileNameButton
			// 
			this.xsltFileNameButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.xsltFileNameButton.Location = new System.Drawing.Point(739, 3);
			this.xsltFileNameButton.Name = "xsltFileNameButton";
			this.xsltFileNameButton.Size = new System.Drawing.Size(50, 20);
			this.xsltFileNameButton.TabIndex = 0;
			this.xsltFileNameButton.Text = "…";
			this.xsltFileNameButton.UseVisualStyleBackColor = true;
			this.xsltFileNameButton.Click += new System.EventHandler(this.SelectXsltFileName);
			// 
			// xsltListButton
			// 
			this.xsltListButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.xsltListButton.Font = new System.Drawing.Font("Webdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
			this.xsltListButton.Location = new System.Drawing.Point(789, 3);
			this.xsltListButton.Name = "xsltListButton";
			this.xsltListButton.Size = new System.Drawing.Size(22, 20);
			this.xsltListButton.TabIndex = 4;
			this.xsltListButton.Text = "6";
			this.xsltListButton.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			this.xsltListButton.UseVisualStyleBackColor = true;
			this.xsltListButton.Click += new System.EventHandler(this.ShowXsltList);
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.exportButton);
			this.panel4.Controls.Add(this.panel5);
			this.panel4.Controls.Add(this.cancelButton);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel4.Location = new System.Drawing.Point(10, 96);
			this.panel4.Name = "panel4";
			this.panel4.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.panel4.Size = new System.Drawing.Size(814, 26);
			this.panel4.TabIndex = 9;
			// 
			// exportButton
			// 
			this.exportButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.exportButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.exportButton.Enabled = false;
			this.exportButton.Location = new System.Drawing.Point(657, 3);
			this.exportButton.Name = "exportButton";
			this.exportButton.Size = new System.Drawing.Size(75, 23);
			this.exportButton.TabIndex = 2;
			this.exportButton.Text = "&Export";
			this.exportButton.UseVisualStyleBackColor = true;
			// 
			// panel5
			// 
			this.panel5.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel5.Location = new System.Drawing.Point(732, 3);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(7, 23);
			this.panel5.TabIndex = 1;
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.cancelButton.Location = new System.Drawing.Point(739, 3);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 0;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// ExportFileDialog
			// 
			this.ExportFileDialog.DefaultExt = "xml";
			this.ExportFileDialog.Filter = "XML files|*.xml|All files|*.*";
			this.ExportFileDialog.SupportMultiDottedExtensions = true;
			this.ExportFileDialog.Title = "Export XML File As";
			// 
			// XsltFileDialog
			// 
			this.XsltFileDialog.AddExtension = false;
			this.XsltFileDialog.DefaultExt = "xslt";
			this.XsltFileDialog.Filter = "XSLT files|*.xslt;*.xsl|All files|*.*";
			this.XsltFileDialog.SupportMultiDottedExtensions = true;
			this.XsltFileDialog.Title = "XSLT stylesheet";
			// 
			// xsltContextMenuStrip
			// 
			this.xsltContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cleanupxsltToolStripMenuItem});
			this.xsltContextMenuStrip.Name = "xsltContextMenuStrip";
			this.xsltContextMenuStrip.Size = new System.Drawing.Size(107, 26);
			// 
			// cleanupxsltToolStripMenuItem
			// 
			this.cleanupxsltToolStripMenuItem.Name = "cleanupxsltToolStripMenuItem";
			this.cleanupxsltToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
			this.cleanupxsltToolStripMenuItem.Text = "Cleanup";
			this.cleanupxsltToolStripMenuItem.Click += new System.EventHandler(this.SetXsltFileNameToCleanup);
			// 
			// exportFileNameContextMenuStrip
			// 
			this.exportFileNameContextMenuStrip.Name = "xsltContextMenuStrip";
			this.exportFileNameContextMenuStrip.Size = new System.Drawing.Size(61, 4);
			// 
			// XmlExportDialog
			// 
			this.AcceptButton = this.exportButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(834, 132);
			this.Controls.Add(this.panel4);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(9999, 170);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(565, 170);
			this.Name = "XmlExportDialog";
			this.Padding = new System.Windows.Forms.Padding(10);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.xsltContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox exportFileNameTextBox;
		private System.Windows.Forms.Button exportFileNameButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.TextBox xsltFileNameTextBox;
		private System.Windows.Forms.Button xsltFileNameButton;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Button exportButton;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Button cancelButton;
		public System.Windows.Forms.SaveFileDialog ExportFileDialog;
		public System.Windows.Forms.OpenFileDialog XsltFileDialog;
		private System.Windows.Forms.Button xsltListButton;
		private System.Windows.Forms.ContextMenuStrip xsltContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem cleanupxsltToolStripMenuItem;
		private System.Windows.Forms.Button exportFileNameListButton;
		private System.Windows.Forms.ContextMenuStrip exportFileNameContextMenuStrip;
	}
}