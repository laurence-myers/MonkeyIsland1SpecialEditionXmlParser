using System;

namespace MonkeyIslandSpecialEditionSpriteEditor.Parsing
{
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
	public class IgnoreAttribute : Attribute
	{
	}
}
