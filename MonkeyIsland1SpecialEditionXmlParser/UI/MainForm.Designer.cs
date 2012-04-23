namespace MonkeyIsland1SpecialEditionXmlParser.UI
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
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dummyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mISEExplorerLinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mISEExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.forumLinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.MdiWindowListItem = this.windowToolStripMenuItem;
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
            this.recentToolStripMenuItem,
            this.toolStripMenuItem4,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(31, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenFileWithDialog);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(149, 6);
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.Enabled = false;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exportToolStripMenuItem.Text = "&Export";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dummyToolStripMenuItem});
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.recentToolStripMenuItem.Text = "&Recent";
			this.recentToolStripMenuItem.DropDownOpening += new System.EventHandler(this.UpdateRecentMenuItems);
			// 
			// dummyToolStripMenuItem
			// 
			this.dummyToolStripMenuItem.Name = "dummyToolStripMenuItem";
			this.dummyToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.dummyToolStripMenuItem.Text = "dummy";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(149, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitApplication);
			// 
			// windowToolStripMenuItem
			// 
			this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
			this.windowToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
			this.windowToolStripMenuItem.Text = "&Window";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mISEExplorerLinkToolStripMenuItem,
            this.forumLinkToolStripMenuItem,
            this.toolStripMenuItem3,
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// mISEExplorerLinkToolStripMenuItem
			// 
			this.mISEExplorerLinkToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mISEExplorerToolStripMenuItem});
			this.mISEExplorerLinkToolStripMenuItem.Name = "mISEExplorerLinkToolStripMenuItem";
			this.mISEExplorerLinkToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.mISEExplorerLinkToolStripMenuItem.Text = "3rd Party links";
			// 
			// mISEExplorerToolStripMenuItem
			// 
			this.mISEExplorerToolStripMenuItem.Name = "mISEExplorerToolStripMenuItem";
			this.mISEExplorerToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
			this.mISEExplorerToolStripMenuItem.Text = "MISE Explorer";
			this.mISEExplorerToolStripMenuItem.Click += new System.EventHandler(this.NavigateToMISEExplorer);
			// 
			// forumLinkToolStripMenuItem
			// 
			this.forumLinkToolStripMenuItem.Name = "forumLinkToolStripMenuItem";
			this.forumLinkToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.forumLinkToolStripMenuItem.Text = "Forum thread link";
			this.forumLinkToolStripMenuItem.Click += new System.EventHandler(this.NavigateToForumThread);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(145, 6);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.aboutToolStripMenuItem.Text = "About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.ShowAboutForm);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(776, 653);
			this.Controls.Add(this.menuStrip1);
			this.IsMdiContainer = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "MonkeyIsland1SpecialEditionXmlParser";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mISEExplorerLinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mISEExplorerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem forumLinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem dummyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
	}
}