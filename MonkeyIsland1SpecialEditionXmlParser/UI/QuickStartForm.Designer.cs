namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class QuickStartForm
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
			this.panel2 = new System.Windows.Forms.Panel();
			this.openFileButton = new System.Windows.Forms.Button();
			this.panel3 = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.panel4 = new System.Windows.Forms.Panel();
			this.panel14 = new System.Windows.Forms.Panel();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.panel5 = new System.Windows.Forms.Panel();
			this.openRecentFileButton = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.panel6 = new System.Windows.Forms.Panel();
			this.dontShowAtStartupButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoEllipsis = true;
			this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.ForeColor = System.Drawing.SystemColors.ButtonFace;
			this.label1.Location = new System.Drawing.Point(10, 10);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(3);
			this.label1.Size = new System.Drawing.Size(498, 23);
			this.label1.TabIndex = 4;
			this.label1.Text = "Select";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel2
			// 
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(10, 33);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(498, 3);
			this.panel2.TabIndex = 8;
			// 
			// openFileButton
			// 
			this.openFileButton.Dock = System.Windows.Forms.DockStyle.Top;
			this.openFileButton.Location = new System.Drawing.Point(10, 36);
			this.openFileButton.Name = "openFileButton";
			this.openFileButton.Size = new System.Drawing.Size(498, 46);
			this.openFileButton.TabIndex = 9;
			this.openFileButton.Text = "Open file...";
			this.openFileButton.UseVisualStyleBackColor = true;
			this.openFileButton.Click += new System.EventHandler(this.ShowOpenFileDialog);
			// 
			// panel3
			// 
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(10, 82);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(498, 7);
			this.panel3.TabIndex = 10;
			// 
			// label2
			// 
			this.label2.AutoEllipsis = true;
			this.label2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label2.Dock = System.Windows.Forms.DockStyle.Top;
			this.label2.ForeColor = System.Drawing.SystemColors.ButtonFace;
			this.label2.Location = new System.Drawing.Point(10, 89);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(3);
			this.label2.Size = new System.Drawing.Size(498, 23);
			this.label2.TabIndex = 11;
			this.label2.Text = "History";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel4
			// 
			this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel4.Location = new System.Drawing.Point(10, 112);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(498, 3);
			this.panel4.TabIndex = 13;
			// 
			// panel14
			// 
			this.panel14.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel14.Location = new System.Drawing.Point(10, 322);
			this.panel14.Name = "panel14";
			this.panel14.Size = new System.Drawing.Size(498, 7);
			this.panel14.TabIndex = 33;
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Top;
			this.treeView1.HotTracking = true;
			this.treeView1.Location = new System.Drawing.Point(10, 115);
			this.treeView1.Name = "treeView1";
			this.treeView1.ShowRootLines = false;
			this.treeView1.Size = new System.Drawing.Size(498, 158);
			this.treeView1.TabIndex = 34;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.UpdateRecentButtonEnabled);
			this.treeView1.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.OpenRecentFile);
			// 
			// panel5
			// 
			this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel5.Location = new System.Drawing.Point(10, 273);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(498, 3);
			this.panel5.TabIndex = 35;
			// 
			// openRecentFileButton
			// 
			this.openRecentFileButton.Dock = System.Windows.Forms.DockStyle.Top;
			this.openRecentFileButton.Enabled = false;
			this.openRecentFileButton.Location = new System.Drawing.Point(10, 276);
			this.openRecentFileButton.Name = "openRecentFileButton";
			this.openRecentFileButton.Size = new System.Drawing.Size(498, 46);
			this.openRecentFileButton.TabIndex = 36;
			this.openRecentFileButton.Text = "Open recent file";
			this.openRecentFileButton.UseVisualStyleBackColor = true;
			this.openRecentFileButton.Click += new System.EventHandler(this.OpenRecentFile);
			// 
			// label3
			// 
			this.label3.AutoEllipsis = true;
			this.label3.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label3.Dock = System.Windows.Forms.DockStyle.Top;
			this.label3.ForeColor = System.Drawing.SystemColors.ButtonFace;
			this.label3.Location = new System.Drawing.Point(10, 329);
			this.label3.Name = "label3";
			this.label3.Padding = new System.Windows.Forms.Padding(3);
			this.label3.Size = new System.Drawing.Size(498, 23);
			this.label3.TabIndex = 37;
			this.label3.Text = "Options";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel6
			// 
			this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel6.Location = new System.Drawing.Point(10, 352);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(498, 3);
			this.panel6.TabIndex = 38;
			// 
			// dontShowAtStartupButton
			// 
			this.dontShowAtStartupButton.Dock = System.Windows.Forms.DockStyle.Top;
			this.dontShowAtStartupButton.Location = new System.Drawing.Point(10, 355);
			this.dontShowAtStartupButton.Name = "dontShowAtStartupButton";
			this.dontShowAtStartupButton.Size = new System.Drawing.Size(498, 46);
			this.dontShowAtStartupButton.TabIndex = 39;
			this.dontShowAtStartupButton.Text = "Do not show this window at startup";
			this.dontShowAtStartupButton.UseVisualStyleBackColor = true;
			this.dontShowAtStartupButton.Click += new System.EventHandler(this.SetDontShowAtStartupFlag);
			// 
			// QuickStartForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(518, 411);
			this.Controls.Add(this.dontShowAtStartupButton);
			this.Controls.Add(this.panel6);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.panel14);
			this.Controls.Add(this.openRecentFileButton);
			this.Controls.Add(this.panel5);
			this.Controls.Add(this.treeView1);
			this.Controls.Add(this.panel4);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.openFileButton);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "QuickStartForm";
			this.Padding = new System.Windows.Forms.Padding(10);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Quick Start";
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.CloseIfEscPressed);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Button openFileButton;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Panel panel14;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Button openRecentFileButton;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.Button dontShowAtStartupButton;

	}
}