using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	partial class AboutBox : System.Windows.Forms.Form
	{
		public AboutBox()
		{
			InitializeComponent();
			this.Text = String.Format( "About {0}", AssemblyTitle );
			this.labelProductName.Text = AssemblyProduct;
			this.labelVersion.Text = String.Format( "Version {0}", AssemblyVersion );
			this.labelCopyright.Text = AssemblyCopyright;
			this.labelCompanyName.Text = AssemblyCompany;
			this.textBoxDescription.Text = string.Concat(
				"Thanks to LucasArts for creating great games.",
				Environment.NewLine, Environment.NewLine,
				"Thanks to jott for his research on the file format http://www.lucasforums.com/showpost.php?p=2651346&postcount=84",
				Environment.NewLine, Environment.NewLine,
				"Originally posted to the (now defunct) LucasForums: http://www.lucasforums.com/showthread.php?p=2809988#post2809988"
				);
		}

		private const string LogoResourceName = "MonkeyIslandSpecialEditionSpriteEditor.UI.AboutBoxLogo.png";

		/// <summary>
		/// Reads the logo from the embedded PNG. The bitmap is copied so it does not keep the
		/// manifest resource stream open.
		/// </summary>
		private static Image LoadLogo()
		{
			using( var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( LogoResourceName ) )
			{
				if( stream == null )
				{
					throw new InvalidOperationException( $"Missing embedded resource '{LogoResourceName}'." );
				}
				using( var logo = new Bitmap( stream ) )
				{
					return new Bitmap( logo );
				}
			}
		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyTitleAttribute ), false );
				if( attributes.Length > 0 )
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if( titleAttribute.Title != "" )
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase );
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}

		public string AssemblyDescription
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyDescriptionAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyDescriptionAttribute)attributes[0] ).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyProductAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyProductAttribute)attributes[0] ).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyCopyrightAttribute)attributes[0] ).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCompanyAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyCompanyAttribute)attributes[0] ).Company;
			}
		}
		#endregion
	}
}
