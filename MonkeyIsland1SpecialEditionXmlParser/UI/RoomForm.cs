using System.Collections.Generic;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class RoomForm : Form
	{
		private static readonly List<RoomForm> instances = new List<RoomForm>();

		public Room Room
		{
			get;
			set;
		}

		public static RoomForm[] Instances
		{
			get
			{
				return RoomForm.instances.ToArray();
			}
		}

		public RoomForm()
		{
			RoomForm.instances.Add( this );
			this.FormClosed += delegate { RoomForm.instances.Remove( this ); };

			this.InitializeComponent();
		}
	}
}
