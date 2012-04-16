
namespace MonkeyIsland1SpecialEditionXmlParser
{
	class Program
	{
		static void Main( string[] args )
		{
			var costume21 = Parser.Parse( @"Data\Costumes\21_test-skin.costume.xml" );
			var costume44 = Parser.Parse( @"Data\Costumes\44_test-skin.costume.xml" );
			var costume72 = Parser.Parse( @"Data\Costumes\72_test.costume.xml" );
			var costume124 = Parser.Parse( @"Data\Costumes\124_test-object.costume.xml" );
		}
	}
}
