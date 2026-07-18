namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	partial class TexturePickerDialog
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				this.previewBitmap?.Dispose();
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			this.labelFilter = new System.Windows.Forms.Label();
			this.textBoxFilter = new System.Windows.Forms.TextBox();
			this.listBoxTextures = new System.Windows.Forms.ListBox();
			this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
			this.labelInfo = new System.Windows.Forms.Label();
			this.buttonImportNew = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			( (System.ComponentModel.ISupportInitialize)this.pictureBoxPreview ).BeginInit();
			this.SuspendLayout();
			//
			// labelFilter
			//
			this.labelFilter.AutoSize = true;
			this.labelFilter.Location = new System.Drawing.Point( 12, 15 );
			this.labelFilter.Name = "labelFilter";
			this.labelFilter.Size = new System.Drawing.Size( 32, 13 );
			this.labelFilter.TabIndex = 0;
			this.labelFilter.Text = "Filter:";
			//
			// textBoxFilter
			//
			this.textBoxFilter.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.textBoxFilter.Location = new System.Drawing.Point( 60, 12 );
			this.textBoxFilter.Name = "textBoxFilter";
			this.textBoxFilter.Size = new System.Drawing.Size( 572, 20 );
			this.textBoxFilter.TabIndex = 1;
			//
			// listBoxTextures
			//
			this.listBoxTextures.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom ) | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.listBoxTextures.FormattingEnabled = true;
			this.listBoxTextures.HorizontalScrollbar = true;
			this.listBoxTextures.IntegralHeight = false;
			this.listBoxTextures.Location = new System.Drawing.Point( 12, 44 );
			this.listBoxTextures.Name = "listBoxTextures";
			this.listBoxTextures.Size = new System.Drawing.Size( 300, 373 );
			this.listBoxTextures.TabIndex = 2;
			//
			// pictureBoxPreview
			//
			this.pictureBoxPreview.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom ) | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.pictureBoxPreview.BackColor = System.Drawing.SystemColors.ControlDark;
			this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBoxPreview.Location = new System.Drawing.Point( 324, 44 );
			this.pictureBoxPreview.Name = "pictureBoxPreview";
			this.pictureBoxPreview.Size = new System.Drawing.Size( 308, 349 );
			this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBoxPreview.TabIndex = 3;
			this.pictureBoxPreview.TabStop = false;
			//
			// labelInfo
			//
			this.labelInfo.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.labelInfo.AutoEllipsis = true;
			this.labelInfo.Location = new System.Drawing.Point( 324, 398 );
			this.labelInfo.Name = "labelInfo";
			this.labelInfo.Size = new System.Drawing.Size( 308, 19 );
			this.labelInfo.TabIndex = 4;
			//
			// buttonImportNew
			//
			this.buttonImportNew.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.buttonImportNew.Location = new System.Drawing.Point( 12, 428 );
			this.buttonImportNew.Name = "buttonImportNew";
			this.buttonImportNew.Size = new System.Drawing.Size( 190, 23 );
			this.buttonImportNew.TabIndex = 5;
			this.buttonImportNew.Text = "Import PNG as new texture...";
			this.buttonImportNew.UseVisualStyleBackColor = true;
			//
			// buttonOK
			//
			this.buttonOK.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point( 476, 428 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 75, 23 );
			this.buttonOK.TabIndex = 6;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 557, 428 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 75, 23 );
			this.buttonCancel.TabIndex = 7;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// TexturePickerDialog
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 644, 463 );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.buttonImportNew );
			this.Controls.Add( this.labelInfo );
			this.Controls.Add( this.pictureBoxPreview );
			this.Controls.Add( this.listBoxTextures );
			this.Controls.Add( this.textBoxFilter );
			this.Controls.Add( this.labelFilter );
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size( 480, 360 );
			this.Name = "TexturePickerDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose texture";
			( (System.ComponentModel.ISupportInitialize)this.pictureBoxPreview ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label labelFilter;
		private System.Windows.Forms.TextBox textBoxFilter;
		private System.Windows.Forms.ListBox listBoxTextures;
		private System.Windows.Forms.PictureBox pictureBoxPreview;
		private System.Windows.Forms.Label labelInfo;
		private System.Windows.Forms.Button buttonImportNew;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
	}
}
