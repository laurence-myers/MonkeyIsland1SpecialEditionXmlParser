using System.Collections.Generic;
using System.Xml.Serialization;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// A redistributable walkbox patch. It carries only the modder's edited box records and
	/// one-way fingerprints of the pristine classic data - never the original game bytes - so it
	/// can be shared without redistributing monkey1.000/.001. An end user applies it against
	/// their own game data.
	/// </summary>
	[XmlRoot( "ClassicPatch" )]
	public class ClassicPatch
	{
		[XmlAttribute( "formatVersion" )]
		public int FormatVersion
		{
			get;
			set;
		} = 1;

		[XmlAttribute( "toolVersion" )]
		public string ToolVersion
		{
			get;
			set;
		} = "";

		public ClassicPatchInfo Info
		{
			get;
			set;
		} = new ClassicPatchInfo();

		/// <summary>
		/// The pristine classic-data variants this patch was built for (Steam EN, GOG, ...). An
		/// apply only proceeds when the user's own pristine data matches one of them.
		/// </summary>
		[XmlArray( "BaseFingerprints" )]
		[XmlArrayItem( "Fingerprint" )]
		public List<ClassicPatchFingerprint> BaseFingerprints
		{
			get;
			set;
		} = new List<ClassicPatchFingerprint>();

		/// <summary>
		/// The per-room walkbox edits (only rooms that changed, only the boxes that changed).
		/// </summary>
		[XmlArray( "RoomEdits" )]
		[XmlArrayItem( "Room" )]
		public List<ClassicPatchRoomEdit> RoomEdits
		{
			get;
			set;
		} = new List<ClassicPatchRoomEdit>();

		/// <summary>
		/// Checks the patch is well formed. Returns an error message, or null when valid.
		/// </summary>
		public string? Validate()
		{
			if( this.FormatVersion != 1 )
			{
				return string.Concat( "Unsupported patch format version ", this.FormatVersion, "." );
			}
			if( this.BaseFingerprints.Count == 0 )
			{
				return "The patch has no base fingerprints, so it cannot be matched to a game version.";
			}
			foreach( var fingerprint in this.BaseFingerprints )
			{
				if( string.IsNullOrWhiteSpace( fingerprint.DataFileSha256 ) || fingerprint.DataFileLength <= 0 )
				{
					return "The patch has an incomplete base fingerprint.";
				}
			}
			if( this.RoomEdits.Count == 0 )
			{
				return "The patch contains no room edits.";
			}
			foreach( var roomEdit in this.RoomEdits )
			{
				if( string.IsNullOrWhiteSpace( roomEdit.PostBoxdSha256 ) )
				{
					return string.Concat( "Room ", roomEdit.Number, " is missing its result fingerprint." );
				}
				foreach( var box in roomEdit.Boxes )
				{
					if( box.Index < 0 )
					{
						return string.Concat( "Room ", roomEdit.Number, " has a box with a negative index." );
					}
				}
			}
			return null;
		}
	}

	public class ClassicPatchInfo
	{
		[XmlAttribute( "name" )]
		public string Name
		{
			get;
			set;
		} = "";

		[XmlAttribute( "author" )]
		public string Author
		{
			get;
			set;
		} = "";

		[XmlAttribute( "notes" )]
		public string Notes
		{
			get;
			set;
		} = "";
	}

	public class ClassicPatchFingerprint
	{
		[XmlAttribute( "label" )]
		public string Label
		{
			get;
			set;
		} = "";

		/// <summary>
		/// The length of the pristine, still-encoded resource file (.001).
		/// </summary>
		[XmlAttribute( "dataFileLength" )]
		public long DataFileLength
		{
			get;
			set;
		}

		/// <summary>
		/// The SHA-256 of the pristine, still-encoded resource file. XOR is symmetric, so hashing
		/// the encoded bytes identifies the variant without decoding.
		/// </summary>
		[XmlAttribute( "dataFileSha256" )]
		public string DataFileSha256
		{
			get;
			set;
		} = "";

		[XmlArray( "Rooms" )]
		[XmlArrayItem( "Room" )]
		public List<ClassicPatchFingerprintRoom> Rooms
		{
			get;
			set;
		} = new List<ClassicPatchFingerprintRoom>();
	}

	public class ClassicPatchFingerprintRoom
	{
		[XmlAttribute( "number" )]
		public int Number
		{
			get;
			set;
		}

		/// <summary>
		/// SHA-256 of this room's pristine BOXD payload (the count word and the box records).
		/// </summary>
		[XmlAttribute( "boxdSha256" )]
		public string BoxdSha256
		{
			get;
			set;
		} = "";

		[XmlAttribute( "boxCount" )]
		public int BoxCount
		{
			get;
			set;
		}
	}

	public class ClassicPatchRoomEdit
	{
		[XmlAttribute( "number" )]
		public int Number
		{
			get;
			set;
		}

		/// <summary>
		/// SHA-256 of this room's BOXD payload after the patch is applied, used to confirm the
		/// splice and to detect an already-applied room.
		/// </summary>
		[XmlAttribute( "postBoxdSha256" )]
		public string PostBoxdSha256
		{
			get;
			set;
		} = "";

		[XmlArray( "Boxes" )]
		[XmlArrayItem( "Box" )]
		public List<ClassicPatchBox> Boxes
		{
			get;
			set;
		} = new List<ClassicPatchBox>();
	}

	/// <summary>
	/// One edited walkbox, addressed by its index in the room, holding the modder-authored
	/// replacement record (four corners, mask, flags, scale).
	/// </summary>
	public class ClassicPatchBox
	{
		[XmlAttribute( "index" )]
		public int Index
		{
			get;
			set;
		}

		[XmlAttribute( "x1" )] public int X1 { get; set; }
		[XmlAttribute( "y1" )] public int Y1 { get; set; }
		[XmlAttribute( "x2" )] public int X2 { get; set; }
		[XmlAttribute( "y2" )] public int Y2 { get; set; }
		[XmlAttribute( "x3" )] public int X3 { get; set; }
		[XmlAttribute( "y3" )] public int Y3 { get; set; }
		[XmlAttribute( "x4" )] public int X4 { get; set; }
		[XmlAttribute( "y4" )] public int Y4 { get; set; }
		[XmlAttribute( "mask" )] public int Mask { get; set; }
		[XmlAttribute( "flags" )] public int Flags { get; set; }
		[XmlAttribute( "scale" )] public int Scale { get; set; }
	}
}
