using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes
{
	/// <summary>
	/// Writes a costume using the same layout rules as the original files, so that an
	/// unmodified costume repacks byte-identically (verified against all 122 costumes in
	/// the retail pak).
	///
	/// Section order: header, name, texture headers, animation headers, sprite group
	/// headers, path points, texture file names, per animation its name plus track records,
	/// per group its sprite list, per track its frame list, shared sound name strings.
	///
	/// Padding rules: every item's size (a string's length including its terminator, a
	/// list's byte size) is rounded up to a multiple of 16, but the padding is emitted
	/// lazily - only when the next item is written - so the file ends unpadded. An empty
	/// track flushes the pending padding and stores the resulting position as its frame
	/// list address; an empty sprite group stores address 0.
	/// </summary>
	public static class Packer
	{
		/// <summary>
		/// Writes a Costume to a single .dat file.
		/// </summary>
		/// <param name="fileName">The path to the .dat file.</param>
		/// <param name="costume">The Costume object to write.</param>
		public static void WriteCostumeToBinaryFile( string fileName, Costume costume )
		{
			using( var stream = File.Create( fileName ) )
			using( var writer = new BinaryWriter( stream ) )
			{
				WriteCostume( writer, costume );
			}
		}

		public static void WriteCostume( BinaryWriter writer, Costume costume )
		{
			long startPosition = writer.BaseStream.Position;
			long pendingPadding = 0;

			void Flush()
			{
				for( ; pendingPadding > 0; pendingPadding-- )
				{
					writer.Write( (byte)0 );
				}
			}

			// writes a zero-terminated string and leaves its padding pending; the string's
			// length including the terminator is rounded up to a multiple of 16 (matching
			// ReadStringMonkey, whose padding counts from the terminator's position)
			long WriteString( string text )
			{
				Flush();
				var address = writer.BaseStream.Position;
				var bytes = System.Text.Encoding.ASCII.GetBytes( text );
				writer.Write( bytes );
				writer.Write( (byte)0 );
				pendingPadding = ( 16 - ( bytes.Length + 1 ) % 16 ) % 16;
				return address;
			}

			// leaves the padding of a just-written list pending; lists are only padded when
			// their size is not already a multiple of 16
			void EndList( long listStart )
			{
				pendingPadding = ( 16 - ( writer.BaseStream.Position - listStart ) % 16 ) % 16;
			}

			// 1. Header placeholder (80 bytes)
			writer.Write( new byte[80] );

			// 2. Costume name
			long nameAddress = WriteString( costume.Header.Name );

			// 3. Texture header placeholders, one record per two texture file names
			Flush();
			long textureHeaderAddress = writer.BaseStream.Position;
			var textureHeaderCount = ( costume.TextureFileNameList.Count + 1 ) / 2;
			writer.Write( new byte[textureHeaderCount * 16] );

			// 4. Animation header placeholders
			long animationHeaderAddress = writer.BaseStream.Position;
			writer.Write( new byte[costume.AnimationList.Count * 16] );

			// 5. Sprite group header placeholders
			long spriteGroupHeaderAddress = writer.BaseStream.Position;
			writer.Write( new byte[costume.SpriteGroupList.Count * 16] );

			// 6. Path points. The header's path point address points here even when there
			// are no path points.
			long pathPointAddress = writer.BaseStream.Position;
			if( costume.PathPointList != null && costume.PathPointList.Count > 0 )
			{
				foreach( var pathPoint in costume.PathPointList )
				{
					writer.Write( pathPoint.Type );
					writer.Write( pathPoint.Flag );
					writer.Write( pathPoint.UnknownByte3 );
					writer.Write( pathPoint.UnknownByte4 );
					writer.Write( pathPoint.X );
					writer.Write( pathPoint.Y );
				}
				EndList( pathPointAddress );
			}

			// 7. Texture file names
			var textureFileNameAddresses = new List<long>();
			foreach( var textureFileName in costume.TextureFileNameList )
			{
				textureFileNameAddresses.Add( WriteString( textureFileName.Path ) );
			}

			// 8. Per animation: name, then its track records (patched later)
			var animationNameAddresses = new List<long>();
			var trackTableAddresses = new List<long>();
			foreach( var animation in costume.AnimationList )
			{
				animationNameAddresses.Add( WriteString( animation.Name ) );

				Flush();
				trackTableAddresses.Add( writer.BaseStream.Position );
				writer.Write( new byte[animation.AnimationFrameList.Count * 16] );
			}

			// 9. Per sprite group: its sprite list. Empty groups store address 0.
			var spriteListAddresses = new List<long>();
			foreach( var spriteGroup in costume.SpriteGroupList )
			{
				if( spriteGroup.SpriteList.Count == 0 )
				{
					spriteListAddresses.Add( 0 );
					continue;
				}
				Flush();
				var listStart = writer.BaseStream.Position;
				spriteListAddresses.Add( listStart );
				foreach( var sprite in spriteGroup.SpriteList )
				{
					writer.Write( sprite.TextureNumber );
					writer.Write( sprite.TextureX );
					writer.Write( sprite.TextureY );
					writer.Write( sprite.TextureWidth );
					writer.Write( sprite.TextureHeight );
					writer.Write( sprite.ScreenX );
					writer.Write( sprite.ScreenY );
					writer.Write( sprite.MoveX );
					writer.Write( sprite.MoveY );
					writer.Write( sprite.PathPointIndex );
				}
				EndList( listStart );
			}

			// 10. Per animation, per track: its frame list. Sound name pointers are recorded
			// as fix-ups and patched after the shared strings are written.
			var frameListAddresses = new List<List<long>>();
			var soundNameFixUps = new List<KeyValuePair<long, string>>();
			foreach( var animation in costume.AnimationList )
			{
				var addresses = new List<long>();
				frameListAddresses.Add( addresses );
				foreach( var track in animation.AnimationFrameList )
				{
					Flush();
					var listStart = writer.BaseStream.Position;
					addresses.Add( listStart );
					var frameList = track.FrameList ?? new List<Frame>();
					foreach( var frame in frameList )
					{
						writer.Write( frame.SpriteIdentifier );
						writer.Write( frame.Command );
						if( !string.IsNullOrEmpty( frame.SoundName ) )
						{
							soundNameFixUps.Add( new KeyValuePair<long, string>( writer.BaseStream.Position, frame.SoundName! ) );
						}
						writer.Write( 0 );
					}
					EndList( listStart );
				}
			}

			// 11. Shared sound name strings, de-duplicated, in first-reference order
			var soundNameAddresses = new Dictionary<string, long>();
			foreach( var fixUp in soundNameFixUps )
			{
				if( !soundNameAddresses.ContainsKey( fixUp.Value ) )
				{
					soundNameAddresses.Add( fixUp.Value, WriteString( fixUp.Value ) );
				}
			}

			// the padding of the very last item is dropped
			long endOfFile = writer.BaseStream.Position;

			// --- BACKPATCHING ---

			// Patch sound name pointers
			foreach( var fixUp in soundNameFixUps )
			{
				writer.BaseStream.Position = fixUp.Key;
				writer.Write( (int)( soundNameAddresses[fixUp.Value] - fixUp.Key ) );
			}

			// Patch texture headers; the sprite counts are recomputed from the sprites that
			// reference each texture
			var textureSpriteCounts = CountSpritesPerTexture( costume );
			writer.BaseStream.Position = textureHeaderAddress;
			for( var i = 0; i < textureHeaderCount; i++ )
			{
				WriteTextureHeaderHalf( writer, textureSpriteCounts, textureFileNameAddresses, i * 2 );
				WriteTextureHeaderHalf( writer, textureSpriteCounts, textureFileNameAddresses, i * 2 + 1 );
			}

			// Patch animation headers
			writer.BaseStream.Position = animationHeaderAddress;
			for( var i = 0; i < costume.AnimationList.Count; i++ )
			{
				var animation = costume.AnimationList[i];
				writer.Write( (int)( animationNameAddresses[i] - writer.BaseStream.Position ) );
				writer.Write( costume.AnimationHeaderList[i].Identifier );
				writer.Write( animation.AnimationFrameList.Count );
				writer.Write( (int)( trackTableAddresses[i] - writer.BaseStream.Position ) );
			}

			// Patch track records
			for( var i = 0; i < costume.AnimationList.Count; i++ )
			{
				writer.BaseStream.Position = trackTableAddresses[i];
				var animation = costume.AnimationList[i];
				for( var t = 0; t < animation.AnimationFrameList.Count; t++ )
				{
					var track = animation.AnimationFrameList[t];
					writer.Write( track.SpriteGroupIdentifier );
					writer.Write( track.PlaybackFlags );
					writer.Write( track.FrameList?.Count ?? 0 );
					writer.Write( (int)( frameListAddresses[i][t] - writer.BaseStream.Position ) );
				}
			}

			// Patch sprite group headers
			writer.BaseStream.Position = spriteGroupHeaderAddress;
			for( var i = 0; i < costume.SpriteGroupList.Count; i++ )
			{
				var spriteGroup = costume.SpriteGroupList[i];
				writer.Write( spriteGroup.Identifier );
				writer.Write( spriteGroup.FirstSpriteIdentifier );
				writer.Write( spriteGroup.SpriteList.Count );
				writer.Write( spriteListAddresses[i] > 0 ? (int)( spriteListAddresses[i] - writer.BaseStream.Position ) : 0 );
			}

			// Patch main header
			writer.BaseStream.Position = startPosition;
			writer.Write( costume.Header.Identifier );
			writer.Write( (int)( nameAddress - writer.BaseStream.Position ) );
			writer.Write( costume.TextureFileNameList.Count );
			writer.Write( (int)( textureHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( costume.AnimationList.Count );
			writer.Write( (int)( animationHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( costume.Header.UnknownInteger1 );
			writer.Write( costume.SpriteGroupList.Count );
			writer.Write( (int)( spriteGroupHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( CountPathPointTypes( costume ) );
			writer.Write( costume.PathPointList?.Count ?? 0 );
			writer.Write( (int)( pathPointAddress - writer.BaseStream.Position ) );
			writer.Write( costume.Header.UnknownInteger5 );
			writer.Write( costume.Header.UnknownInteger6 );
			writer.Write( costume.Header.UnknownFloat7 );
			writer.Write( costume.Header.UnknownFloat8 );
			writer.Write( costume.Header.UnknownFloat9 );
			writer.Write( costume.Header.UnknownInteger10 );
			writer.Write( costume.Header.UnknownInteger11 );
			writer.Write( costume.Header.UnknownInteger12 );

			writer.BaseStream.Position = endOfFile;
		}

		/// <summary>
		/// The number of distinct path point types: the highest <see cref="PathPoint.Type"/>
		/// plus one, or 0 without path points (matches the header field in every retail
		/// costume).
		/// </summary>
		private static int CountPathPointTypes( Costume costume )
		{
			var count = 0;
			if( costume.PathPointList != null )
			{
				foreach( var pathPoint in costume.PathPointList )
				{
					if( pathPoint.Type + 1 > count )
					{
						count = pathPoint.Type + 1;
					}
				}
			}
			return count;
		}

		/// <summary>
		/// Counts, per texture index, the sprites that reference it (TextureNumber -1 is not
		/// counted).
		/// </summary>
		private static int[] CountSpritesPerTexture( Costume costume )
		{
			var counts = new int[costume.TextureFileNameList.Count];
			foreach( var spriteGroup in costume.SpriteGroupList )
			{
				foreach( var sprite in spriteGroup.SpriteList )
				{
					if( sprite.TextureNumber >= 0 && sprite.TextureNumber < counts.Length )
					{
						counts[sprite.TextureNumber]++;
					}
				}
			}
			return counts;
		}

		private static void WriteTextureHeaderHalf( BinaryWriter writer, int[] counts, List<long> addresses, int textureIndex )
		{
			if( textureIndex < addresses.Count )
			{
				writer.Write( counts[textureIndex] );
				writer.Write( (int)( addresses[textureIndex] - writer.BaseStream.Position ) );
			}
			else
			{
				writer.Write( 0 );
				writer.Write( 0 );
			}
		}
	}
}
