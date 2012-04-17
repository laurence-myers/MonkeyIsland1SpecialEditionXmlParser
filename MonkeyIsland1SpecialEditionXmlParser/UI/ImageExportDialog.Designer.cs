namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class ImageExportDialog
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
			this.panel4 = new System.Windows.Forms.Panel();
			this.exportButton = new System.Windows.Forms.Button();
			this.panel5 = new System.Windows.Forms.Panel();
			this.cancelButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.directoryTextBox = new System.Windows.Forms.TextBox();
			this.directoryBrowseButton = new System.Windows.Forms.Button();
			this.directoryRecentButton = new System.Windows.Forms.Button();
			this.panel3 = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.filePrefixTextBox = new System.Windows.Forms.TextBox();
			this.filePrefixRecentButton = new System.Windows.Forms.Button();
			this.panel6 = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.panel7 = new System.Windows.Forms.Panel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.paddingBottomTextBox = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.paddingRightTextBox = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.paddingTopTextBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.paddingLeftTextBox = new System.Windows.Forms.TextBox();
			this.FolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.directoryRecentContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.filePrefixRecentContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.panel4.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel7.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.exportButton);
			this.panel4.Controls.Add(this.panel5);
			this.panel4.Controls.Add(this.cancelButton);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel4.Location = new System.Drawing.Point(10, 142);
			this.panel4.Name = "panel4";
			this.panel4.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.panel4.Size = new System.Drawing.Size(814, 26);
			this.panel4.TabIndex = 10;
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
			// label1
			// 
			this.label1.AutoEllipsis = true;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Location = new System.Drawing.Point(10, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(814, 13);
			this.label1.TabIndex = 11;
			this.label1.Text = "Directory";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.directoryTextBox);
			this.panel1.Controls.Add(this.directoryBrowseButton);
			this.panel1.Controls.Add(this.directoryRecentButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(10, 23);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(3);
			this.panel1.Size = new System.Drawing.Size(814, 26);
			this.panel1.TabIndex = 12;
			// 
			// directoryTextBox
			// 
			this.directoryTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.directoryTextBox.Location = new System.Drawing.Point(3, 3);
			this.directoryTextBox.Name = "directoryTextBox";
			this.directoryTextBox.Size = new System.Drawing.Size(736, 20);
			this.directoryTextBox.TabIndex = 3;
			this.directoryTextBox.TextChanged += new System.EventHandler(this.HandleDirectoryChanged);
			// 
			// directoryBrowseButton
			// 
			this.directoryBrowseButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.directoryBrowseButton.Location = new System.Drawing.Point(739, 3);
			this.directoryBrowseButton.Name = "directoryBrowseButton";
			this.directoryBrowseButton.Size = new System.Drawing.Size(50, 20);
			this.directoryBrowseButton.TabIndex = 0;
			this.directoryBrowseButton.Text = "...";
			this.directoryBrowseButton.UseVisualStyleBackColor = true;
			this.directoryBrowseButton.Click += new System.EventHandler(this.ShowDirectoryBrowser);
			// 
			// directoryRecentButton
			// 
			this.directoryRecentButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.directoryRecentButton.Font = new System.Drawing.Font("Webdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
			this.directoryRecentButton.Location = new System.Drawing.Point(789, 3);
			this.directoryRecentButton.Name = "directoryRecentButton";
			this.directoryRecentButton.Size = new System.Drawing.Size(22, 20);
			this.directoryRecentButton.TabIndex = 5;
			this.directoryRecentButton.Text = "6";
			this.directoryRecentButton.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			this.directoryRecentButton.UseVisualStyleBackColor = true;
			this.directoryRecentButton.Click += new System.EventHandler(this.ShowDirectoryRecentList);
			// 
			// panel3
			// 
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(10, 49);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(814, 7);
			this.panel3.TabIndex = 13;
			// 
			// label2
			// 
			this.label2.AutoEllipsis = true;
			this.label2.Dock = System.Windows.Forms.DockStyle.Top;
			this.label2.Location = new System.Drawing.Point(10, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(814, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "File prefix";
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.filePrefixTextBox);
			this.panel2.Controls.Add(this.filePrefixRecentButton);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(10, 69);
			this.panel2.Name = "panel2";
			this.panel2.Padding = new System.Windows.Forms.Padding(3);
			this.panel2.Size = new System.Drawing.Size(814, 26);
			this.panel2.TabIndex = 15;
			// 
			// filePrefixTextBox
			// 
			this.filePrefixTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.filePrefixTextBox.Location = new System.Drawing.Point(3, 3);
			this.filePrefixTextBox.Name = "filePrefixTextBox";
			this.filePrefixTextBox.Size = new System.Drawing.Size(786, 20);
			this.filePrefixTextBox.TabIndex = 3;
			this.filePrefixTextBox.TextChanged += new System.EventHandler(this.HandleFilePrefixChanged);
			// 
			// filePrefixRecentButton
			// 
			this.filePrefixRecentButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.filePrefixRecentButton.Font = new System.Drawing.Font("Webdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
			this.filePrefixRecentButton.Location = new System.Drawing.Point(789, 3);
			this.filePrefixRecentButton.Name = "filePrefixRecentButton";
			this.filePrefixRecentButton.Size = new System.Drawing.Size(22, 20);
			this.filePrefixRecentButton.TabIndex = 5;
			this.filePrefixRecentButton.Text = "6";
			this.filePrefixRecentButton.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			this.filePrefixRecentButton.UseVisualStyleBackColor = true;
			this.filePrefixRecentButton.Click += new System.EventHandler(this.ShowFilePrefixRecentList);
			// 
			// panel6
			// 
			this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel6.Location = new System.Drawing.Point(10, 95);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(814, 7);
			this.panel6.TabIndex = 16;
			// 
			// label3
			// 
			this.label3.AutoEllipsis = true;
			this.label3.Dock = System.Windows.Forms.DockStyle.Top;
			this.label3.Location = new System.Drawing.Point(10, 102);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(814, 13);
			this.label3.TabIndex = 17;
			this.label3.Text = "Sprite padding";
			// 
			// panel7
			// 
			this.panel7.Controls.Add(this.tableLayoutPanel1);
			this.panel7.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel7.Location = new System.Drawing.Point(10, 115);
			this.panel7.Name = "panel7";
			this.panel7.Padding = new System.Windows.Forms.Padding(3);
			this.panel7.Size = new System.Drawing.Size(814, 26);
			this.panel7.TabIndex = 18;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 8;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.Controls.Add(this.paddingBottomTextBox, 7, 0);
			this.tableLayoutPanel1.Controls.Add(this.label7, 6, 0);
			this.tableLayoutPanel1.Controls.Add(this.paddingRightTextBox, 5, 0);
			this.tableLayoutPanel1.Controls.Add(this.label6, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this.paddingTopTextBox, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this.label5, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.paddingLeftTextBox, 1, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(808, 20);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// paddingBottomTextBox
			// 
			this.paddingBottomTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.paddingBottomTextBox.Location = new System.Drawing.Point(642, 0);
			this.paddingBottomTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.paddingBottomTextBox.Name = "paddingBottomTextBox";
			this.paddingBottomTextBox.Size = new System.Drawing.Size(166, 20);
			this.paddingBottomTextBox.TabIndex = 7;
			this.paddingBottomTextBox.Text = "3";
			this.paddingBottomTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.paddingBottomTextBox.TextChanged += new System.EventHandler(this.HandlePaddingChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Dock = System.Windows.Forms.DockStyle.Left;
			this.label7.Location = new System.Drawing.Point(599, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(40, 20);
			this.label7.TabIndex = 6;
			this.label7.Text = "Bottom";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// paddingRightTextBox
			// 
			this.paddingRightTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.paddingRightTextBox.Location = new System.Drawing.Point(431, 0);
			this.paddingRightTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.paddingRightTextBox.Name = "paddingRightTextBox";
			this.paddingRightTextBox.Size = new System.Drawing.Size(165, 20);
			this.paddingRightTextBox.TabIndex = 5;
			this.paddingRightTextBox.Text = "3";
			this.paddingRightTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.paddingRightTextBox.TextChanged += new System.EventHandler(this.HandlePaddingChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Dock = System.Windows.Forms.DockStyle.Left;
			this.label6.Location = new System.Drawing.Point(396, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(32, 20);
			this.label6.TabIndex = 4;
			this.label6.Text = "Right";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// paddingTopTextBox
			// 
			this.paddingTopTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.paddingTopTextBox.Location = new System.Drawing.Point(228, 0);
			this.paddingTopTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.paddingTopTextBox.Name = "paddingTopTextBox";
			this.paddingTopTextBox.Size = new System.Drawing.Size(165, 20);
			this.paddingTopTextBox.TabIndex = 3;
			this.paddingTopTextBox.Text = "3";
			this.paddingTopTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.paddingTopTextBox.TextChanged += new System.EventHandler(this.HandlePaddingChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Dock = System.Windows.Forms.DockStyle.Left;
			this.label5.Location = new System.Drawing.Point(199, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(26, 20);
			this.label5.TabIndex = 2;
			this.label5.Text = "Top";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Dock = System.Windows.Forms.DockStyle.Left;
			this.label4.Location = new System.Drawing.Point(3, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(25, 20);
			this.label4.TabIndex = 0;
			this.label4.Text = "Left";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// paddingLeftTextBox
			// 
			this.paddingLeftTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.paddingLeftTextBox.Location = new System.Drawing.Point(31, 0);
			this.paddingLeftTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.paddingLeftTextBox.Name = "paddingLeftTextBox";
			this.paddingLeftTextBox.Size = new System.Drawing.Size(165, 20);
			this.paddingLeftTextBox.TabIndex = 1;
			this.paddingLeftTextBox.Text = "3";
			this.paddingLeftTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.paddingLeftTextBox.TextChanged += new System.EventHandler(this.HandlePaddingChanged);
			// 
			// FolderBrowserDialog
			// 
			this.FolderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
			// 
			// directoryRecentContextMenuStrip
			// 
			this.directoryRecentContextMenuStrip.Name = "directoryRecentContextMenuStrip";
			this.directoryRecentContextMenuStrip.Size = new System.Drawing.Size(61, 4);
			// 
			// filePrefixRecentContextMenuStrip
			// 
			this.filePrefixRecentContextMenuStrip.Name = "contextMenuStrip1";
			this.filePrefixRecentContextMenuStrip.Size = new System.Drawing.Size(153, 26);
			// 
			// ImageExportDialog
			// 
			this.AcceptButton = this.exportButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(834, 178);
			this.Controls.Add(this.panel7);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.panel6);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.panel4);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(9999, 216);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(565, 216);
			this.Name = "ImageExportDialog";
			this.Padding = new System.Windows.Forms.Padding(10);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.panel4.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.panel7.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Button exportButton;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox directoryTextBox;
		private System.Windows.Forms.Button directoryBrowseButton;
		private System.Windows.Forms.Button directoryRecentButton;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.TextBox filePrefixTextBox;
		private System.Windows.Forms.Button filePrefixRecentButton;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel7;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.TextBox paddingBottomTextBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox paddingRightTextBox;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox paddingTopTextBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox paddingLeftTextBox;
		public System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog;
		private System.Windows.Forms.ContextMenuStrip directoryRecentContextMenuStrip;
		private System.Windows.Forms.ContextMenuStrip filePrefixRecentContextMenuStrip;
	}
}