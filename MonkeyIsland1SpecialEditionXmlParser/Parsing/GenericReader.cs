using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Parsing
{
	public static class GenericReader
	{
		public static object Read( BinaryReader reader, Type type, IEntity entityParent, int entityIndex )
		{
			var entity = Activator.CreateInstance( type ) as IEntity;
			entity.PreProcess();
			
			var fields = type.GetFields( BindingFlags.Public | BindingFlags.Instance ).OfType<MemberInfo>();
			var properties = type.GetProperties( BindingFlags.Public | BindingFlags.Instance ).OfType<MemberInfo>();
			var members = fields.Union( properties ).ToArray();

			foreach( var member in members )
			{
				var memberType = member.GetFieldOrPropertyType();

				if( member.Name == "EntityParent" )
				{
					member.SetMemberValue( entity, entityParent );
					continue;
				}
				else if( member.Name == "EntityIndex" )
				{
					member.SetMemberValue( entity, entityIndex );
					continue;
				}

				entity.IterationProcess();

				var ignoreAttributes = member.GetCustomAttributes<IgnoreAttribute>( inherit: true );
				if( ignoreAttributes != null && ignoreAttributes.Length > 0 )
				{
					continue;
				}

				// handle IEntity fields
				if( typeof( IEntity ).IsAssignableFrom( memberType ) )
				{
					var value = GenericReader.Read( reader, memberType, entity, 0 );
					member.SetMemberValue( entity, value );
					continue;
				}

				// handle IEntity array fields
				if( memberType.IsArray )
				{
					var elementType = memberType.GetElementType();
					var array = member.GetMemberValue( entity ) as object[];
					if( typeof( IEntity ).IsAssignableFrom( elementType ) )
					{
						for( var index = 0; index < array.Length; index++ )
						{
							var value = GenericReader.Read( reader, elementType, entity, index );
							array[index] = value;
						}
						continue;
					}
					var binaryReaderType = typeof( BinaryReader );
					var method = binaryReaderType.GetMethod( "Read" + memberType.Name, BindingFlags.Instance | BindingFlags.Public );
					if( method == null )
					{
						throw new NotSupportedException( "Type not supported: " + memberType.FullName );
					}
					for( var index = 0; index < array.Length; index++ )
					{
						var value = method.Invoke( reader, null );
						array[index] = value;
					}
					continue;
				}

				// handle strings differently that other standard .net types
				if( typeof( string ).IsAssignableFrom( memberType ) )
				{
					var lengthAttributes = member.GetCustomAttributes<LengthAttribute>( inherit: true );
					if( lengthAttributes != null && lengthAttributes.Length > 0 )
					{
						var length = lengthAttributes[0].Length;
						if( length <= 0 )
						{
							member.SetMemberValue( entity, Helper.ReadStringMonkeyNoPadding( reader ) );
						}
						else
						{
							member.SetMemberValue( entity, Helper.ReadStringMonkeyNoPadding( reader, length ) );
						}
					}
					else
					{
						member.SetMemberValue( entity, Helper.ReadStringMonkey( reader ) );
					}
					continue;
				}

				// handle standard .net types
				{
					var binaryReaderType = typeof( BinaryReader );
					var method = binaryReaderType.GetMethod( "Read" + memberType.Name, BindingFlags.Instance | BindingFlags.Public );
					if( method == null )
					{
						throw new NotSupportedException( "Type not supported: " + memberType.FullName );
					}

					var value = method.Invoke( reader, null );
					member.SetMemberValue( entity, value );
				}
			}
			entity.PostProcess();
			return entity;
		}
	}
}
