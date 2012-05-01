using System;
using System.IO;
using System.Reflection;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Parsing
{
	public static class GenericReader
	{
		public static object Read( BinaryReader reader, Type type )
		{
			var entity = Activator.CreateInstance( type ) as IEntity;
			entity.PreProcess();
			var fields = type.GetFields( BindingFlags.Public | BindingFlags.Instance );
			foreach( var field in fields )
			{
				entity.IterationProcess();

				// handle IEntity fields
				if( typeof( IEntity ).IsAssignableFrom( field.FieldType ) )
				{
					var value = GenericReader.Read( reader, field.FieldType );
					field.SetValue( entity, value );
					continue;
				}

				// handle IEntity array fields
				if( field.FieldType.IsArray )
				{
					var elementType = field.FieldType.GetElementType();
					if( typeof( IEntity ).IsAssignableFrom( elementType ) )
					{
						var array = field.GetValue( entity ) as object[];
						for( var index = 0; index < array.Length; index++ )
						{
							var value = GenericReader.Read( reader, elementType );
							array[index] = value;
						}
						continue;
					}
				}

				// handle strings differently that other standard .net types
				if( typeof( string ).IsAssignableFrom( field.FieldType ) )
				{
					var lengthAttributes = field.GetCustomAttributes<LengthAttribute>( inherit: true );
					if( lengthAttributes != null && lengthAttributes.Length > 0 )
					{
						var length = lengthAttributes[0].Length;
						if( length <= 0 )
						{
							field.SetValue( entity, Helper.ReadStringMonkeyNoPadding( reader ) );
						}
						else
						{
							field.SetValue( entity, Helper.ReadStringMonkeyNoPadding( reader, length ) );
						}
					}
					else
					{
						field.SetValue( entity, Helper.ReadStringMonkey( reader ) );
					}
					continue;
				}

				// handle standard .net types
				{
					var binaryReaderType = typeof( BinaryReader );
					var method = binaryReaderType.GetMethod( "Read" + field.FieldType.Name, BindingFlags.Instance | BindingFlags.Public );
					if( method == null )
					{
						throw new NotSupportedException( "Type not supported: " + field.FieldType.FullName );
					}

					var value = method.Invoke( reader, null );
					field.SetValue( entity, value );
				}
			}
			entity.PostProcess();
			return entity;
		}
	}
}
