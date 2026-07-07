namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class SpriteSheetEditorForm
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
			this.saveOverrideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.revertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportTexturePngToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importTexturePngToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.warningLabel = new System.Windows.Forms.Label();
			this.splitMain = new System.Windows.Forms.SplitContainer();
			this.panelAtlas = new System.Windows.Forms.Panel();
			this.atlasViewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.AtlasViewControl();
			this.comboBoxTextures = new System.Windows.Forms.ComboBox();
			this.labelDiagnostics = new System.Windows.Forms.Label();
			this.listBoxDiagnostics = new System.Windows.Forms.ListBox();
			this.splitRight = new System.Windows.Forms.SplitContainer();
			this.splitEdit = new System.Windows.Forms.SplitContainer();
			this.treeViewSprites = new System.Windows.Forms.TreeView();
			this.panelProperties = new System.Windows.Forms.Panel();
			this.labelTextureX = new System.Windows.Forms.Label();
			this.numericTextureX = new System.Windows.Forms.NumericUpDown();
			this.labelTextureY = new System.Windows.Forms.Label();
			this.numericTextureY = new System.Windows.Forms.NumericUpDown();
			this.labelTextureWidth = new System.Windows.Forms.Label();
			this.numericTextureWidth = new System.Windows.Forms.NumericUpDown();
			this.labelTextureHeight = new System.Windows.Forms.Label();
			this.numericTextureHeight = new System.Windows.Forms.NumericUpDown();
			this.labelOffsetX = new System.Windows.Forms.Label();
			this.numericOffsetX = new System.Windows.Forms.NumericUpDown();
			this.labelOffsetY = new System.Windows.Forms.Label();
			this.numericOffsetY = new System.Windows.Forms.NumericUpDown();
			this.labelLayer = new System.Windows.Forms.Label();
			this.numericLayer = new System.Windows.Forms.NumericUpDown();
			this.labelScaleX = new System.Windows.Forms.Label();
			this.numericScaleX = new System.Windows.Forms.NumericUpDown();
			this.labelScaleY = new System.Windows.Forms.Label();
			this.numericScaleY = new System.Windows.Forms.NumericUpDown();
			this.checkBoxCalibration = new System.Windows.Forms.CheckBox();
			this.checkBoxForeground = new System.Windows.Forms.CheckBox();
			this.labelHint = new System.Windows.Forms.Label();
			this.panelPreview = new System.Windows.Forms.Panel();
			this.roomPreviewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.RoomPreviewControl();
			((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
			this.splitMain.Panel1.SuspendLayout();
			this.splitMain.Panel2.SuspendLayout();
			this.splitMain.SuspendLayout();
			this.panelAtlas.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
			this.splitRight.Panel1.SuspendLayout();
			this.splitRight.Panel2.SuspendLayout();
			this.splitRight.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitEdit)).BeginInit();
			this.splitEdit.Panel1.SuspendLayout();
			this.splitEdit.Panel2.SuspendLayout();
			this.splitEdit.SuspendLayout();
			this.panelProperties.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureWidth)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureHeight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericOffsetX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericOffsetY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericLayer)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScaleX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScaleY)).BeginInit();
			this.panelPreview.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// menuStrip1
			//
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(1008, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			this.menuStrip1.Visible = false;
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveOverrideToolStripMenuItem,
            this.revertToolStripMenuItem,
            this.exportTexturePngToolStripMenuItem,
            this.importTexturePngToolStripMenuItem});
			this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			//
			// saveOverrideToolStripMenuItem
			//
			this.saveOverrideToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.saveOverrideToolStripMenuItem.MergeIndex = 1;
			this.saveOverrideToolStripMenuItem.Name = "saveOverrideToolStripMenuItem";
			this.saveOverrideToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveOverrideToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.saveOverrideToolStripMenuItem.Text = "&Save override";
			this.saveOverrideToolStripMenuItem.Click += new System.EventHandler(this.SaveOverride);
			//
			// revertToolStripMenuItem
			//
			this.revertToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.revertToolStripMenuItem.MergeIndex = 2;
			this.revertToolStripMenuItem.Name = "revertToolStripMenuItem";
			this.revertToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.revertToolStripMenuItem.Text = "&Revert changes";
			this.revertToolStripMenuItem.Click += new System.EventHandler(this.RevertChanges);
			//
			// exportTexturePngToolStripMenuItem
			//
			this.exportTexturePngToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.exportTexturePngToolStripMenuItem.MergeIndex = 3;
			this.exportTexturePngToolStripMenuItem.Name = "exportTexturePngToolStripMenuItem";
			this.exportTexturePngToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.exportTexturePngToolStripMenuItem.Text = "Export &texture as PNG...";
			this.exportTexturePngToolStripMenuItem.Click += new System.EventHandler(this.ExportTexturePng);
			//
			// importTexturePngToolStripMenuItem
			//
			this.importTexturePngToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			this.importTexturePngToolStripMenuItem.MergeIndex = 4;
			this.importTexturePngToolStripMenuItem.Name = "importTexturePngToolStripMenuItem";
			this.importTexturePngToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.importTexturePngToolStripMenuItem.Text = "&Import texture PNG...";
			this.importTexturePngToolStripMenuItem.Click += new System.EventHandler(this.ImportTexturePng);
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
			this.label1.Size = new System.Drawing.Size(1008, 23);
			this.label1.TabIndex = 0;
			this.label1.Text = "Spritesheet Editor";
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
			this.warningLabel.Size = new System.Drawing.Size(1008, 23);
			this.warningLabel.TabIndex = 1;
			this.warningLabel.Text = "Warning";
			this.warningLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.warningLabel.Visible = false;
			//
			// splitMain
			//
			this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitMain.Location = new System.Drawing.Point(0, 46);
			this.splitMain.Name = "splitMain";
			//
			// splitMain.Panel1
			//
			this.splitMain.Panel1.Controls.Add(this.panelAtlas);
			this.splitMain.Panel1.Controls.Add(this.labelDiagnostics);
			this.splitMain.Panel1.Controls.Add(this.listBoxDiagnostics);
			this.splitMain.Panel1.Controls.Add(this.comboBoxTextures);
			//
			// splitMain.Panel2
			//
			this.splitMain.Panel2.Controls.Add(this.splitRight);
			this.splitMain.Size = new System.Drawing.Size(1008, 596);
			this.splitMain.SplitterDistance = 420;
			this.splitMain.TabIndex = 2;
			//
			// panelAtlas
			//
			this.panelAtlas.AutoScroll = true;
			this.panelAtlas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelAtlas.Controls.Add(this.atlasViewControl);
			this.panelAtlas.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelAtlas.Location = new System.Drawing.Point(0, 21);
			this.panelAtlas.Name = "panelAtlas";
			this.panelAtlas.Size = new System.Drawing.Size(420, 447);
			this.panelAtlas.TabIndex = 3;
			//
			// atlasViewControl
			//
			this.atlasViewControl.Location = new System.Drawing.Point(0, 0);
			this.atlasViewControl.Name = "atlasViewControl";
			this.atlasViewControl.Size = new System.Drawing.Size(256, 256);
			this.atlasViewControl.TabIndex = 0;
			this.atlasViewControl.Text = "atlasViewControl";
			//
			// comboBoxTextures
			//
			this.comboBoxTextures.Dock = System.Windows.Forms.DockStyle.Top;
			this.comboBoxTextures.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTextures.FormattingEnabled = true;
			this.comboBoxTextures.Location = new System.Drawing.Point(0, 0);
			this.comboBoxTextures.Name = "comboBoxTextures";
			this.comboBoxTextures.Size = new System.Drawing.Size(420, 21);
			this.comboBoxTextures.TabIndex = 0;
			this.comboBoxTextures.SelectedIndexChanged += new System.EventHandler(this.HandleTextureSelected);
			//
			// labelDiagnostics
			//
			this.labelDiagnostics.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.labelDiagnostics.Location = new System.Drawing.Point(0, 468);
			this.labelDiagnostics.Name = "labelDiagnostics";
			this.labelDiagnostics.Padding = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.labelDiagnostics.Size = new System.Drawing.Size(420, 18);
			this.labelDiagnostics.TabIndex = 1;
			this.labelDiagnostics.Text = "Diagnostics";
			//
			// listBoxDiagnostics
			//
			this.listBoxDiagnostics.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.listBoxDiagnostics.FormattingEnabled = true;
			this.listBoxDiagnostics.HorizontalScrollbar = true;
			this.listBoxDiagnostics.IntegralHeight = false;
			this.listBoxDiagnostics.Location = new System.Drawing.Point(0, 486);
			this.listBoxDiagnostics.Name = "listBoxDiagnostics";
			this.listBoxDiagnostics.Size = new System.Drawing.Size(420, 110);
			this.listBoxDiagnostics.TabIndex = 2;
			//
			// splitRight
			//
			this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitRight.Location = new System.Drawing.Point(0, 0);
			this.splitRight.Name = "splitRight";
			this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
			//
			// splitRight.Panel1
			//
			this.splitRight.Panel1.Controls.Add(this.splitEdit);
			//
			// splitRight.Panel2
			//
			this.splitRight.Panel2.Controls.Add(this.panelPreview);
			this.splitRight.Size = new System.Drawing.Size(584, 596);
			this.splitRight.SplitterDistance = 280;
			this.splitRight.TabIndex = 0;
			//
			// splitEdit
			//
			this.splitEdit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitEdit.Location = new System.Drawing.Point(0, 0);
			this.splitEdit.Name = "splitEdit";
			//
			// splitEdit.Panel1
			//
			this.splitEdit.Panel1.Controls.Add(this.treeViewSprites);
			//
			// splitEdit.Panel2
			//
			this.splitEdit.Panel2.Controls.Add(this.panelProperties);
			this.splitEdit.Panel2MinSize = 200;
			this.splitEdit.Size = new System.Drawing.Size(584, 280);
			this.splitEdit.SplitterDistance = 370;
			this.splitEdit.TabIndex = 0;
			//
			// treeViewSprites
			//
			this.treeViewSprites.CheckBoxes = true;
			this.treeViewSprites.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeViewSprites.HideSelection = false;
			this.treeViewSprites.Location = new System.Drawing.Point(0, 0);
			this.treeViewSprites.Name = "treeViewSprites";
			this.treeViewSprites.Size = new System.Drawing.Size(370, 280);
			this.treeViewSprites.TabIndex = 0;
			this.treeViewSprites.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.HandleTreeAfterCheck);
			this.treeViewSprites.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.HandleTreeAfterSelect);
			//
			// panelProperties
			//
			this.panelProperties.AutoScroll = true;
			this.panelProperties.Controls.Add(this.labelTextureX);
			this.panelProperties.Controls.Add(this.numericTextureX);
			this.panelProperties.Controls.Add(this.labelTextureY);
			this.panelProperties.Controls.Add(this.numericTextureY);
			this.panelProperties.Controls.Add(this.labelTextureWidth);
			this.panelProperties.Controls.Add(this.numericTextureWidth);
			this.panelProperties.Controls.Add(this.labelTextureHeight);
			this.panelProperties.Controls.Add(this.numericTextureHeight);
			this.panelProperties.Controls.Add(this.labelOffsetX);
			this.panelProperties.Controls.Add(this.numericOffsetX);
			this.panelProperties.Controls.Add(this.labelOffsetY);
			this.panelProperties.Controls.Add(this.numericOffsetY);
			this.panelProperties.Controls.Add(this.labelLayer);
			this.panelProperties.Controls.Add(this.numericLayer);
			this.panelProperties.Controls.Add(this.labelScaleX);
			this.panelProperties.Controls.Add(this.numericScaleX);
			this.panelProperties.Controls.Add(this.labelScaleY);
			this.panelProperties.Controls.Add(this.numericScaleY);
			this.panelProperties.Controls.Add(this.checkBoxCalibration);
			this.panelProperties.Controls.Add(this.checkBoxForeground);
			this.panelProperties.Controls.Add(this.labelHint);
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelProperties.Location = new System.Drawing.Point(0, 0);
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size(210, 280);
			this.panelProperties.TabIndex = 0;
			//
			// labelTextureX
			//
			this.labelTextureX.Location = new System.Drawing.Point(6, 10);
			this.labelTextureX.Name = "labelTextureX";
			this.labelTextureX.Size = new System.Drawing.Size(88, 16);
			this.labelTextureX.TabIndex = 0;
			this.labelTextureX.Text = "Texture X";
			//
			// numericTextureX
			//
			this.numericTextureX.Location = new System.Drawing.Point(100, 8);
			this.numericTextureX.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureX.Name = "numericTextureX";
			this.numericTextureX.Size = new System.Drawing.Size(90, 20);
			this.numericTextureX.TabIndex = 1;
			this.numericTextureX.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureY
			//
			this.labelTextureY.Location = new System.Drawing.Point(6, 36);
			this.labelTextureY.Name = "labelTextureY";
			this.labelTextureY.Size = new System.Drawing.Size(88, 16);
			this.labelTextureY.TabIndex = 2;
			this.labelTextureY.Text = "Texture Y";
			//
			// numericTextureY
			//
			this.numericTextureY.Location = new System.Drawing.Point(100, 34);
			this.numericTextureY.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureY.Name = "numericTextureY";
			this.numericTextureY.Size = new System.Drawing.Size(90, 20);
			this.numericTextureY.TabIndex = 3;
			this.numericTextureY.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureWidth
			//
			this.labelTextureWidth.Location = new System.Drawing.Point(6, 62);
			this.labelTextureWidth.Name = "labelTextureWidth";
			this.labelTextureWidth.Size = new System.Drawing.Size(88, 16);
			this.labelTextureWidth.TabIndex = 4;
			this.labelTextureWidth.Text = "Texture Width";
			//
			// numericTextureWidth
			//
			this.numericTextureWidth.Location = new System.Drawing.Point(100, 60);
			this.numericTextureWidth.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureWidth.Name = "numericTextureWidth";
			this.numericTextureWidth.Size = new System.Drawing.Size(90, 20);
			this.numericTextureWidth.TabIndex = 5;
			this.numericTextureWidth.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureHeight
			//
			this.labelTextureHeight.Location = new System.Drawing.Point(6, 88);
			this.labelTextureHeight.Name = "labelTextureHeight";
			this.labelTextureHeight.Size = new System.Drawing.Size(88, 16);
			this.labelTextureHeight.TabIndex = 6;
			this.labelTextureHeight.Text = "Texture Height";
			//
			// numericTextureHeight
			//
			this.numericTextureHeight.Location = new System.Drawing.Point(100, 86);
			this.numericTextureHeight.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureHeight.Name = "numericTextureHeight";
			this.numericTextureHeight.Size = new System.Drawing.Size(90, 20);
			this.numericTextureHeight.TabIndex = 7;
			this.numericTextureHeight.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelOffsetX
			//
			this.labelOffsetX.Location = new System.Drawing.Point(6, 114);
			this.labelOffsetX.Name = "labelOffsetX";
			this.labelOffsetX.Size = new System.Drawing.Size(88, 16);
			this.labelOffsetX.TabIndex = 8;
			this.labelOffsetX.Text = "Offset X";
			//
			// numericOffsetX
			//
			this.numericOffsetX.DecimalPlaces = 3;
			this.numericOffsetX.Location = new System.Drawing.Point(100, 112);
			this.numericOffsetX.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericOffsetX.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericOffsetX.Name = "numericOffsetX";
			this.numericOffsetX.Size = new System.Drawing.Size(90, 20);
			this.numericOffsetX.TabIndex = 9;
			this.numericOffsetX.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelOffsetY
			//
			this.labelOffsetY.Location = new System.Drawing.Point(6, 140);
			this.labelOffsetY.Name = "labelOffsetY";
			this.labelOffsetY.Size = new System.Drawing.Size(88, 16);
			this.labelOffsetY.TabIndex = 10;
			this.labelOffsetY.Text = "Offset Y";
			//
			// numericOffsetY
			//
			this.numericOffsetY.DecimalPlaces = 3;
			this.numericOffsetY.Location = new System.Drawing.Point(100, 138);
			this.numericOffsetY.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericOffsetY.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericOffsetY.Name = "numericOffsetY";
			this.numericOffsetY.Size = new System.Drawing.Size(90, 20);
			this.numericOffsetY.TabIndex = 11;
			this.numericOffsetY.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelLayer
			//
			this.labelLayer.Location = new System.Drawing.Point(6, 166);
			this.labelLayer.Name = "labelLayer";
			this.labelLayer.Size = new System.Drawing.Size(88, 16);
			this.labelLayer.TabIndex = 12;
			this.labelLayer.Text = "Layer";
			//
			// numericLayer
			//
			this.numericLayer.Location = new System.Drawing.Point(100, 164);
			this.numericLayer.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
			this.numericLayer.Minimum = new decimal(new int[] { 1000, 0, 0, -2147483648 });
			this.numericLayer.Name = "numericLayer";
			this.numericLayer.Size = new System.Drawing.Size(90, 20);
			this.numericLayer.TabIndex = 13;
			this.numericLayer.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelScaleX
			//
			this.labelScaleX.Location = new System.Drawing.Point(6, 198);
			this.labelScaleX.Name = "labelScaleX";
			this.labelScaleX.Size = new System.Drawing.Size(88, 16);
			this.labelScaleX.TabIndex = 14;
			this.labelScaleX.Text = "HD Scale X";
			//
			// numericScaleX
			//
			this.numericScaleX.DecimalPlaces = 2;
			this.numericScaleX.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericScaleX.Location = new System.Drawing.Point(100, 196);
			this.numericScaleX.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
			this.numericScaleX.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericScaleX.Name = "numericScaleX";
			this.numericScaleX.Size = new System.Drawing.Size(90, 20);
			this.numericScaleX.TabIndex = 15;
			this.numericScaleX.Value = new decimal(new int[] { 6, 0, 0, 0 });
			this.numericScaleX.ValueChanged += new System.EventHandler(this.HandleScaleChanged);
			//
			// labelScaleY
			//
			this.labelScaleY.Location = new System.Drawing.Point(6, 224);
			this.labelScaleY.Name = "labelScaleY";
			this.labelScaleY.Size = new System.Drawing.Size(88, 16);
			this.labelScaleY.TabIndex = 16;
			this.labelScaleY.Text = "HD Scale Y";
			//
			// numericScaleY
			//
			this.numericScaleY.DecimalPlaces = 2;
			this.numericScaleY.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericScaleY.Location = new System.Drawing.Point(100, 222);
			this.numericScaleY.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
			this.numericScaleY.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericScaleY.Name = "numericScaleY";
			this.numericScaleY.Size = new System.Drawing.Size(90, 20);
			this.numericScaleY.TabIndex = 17;
			this.numericScaleY.Value = new decimal(new int[] { 72, 0, 0, 65536 });
			this.numericScaleY.ValueChanged += new System.EventHandler(this.HandleScaleChanged);
			//
			// checkBoxCalibration
			//
			this.checkBoxCalibration.Location = new System.Drawing.Point(9, 248);
			this.checkBoxCalibration.Name = "checkBoxCalibration";
			this.checkBoxCalibration.Size = new System.Drawing.Size(181, 20);
			this.checkBoxCalibration.TabIndex = 18;
			this.checkBoxCalibration.Text = "Calibration overlay";
			this.checkBoxCalibration.CheckedChanged += new System.EventHandler(this.HandleCalibrationCheckedChanged);
			//
			// checkBoxForeground
			//
			this.checkBoxForeground.Checked = true;
			this.checkBoxForeground.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxForeground.Location = new System.Drawing.Point(9, 274);
			this.checkBoxForeground.Name = "checkBoxForeground";
			this.checkBoxForeground.Size = new System.Drawing.Size(181, 20);
			this.checkBoxForeground.TabIndex = 19;
			this.checkBoxForeground.Text = "Show foreground layer";
			this.checkBoxForeground.CheckedChanged += new System.EventHandler(this.HandleForegroundCheckedChanged);
			//
			// labelHint
			//
			this.labelHint.ForeColor = System.Drawing.SystemColors.GrayText;
			this.labelHint.Location = new System.Drawing.Point(6, 300);
			this.labelHint.Name = "labelHint";
			this.labelHint.Size = new System.Drawing.Size(196, 70);
			this.labelHint.TabIndex = 20;
			this.labelHint.Text = "Arrow keys nudge the selection:\r\natlas = texture rect, preview = offset.\r\nHold S" +
				"hift for steps of 10.\r\nMouse wheel zooms.";
			//
			// panelPreview
			//
			this.panelPreview.AutoScroll = true;
			this.panelPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelPreview.Controls.Add(this.roomPreviewControl);
			this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPreview.Location = new System.Drawing.Point(0, 0);
			this.panelPreview.Name = "panelPreview";
			this.panelPreview.Size = new System.Drawing.Size(584, 312);
			this.panelPreview.TabIndex = 0;
			//
			// roomPreviewControl
			//
			this.roomPreviewControl.Location = new System.Drawing.Point(0, 0);
			this.roomPreviewControl.Name = "roomPreviewControl";
			this.roomPreviewControl.Size = new System.Drawing.Size(282, 214);
			this.roomPreviewControl.TabIndex = 0;
			this.roomPreviewControl.Text = "roomPreviewControl";
			//
			// SpriteSheetEditorForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1008, 642);
			this.Controls.Add(this.splitMain);
			this.Controls.Add(this.warningLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "SpriteSheetEditorForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "SpriteSheetEditorForm";
			this.splitMain.Panel1.ResumeLayout(false);
			this.splitMain.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
			this.splitMain.ResumeLayout(false);
			this.panelAtlas.ResumeLayout(false);
			this.splitRight.Panel1.ResumeLayout(false);
			this.splitRight.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
			this.splitRight.ResumeLayout(false);
			this.splitEdit.Panel1.ResumeLayout(false);
			this.splitEdit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitEdit)).EndInit();
			this.splitEdit.ResumeLayout(false);
			this.panelProperties.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericTextureX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureWidth)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTextureHeight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericOffsetX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericOffsetY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericLayer)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScaleX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScaleY)).EndInit();
			this.panelPreview.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveOverrideToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem revertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportTexturePngToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importTexturePngToolStripMenuItem;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label warningLabel;
		private System.Windows.Forms.SplitContainer splitMain;
		private System.Windows.Forms.ComboBox comboBoxTextures;
		private System.Windows.Forms.Panel panelAtlas;
		private AtlasViewControl atlasViewControl;
		private System.Windows.Forms.Label labelDiagnostics;
		private System.Windows.Forms.ListBox listBoxDiagnostics;
		private System.Windows.Forms.SplitContainer splitRight;
		private System.Windows.Forms.SplitContainer splitEdit;
		private System.Windows.Forms.TreeView treeViewSprites;
		private System.Windows.Forms.Panel panelProperties;
		private System.Windows.Forms.Label labelTextureX;
		private System.Windows.Forms.NumericUpDown numericTextureX;
		private System.Windows.Forms.Label labelTextureY;
		private System.Windows.Forms.NumericUpDown numericTextureY;
		private System.Windows.Forms.Label labelTextureWidth;
		private System.Windows.Forms.NumericUpDown numericTextureWidth;
		private System.Windows.Forms.Label labelTextureHeight;
		private System.Windows.Forms.NumericUpDown numericTextureHeight;
		private System.Windows.Forms.Label labelOffsetX;
		private System.Windows.Forms.NumericUpDown numericOffsetX;
		private System.Windows.Forms.Label labelOffsetY;
		private System.Windows.Forms.NumericUpDown numericOffsetY;
		private System.Windows.Forms.Label labelLayer;
		private System.Windows.Forms.NumericUpDown numericLayer;
		private System.Windows.Forms.Label labelScaleX;
		private System.Windows.Forms.NumericUpDown numericScaleX;
		private System.Windows.Forms.Label labelScaleY;
		private System.Windows.Forms.NumericUpDown numericScaleY;
		private System.Windows.Forms.CheckBox checkBoxCalibration;
		private System.Windows.Forms.CheckBox checkBoxForeground;
		private System.Windows.Forms.Label labelHint;
		private System.Windows.Forms.Panel panelPreview;
		private RoomPreviewControl roomPreviewControl;
	}
}
