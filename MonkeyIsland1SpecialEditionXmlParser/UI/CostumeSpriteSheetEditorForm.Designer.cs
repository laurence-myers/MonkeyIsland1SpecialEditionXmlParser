namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	partial class CostumeSpriteSheetEditorForm
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
			this.labelScreenX = new System.Windows.Forms.Label();
			this.numericScreenX = new System.Windows.Forms.NumericUpDown();
			this.labelScreenY = new System.Windows.Forms.Label();
			this.numericScreenY = new System.Windows.Forms.NumericUpDown();
			this.labelMoveX = new System.Windows.Forms.Label();
			this.numericMoveX = new System.Windows.Forms.NumericUpDown();
			this.labelMoveY = new System.Windows.Forms.Label();
			this.numericMoveY = new System.Windows.Forms.NumericUpDown();
			this.labelClassicDelta = new System.Windows.Forms.Label();
			this.buttonAlignToClassic = new System.Windows.Forms.Button();
			this.checkBoxCalibration = new System.Windows.Forms.CheckBox();
			this.labelHint = new System.Windows.Forms.Label();
			this.panelPreviewHost = new System.Windows.Forms.Panel();
			this.panelPreview = new System.Windows.Forms.Panel();
			this.costumePreviewControl = new MonkeyIsland1SpecialEditionXmlParser.UI.CostumePreviewControl();
			this.panelAnimation = new System.Windows.Forms.Panel();
			this.comboBoxAnimations = new System.Windows.Forms.ComboBox();
			this.labelStep = new System.Windows.Forms.Label();
			this.numericStep = new System.Windows.Forms.NumericUpDown();
			this.labelStepCount = new System.Windows.Forms.Label();
			this.checkBoxPlay = new System.Windows.Forms.CheckBox();
			this.timerPlayback = new System.Windows.Forms.Timer( this.components );
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
			((System.ComponentModel.ISupportInitialize)(this.numericScreenX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScreenY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericMoveX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericMoveY)).BeginInit();
			this.panelPreviewHost.SuspendLayout();
			this.panelPreview.SuspendLayout();
			this.panelAnimation.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericStep)).BeginInit();
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
			this.label1.Text = "Costume Spritesheet Editor";
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
			this.splitRight.Panel2.Controls.Add(this.panelPreviewHost);
			this.splitRight.Size = new System.Drawing.Size(584, 596);
			this.splitRight.SplitterDistance = 240;
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
			this.splitEdit.Panel2MinSize = 220;
			this.splitEdit.Size = new System.Drawing.Size(584, 240);
			this.splitEdit.SplitterDistance = 350;
			this.splitEdit.TabIndex = 0;
			//
			// treeViewSprites
			//
			this.treeViewSprites.CheckBoxes = true;
			this.treeViewSprites.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeViewSprites.HideSelection = false;
			this.treeViewSprites.Location = new System.Drawing.Point(0, 0);
			this.treeViewSprites.Name = "treeViewSprites";
			this.treeViewSprites.Size = new System.Drawing.Size(350, 240);
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
			this.panelProperties.Controls.Add(this.labelScreenX);
			this.panelProperties.Controls.Add(this.numericScreenX);
			this.panelProperties.Controls.Add(this.labelScreenY);
			this.panelProperties.Controls.Add(this.numericScreenY);
			this.panelProperties.Controls.Add(this.labelMoveX);
			this.panelProperties.Controls.Add(this.numericMoveX);
			this.panelProperties.Controls.Add(this.labelMoveY);
			this.panelProperties.Controls.Add(this.numericMoveY);
			this.panelProperties.Controls.Add(this.labelClassicDelta);
			this.panelProperties.Controls.Add(this.buttonAlignToClassic);
			this.panelProperties.Controls.Add(this.checkBoxCalibration);
			this.panelProperties.Controls.Add(this.labelHint);
			this.panelProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelProperties.Location = new System.Drawing.Point(0, 0);
			this.panelProperties.Name = "panelProperties";
			this.panelProperties.Size = new System.Drawing.Size(230, 240);
			this.panelProperties.TabIndex = 0;
			//
			// labelTextureX
			//
			this.labelTextureX.Location = new System.Drawing.Point(6, 8);
			this.labelTextureX.Name = "labelTextureX";
			this.labelTextureX.Size = new System.Drawing.Size(70, 18);
			this.labelTextureX.TabIndex = 0;
			this.labelTextureX.Text = "Texture X";
			//
			// numericTextureX
			//
			this.numericTextureX.Location = new System.Drawing.Point(80, 6);
			this.numericTextureX.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureX.Name = "numericTextureX";
			this.numericTextureX.Size = new System.Drawing.Size(70, 20);
			this.numericTextureX.TabIndex = 1;
			this.numericTextureX.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureY
			//
			this.labelTextureY.Location = new System.Drawing.Point(6, 34);
			this.labelTextureY.Name = "labelTextureY";
			this.labelTextureY.Size = new System.Drawing.Size(70, 18);
			this.labelTextureY.TabIndex = 2;
			this.labelTextureY.Text = "Texture Y";
			//
			// numericTextureY
			//
			this.numericTextureY.Location = new System.Drawing.Point(80, 32);
			this.numericTextureY.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureY.Name = "numericTextureY";
			this.numericTextureY.Size = new System.Drawing.Size(70, 20);
			this.numericTextureY.TabIndex = 3;
			this.numericTextureY.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureWidth
			//
			this.labelTextureWidth.Location = new System.Drawing.Point(6, 60);
			this.labelTextureWidth.Name = "labelTextureWidth";
			this.labelTextureWidth.Size = new System.Drawing.Size(70, 18);
			this.labelTextureWidth.TabIndex = 4;
			this.labelTextureWidth.Text = "Width";
			//
			// numericTextureWidth
			//
			this.numericTextureWidth.Location = new System.Drawing.Point(80, 58);
			this.numericTextureWidth.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureWidth.Name = "numericTextureWidth";
			this.numericTextureWidth.Size = new System.Drawing.Size(70, 20);
			this.numericTextureWidth.TabIndex = 5;
			this.numericTextureWidth.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelTextureHeight
			//
			this.labelTextureHeight.Location = new System.Drawing.Point(6, 86);
			this.labelTextureHeight.Name = "labelTextureHeight";
			this.labelTextureHeight.Size = new System.Drawing.Size(70, 18);
			this.labelTextureHeight.TabIndex = 6;
			this.labelTextureHeight.Text = "Height";
			//
			// numericTextureHeight
			//
			this.numericTextureHeight.Location = new System.Drawing.Point(80, 84);
			this.numericTextureHeight.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
			this.numericTextureHeight.Name = "numericTextureHeight";
			this.numericTextureHeight.Size = new System.Drawing.Size(70, 20);
			this.numericTextureHeight.TabIndex = 7;
			this.numericTextureHeight.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelScreenX
			//
			this.labelScreenX.Location = new System.Drawing.Point(6, 116);
			this.labelScreenX.Name = "labelScreenX";
			this.labelScreenX.Size = new System.Drawing.Size(70, 18);
			this.labelScreenX.TabIndex = 8;
			this.labelScreenX.Text = "Screen X";
			//
			// numericScreenX
			//
			this.numericScreenX.DecimalPlaces = 3;
			this.numericScreenX.Location = new System.Drawing.Point(80, 114);
			this.numericScreenX.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericScreenX.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericScreenX.Name = "numericScreenX";
			this.numericScreenX.Size = new System.Drawing.Size(90, 20);
			this.numericScreenX.TabIndex = 9;
			this.numericScreenX.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelScreenY
			//
			this.labelScreenY.Location = new System.Drawing.Point(6, 142);
			this.labelScreenY.Name = "labelScreenY";
			this.labelScreenY.Size = new System.Drawing.Size(70, 18);
			this.labelScreenY.TabIndex = 10;
			this.labelScreenY.Text = "Screen Y";
			//
			// numericScreenY
			//
			this.numericScreenY.DecimalPlaces = 3;
			this.numericScreenY.Location = new System.Drawing.Point(80, 140);
			this.numericScreenY.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericScreenY.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericScreenY.Name = "numericScreenY";
			this.numericScreenY.Size = new System.Drawing.Size(90, 20);
			this.numericScreenY.TabIndex = 11;
			this.numericScreenY.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelMoveX
			//
			this.labelMoveX.Location = new System.Drawing.Point(6, 168);
			this.labelMoveX.Name = "labelMoveX";
			this.labelMoveX.Size = new System.Drawing.Size(70, 18);
			this.labelMoveX.TabIndex = 12;
			this.labelMoveX.Text = "Move X";
			//
			// numericMoveX
			//
			this.numericMoveX.DecimalPlaces = 3;
			this.numericMoveX.Location = new System.Drawing.Point(80, 166);
			this.numericMoveX.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericMoveX.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericMoveX.Name = "numericMoveX";
			this.numericMoveX.Size = new System.Drawing.Size(90, 20);
			this.numericMoveX.TabIndex = 13;
			this.numericMoveX.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelMoveY
			//
			this.labelMoveY.Location = new System.Drawing.Point(6, 194);
			this.labelMoveY.Name = "labelMoveY";
			this.labelMoveY.Size = new System.Drawing.Size(70, 18);
			this.labelMoveY.TabIndex = 14;
			this.labelMoveY.Text = "Move Y";
			//
			// numericMoveY
			//
			this.numericMoveY.DecimalPlaces = 3;
			this.numericMoveY.Location = new System.Drawing.Point(80, 192);
			this.numericMoveY.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
			this.numericMoveY.Minimum = new decimal(new int[] { 100000, 0, 0, -2147483648 });
			this.numericMoveY.Name = "numericMoveY";
			this.numericMoveY.Size = new System.Drawing.Size(90, 20);
			this.numericMoveY.TabIndex = 15;
			this.numericMoveY.ValueChanged += new System.EventHandler(this.HandleNumericValueChanged);
			//
			// labelClassicDelta
			//
			this.labelClassicDelta.AutoEllipsis = true;
			this.labelClassicDelta.Location = new System.Drawing.Point(6, 220);
			this.labelClassicDelta.Name = "labelClassicDelta";
			this.labelClassicDelta.Size = new System.Drawing.Size(220, 18);
			this.labelClassicDelta.TabIndex = 16;
			this.labelClassicDelta.Text = "Classic: n/a";
			//
			// buttonAlignToClassic
			//
			this.buttonAlignToClassic.Location = new System.Drawing.Point(6, 242);
			this.buttonAlignToClassic.Name = "buttonAlignToClassic";
			this.buttonAlignToClassic.Size = new System.Drawing.Size(164, 23);
			this.buttonAlignToClassic.TabIndex = 17;
			this.buttonAlignToClassic.Text = "Align to classic position";
			this.buttonAlignToClassic.UseVisualStyleBackColor = true;
			this.buttonAlignToClassic.Click += new System.EventHandler(this.AlignToClassic);
			//
			// checkBoxCalibration
			//
			this.checkBoxCalibration.Location = new System.Drawing.Point(6, 270);
			this.checkBoxCalibration.Name = "checkBoxCalibration";
			this.checkBoxCalibration.Size = new System.Drawing.Size(164, 20);
			this.checkBoxCalibration.TabIndex = 18;
			this.checkBoxCalibration.Text = "Classic overlay";
			this.checkBoxCalibration.UseVisualStyleBackColor = true;
			this.checkBoxCalibration.CheckedChanged += new System.EventHandler(this.HandleCalibrationCheckedChanged);
			//
			// labelHint
			//
			this.labelHint.ForeColor = System.Drawing.SystemColors.GrayText;
			this.labelHint.Location = new System.Drawing.Point(6, 294);
			this.labelHint.Name = "labelHint";
			this.labelHint.Size = new System.Drawing.Size(220, 72);
			this.labelHint.TabIndex = 19;
			this.labelHint.Text = "Arrows nudge the selection: texture rect in the atlas, screen position in the preview (Shift = 10). Mouse wheel zooms.";
			//
			// panelPreviewHost
			//
			this.panelPreviewHost.Controls.Add(this.panelPreview);
			this.panelPreviewHost.Controls.Add(this.panelAnimation);
			this.panelPreviewHost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPreviewHost.Location = new System.Drawing.Point(0, 0);
			this.panelPreviewHost.Name = "panelPreviewHost";
			this.panelPreviewHost.Size = new System.Drawing.Size(584, 352);
			this.panelPreviewHost.TabIndex = 0;
			//
			// panelPreview
			//
			this.panelPreview.AutoScroll = true;
			this.panelPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelPreview.Controls.Add(this.costumePreviewControl);
			this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPreview.Location = new System.Drawing.Point(0, 27);
			this.panelPreview.Name = "panelPreview";
			this.panelPreview.Size = new System.Drawing.Size(584, 325);
			this.panelPreview.TabIndex = 1;
			//
			// costumePreviewControl
			//
			this.costumePreviewControl.Location = new System.Drawing.Point(0, 0);
			this.costumePreviewControl.Name = "costumePreviewControl";
			this.costumePreviewControl.Size = new System.Drawing.Size(256, 256);
			this.costumePreviewControl.TabIndex = 0;
			this.costumePreviewControl.Text = "costumePreviewControl";
			//
			// panelAnimation
			//
			this.panelAnimation.Controls.Add(this.checkBoxPlay);
			this.panelAnimation.Controls.Add(this.labelStepCount);
			this.panelAnimation.Controls.Add(this.numericStep);
			this.panelAnimation.Controls.Add(this.labelStep);
			this.panelAnimation.Controls.Add(this.comboBoxAnimations);
			this.panelAnimation.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelAnimation.Location = new System.Drawing.Point(0, 0);
			this.panelAnimation.Name = "panelAnimation";
			this.panelAnimation.Size = new System.Drawing.Size(584, 27);
			this.panelAnimation.TabIndex = 0;
			//
			// comboBoxAnimations
			//
			this.comboBoxAnimations.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAnimations.FormattingEnabled = true;
			this.comboBoxAnimations.Location = new System.Drawing.Point(3, 3);
			this.comboBoxAnimations.Name = "comboBoxAnimations";
			this.comboBoxAnimations.Size = new System.Drawing.Size(180, 21);
			this.comboBoxAnimations.TabIndex = 0;
			this.comboBoxAnimations.SelectedIndexChanged += new System.EventHandler(this.HandleAnimationSelected);
			//
			// labelStep
			//
			this.labelStep.Location = new System.Drawing.Point(192, 6);
			this.labelStep.Name = "labelStep";
			this.labelStep.Size = new System.Drawing.Size(38, 18);
			this.labelStep.TabIndex = 1;
			this.labelStep.Text = "Frame";
			//
			// numericStep
			//
			this.numericStep.Location = new System.Drawing.Point(232, 4);
			this.numericStep.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
			this.numericStep.Name = "numericStep";
			this.numericStep.Size = new System.Drawing.Size(56, 20);
			this.numericStep.TabIndex = 2;
			this.numericStep.ValueChanged += new System.EventHandler(this.HandleStepChanged);
			//
			// labelStepCount
			//
			this.labelStepCount.Location = new System.Drawing.Point(292, 6);
			this.labelStepCount.Name = "labelStepCount";
			this.labelStepCount.Size = new System.Drawing.Size(60, 18);
			this.labelStepCount.TabIndex = 3;
			this.labelStepCount.Text = "of 0";
			//
			// checkBoxPlay
			//
			this.checkBoxPlay.Appearance = System.Windows.Forms.Appearance.Button;
			this.checkBoxPlay.Location = new System.Drawing.Point(356, 2);
			this.checkBoxPlay.Name = "checkBoxPlay";
			this.checkBoxPlay.Size = new System.Drawing.Size(50, 23);
			this.checkBoxPlay.TabIndex = 4;
			this.checkBoxPlay.Text = "Play";
			this.checkBoxPlay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.checkBoxPlay.UseVisualStyleBackColor = true;
			this.checkBoxPlay.CheckedChanged += new System.EventHandler(this.HandlePlayCheckedChanged);
			//
			// timerPlayback
			//
			this.timerPlayback.Interval = 150;
			//
			// CostumeSpriteSheetEditorForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1008, 642);
			this.Controls.Add(this.splitMain);
			this.Controls.Add(this.warningLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "CostumeSpriteSheetEditorForm";
			this.Text = "Costume Spritesheet Editor";
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
			((System.ComponentModel.ISupportInitialize)(this.numericScreenX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericScreenY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericMoveX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericMoveY)).EndInit();
			this.panelPreviewHost.ResumeLayout(false);
			this.panelPreview.ResumeLayout(false);
			this.panelAnimation.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericStep)).EndInit();
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
		private System.Windows.Forms.Panel panelAtlas;
		private AtlasViewControl atlasViewControl;
		private System.Windows.Forms.ComboBox comboBoxTextures;
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
		private System.Windows.Forms.Label labelScreenX;
		private System.Windows.Forms.NumericUpDown numericScreenX;
		private System.Windows.Forms.Label labelScreenY;
		private System.Windows.Forms.NumericUpDown numericScreenY;
		private System.Windows.Forms.Label labelMoveX;
		private System.Windows.Forms.NumericUpDown numericMoveX;
		private System.Windows.Forms.Label labelMoveY;
		private System.Windows.Forms.NumericUpDown numericMoveY;
		private System.Windows.Forms.Label labelClassicDelta;
		private System.Windows.Forms.Button buttonAlignToClassic;
		private System.Windows.Forms.CheckBox checkBoxCalibration;
		private System.Windows.Forms.Label labelHint;
		private System.Windows.Forms.Panel panelPreviewHost;
		private System.Windows.Forms.Panel panelPreview;
		private CostumePreviewControl costumePreviewControl;
		private System.Windows.Forms.Panel panelAnimation;
		private System.Windows.Forms.ComboBox comboBoxAnimations;
		private System.Windows.Forms.Label labelStep;
		private System.Windows.Forms.NumericUpDown numericStep;
		private System.Windows.Forms.Label labelStepCount;
		private System.Windows.Forms.CheckBox checkBoxPlay;
		private System.Windows.Forms.Timer timerPlayback;
	}
}
