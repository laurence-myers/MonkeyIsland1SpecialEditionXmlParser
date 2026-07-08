using System;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms
{
	public static class SanityChecker
	{
		public static void Check( Room room )
		{
			room.IsNotNull();
			room.Header.IsNotNull();
			room.Header.Name.IsNotNull();
			room.Header.AlwaysZero1.Is( 0 );
			room.Header.AlwaysZero2.Is( 0 );
			room.Header.AlwaysZero3.Is( 0 );
			// the empty "A" variant of the unknown6 section shares the real section's table
			room.Header.Unknown6HeaderAddressA.Is( room.Header.Unknown6HeaderAddress );
			room.StaticSpriteHeaderList.Count.Is( room.StaticSpriteList.Count );
			room.SpriteHeaderList.Count.Is( room.SpriteGroupList.Count );
			room.RoomObjectHeaderList.Count.Is( room.RoomObjectGroupList.Count );
			// a room object is drawn either as a sprite or as a chunked image, never both
			room.RoomObjectGroupList
				.All( group => group.RoomObjectList.All( roomObject => roomObject.Sprite == null || roomObject.Image == null ) )
				.Is( true );
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
			if( (actual is null && expected is not null) ||
			   (actual is not null && expected is null) ||
			   (actual is not null && !actual.Equals( expected ) ))
			{
				throw new Exception( actual + " != " + expected );
			}
		}
	}
}
