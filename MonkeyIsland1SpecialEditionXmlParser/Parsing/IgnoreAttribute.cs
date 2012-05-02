using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Parsing
{
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
	public class IgnoreAttribute : Attribute
	{
	}
}
