using System;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class ExceptionForm : Form
	{
		public ExceptionForm()
		{
			this.InitializeComponent();
		}

		public void SetException( Exception exception )
		{
			this.Text = exception.GetType().FullName;
			this.textBox1.Text = exception.ToString();
			this.textBox1.Select( 0, 0 );
		}
	}
}
