using System;
using MonkeyIsland1SpecialEditionXmlParser.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class SanityChecker
	{
		public static void IsSane21( Costume costume )
		{
			costume.IsNotNull();
			costume.Header.IsNotNull();
			costume.Header.Identifier.Is( 21 );
			costume.Header.NameAddress.Is( 80 );
			costume.Header.TextureFileNameCount.Is( 3 );
			costume.Header.AfterNameAddress.Is( 96 );
			costume.Header.AnimationCount.Is( 27 );
			costume.Header.Name.Is( "test-skin" );

			costume.TextureFileNameList.IsNotNull();
			costume.TextureFileNameList.Count.Is( 3 );
			costume.TextureFileNameList[0].Path.Is( "art/costumes/images/21_test-skin/costumes_a0.dxt" );
			costume.TextureFileNameList[1].Path.Is( "art/costumes/images/21_test-skin/costumes_a1.dxt" );
			costume.TextureFileNameList[2].Path.Is( "art/costumes/images/21_test-skin/costumes_a2.dxt" );

			costume.AnimationHeaderList.IsNotNull();
			costume.AnimationHeaderList.Count.Is( 27 );

			costume.AnimationList.IsNotNull();
			costume.AnimationList.Count.Is( 27 );
			costume.AnimationList[0].Name.Is( "InitLeft" );
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
