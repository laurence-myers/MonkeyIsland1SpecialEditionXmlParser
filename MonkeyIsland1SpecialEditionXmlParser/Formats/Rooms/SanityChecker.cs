using System;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	public static class SanityChecker
	{
		public static void Check( Room room )
		{
			room.IsNotNull();
			room.Header.IsNotNull();
			room.Header.NameAddress.Is( 80 );
			room.Header.Name.IsNotNull();
			room.Header.AlwaysZero1.Is( 0 );
			room.Header.AlwaysZero2.Is( 0 );
			room.Header.AlwaysZero3.Is( 0 );
			room.Header.Unknown6HeaderAddress1.Is( room.Header.Unknown6HeaderAddress2 );
			room.StaticSpriteHeaderList.Count.Is( room.StaticSpriteList.Count );
		}

		private static void IsNotNull( this object value )
		{
			if( value == null )
			{
				throw new Exception( "value is null" );
			}
		}

		private static void Is<T>( this T actual, T expected )
		{
			if( !actual.Equals( expected ) )
			{
				throw new Exception( actual + " != " + expected );
			}
		}
	}
}
