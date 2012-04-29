
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK
{
	public class LPAKFile : IEntity
	{
		public PakHeader PakHeader;
		public PakIndex[] PakIndicies;
		public PakFileEntry[] PakFileEntries;
		public PakFileName[] PakFileNames;

		public override void IterationProcess()
		{
			if( this.PakHeader != null && this.PakIndicies == null )
			{
				var numberOfFiles = this.PakHeader.SizeOfIndex / 4;
				this.PakIndicies = new PakIndex[numberOfFiles];
				this.PakFileEntries = new PakFileEntry[numberOfFiles];
				this.PakFileNames = new PakFileName[numberOfFiles];
			}
		}
	}

	public class PakHeader : IEntity
	{
		[Length( 4 )]
		public string Name;
		public float Version;
		public int StartOfIndex;
		public int StartOfFileEntries;
		public int StartOfFileNames;
		public int StartOfData;
		public int SizeOfIndex;
		public int SizeOfFileEntries;
		public int SizeOfFileNames;
		public int SizeOfData;

		public override void PostProcess()
		{
			this.Name = this.Name.Reverse();
		}
	}

	public class PakIndex : IEntity
	{
		public uint Unkn1;

		public override string ToString()
		{
			return this.Unkn1.ToString();
		}
	}

	public class PakFileEntry : IEntity
	{
		public int OffsetToStartOfData;
		public int OffsetToFileName;
		public int SizeOfData1;
		public int SizeOfData2;
		public int IsCompressed;

		public override string ToString()
		{
			return string.Concat(
				this.OffsetToStartOfData, "; ",
				this.OffsetToFileName, "; ",
				this.SizeOfData1, "; ",
				this.SizeOfData2, "; ",
				this.IsCompressed
				);
		}
	}

	public class PakFileName : IEntity
	{
		[Length( -1 )]
		public string FileName;

		public override string ToString()
		{
			return this.FileName;
		}
	}

	public abstract class IEntity
	{
		public virtual void PreProcess()
		{
		}

		public virtual void IterationProcess()
		{
		}

		public virtual void PostProcess()
		{
		}
	}
}
