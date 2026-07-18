namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	partial class NewTextureDialog
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			this.labelPng = new System.Windows.Forms.Label();
			this.textBoxPngFile = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.labelPngInfo = new System.Windows.Forms.Label();
			this.labelPath = new System.Windows.Forms.Label();
			this.textBoxResourcePath = new System.Windows.Forms.TextBox();
			this.labelFormat = new System.Windows.Forms.Label();
			this.comboBoxFormat = new System.Windows.Forms.ComboBox();
			new System.Windows.Forms.Label();
			this.buttonImport = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// labelPng
			//
			this.labelPng.AutoSize = true;
			this.labelPng.Location = new System.Drawing.Point( 12, 15 );
			this.labelPng.Name = "labelPng";
			this.labelPng.Size = new System.Drawing.Size( 53, 13 );
			this.labelPng.TabIndex = 0;
			this.labelPng.Text = "PNG file:";
			//
			// textBoxPngFile
			//
			this.textBoxPngFile.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.textBoxPngFile.Location = new System.Drawing.Point( 100, 12 );
			this.textBoxPngFile.Name = "textBoxPngFile";
			this.textBoxPngFile.Size = new System.Drawing.Size( 402, 20 );
			this.textBoxPngFile.TabIndex = 1;
			//
			// buttonBrowse
			//
			this.buttonBrowse.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonBrowse.Location = new System.Drawing.Point( 508, 11 );
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size( 40, 22 );
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			//
			// labelPngInfo
			//
			this.labelPngInfo.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.labelPngInfo.AutoEllipsis = true;
			this.labelPngInfo.ForeColor = System.Drawing.SystemColors.GrayText;
			this.labelPngInfo.Location = new System.Drawing.Point( 97, 35 );
			this.labelPngInfo.Name = "labelPngInfo";
			this.labelPngInfo.Size = new System.Drawing.Size( 451, 15 );
			this.labelPngInfo.TabIndex = 3;
			//
			// labelPath
			//
			this.labelPath.AutoSize = true;
			this.labelPath.Location = new System.Drawing.Point( 12, 63 );
			this.labelPath.Name = "labelPath";
			this.labelPath.Size = new System.Drawing.Size( 79, 13 );
			this.labelPath.TabIndex = 4;
			this.labelPath.Text = "Resource path:";
			//
			// textBoxResourcePath
			//
			this.textBoxResourcePath.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.textBoxResourcePath.Location = new System.Drawing.Point( 100, 60 );
			this.textBoxResourcePath.Name = "textBoxResourcePath";
			this.textBoxResourcePath.Size = new System.Drawing.Size( 448, 20 );
			this.textBoxResourcePath.TabIndex = 5;
			//
			// labelFormat
			//
			this.labelFormat.AutoSize = true;
			this.labelFormat.Location = new System.Drawing.Point( 12, 91 );
			this.labelFormat.Name = "labelFormat";
			this.labelFormat.Size = new System.Drawing.Size( 42, 13 );
			this.labelFormat.TabIndex = 6;
			this.labelFormat.Text = "Format:";
			//
			// comboBoxFormat
			//
			this.comboBoxFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFormat.FormattingEnabled = true;
			this.comboBoxFormat.Location = new System.Drawing.Point( 100, 88 );
			this.comboBoxFormat.Name = "comboBoxFormat";
			this.comboBoxFormat.Size = new System.Drawing.Size( 120, 21 );
			this.comboBoxFormat.TabIndex = 7;
			//
			// buttonImport
			//
			this.buttonImport.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonImport.Location = new System.Drawing.Point( 392, 227 );
			this.buttonImport.Name = "buttonImport";
			this.buttonImport.Size = new System.Drawing.Size( 75, 23 );
			this.buttonImport.TabIndex = 9;
			this.buttonImport.Text = "Import";
			this.buttonImport.UseVisualStyleBackColor = true;
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 473, 227 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 75, 23 );
			this.buttonCancel.TabIndex = 10;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// NewTextureDialog
			//
			this.AcceptButton = this.buttonImport;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 560, 262 );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.buttonImport );
			this.Controls.Add( this.comboBoxFormat );
			this.Controls.Add( this.labelFormat );
			this.Controls.Add( this.textBoxResourcePath );
			this.Controls.Add( this.labelPath );
			this.Controls.Add( this.labelPngInfo );
			this.Controls.Add( this.buttonBrowse );
			this.Controls.Add( this.textBoxPngFile );
			this.Controls.Add( this.labelPng );
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size( 480, 260 );
			this.Name = "NewTextureDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import new texture";
			this.ResumeLayout( false );
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label labelPng;
		private System.Windows.Forms.TextBox textBoxPngFile;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.Label labelPngInfo;
		private System.Windows.Forms.Label labelPath;
		private System.Windows.Forms.TextBox textBoxResourcePath;
		private System.Windows.Forms.Label labelFormat;
		private System.Windows.Forms.ComboBox comboBoxFormat;
		private System.Windows.Forms.Button buttonImport;
		private System.Windows.Forms.Button buttonCancel;
	}
}
