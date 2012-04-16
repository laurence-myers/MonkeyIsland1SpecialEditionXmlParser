
namespace MonkeyIsland1SpecialEditionXmlParser
{
	class Program
	{
		static void Main( string[] args )
		{
			var stanFile = Parser.Parse( @"C:\Users\robin\Desktop\LucasRipper\MI1SE\art\costumes\21_stan-skin.costume.xml" );
			var guyFighting = Parser.Parse( @"C:\Users\robin\Desktop\LucasRipper\MI1SE\art\costumes\44_guy-fighting-skin.costume.xml" );
			var guybrushButt = Parser.Parse( @"C:\Users\robin\Desktop\LucasRipper\MI1SE\art\costumes\72_guybrush-butt.costume.xml" );
			var paralax2 = Parser.Parse( @"C:\Users\robin\Desktop\LucasRipper\MI1SE\art\costumes\124_paralax2-object.costume.xml" );
		}
	}
}
