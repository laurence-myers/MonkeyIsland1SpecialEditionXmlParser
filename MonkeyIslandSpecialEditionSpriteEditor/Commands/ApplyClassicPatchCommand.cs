using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Applies a redistributable walkbox patch to the user's own game data, writing the result as
	/// a loose classic-data override beside the pak (the pak stays pristine). Refuses when the
	/// patch was built for different game data, or when a target room was changed by something
	/// else. The work lives in <see cref="ClassicPatchService"/> so the CLI shares it.
	/// </summary>
	public class ApplyClassicPatchCommand( LPAKFile? lpakFile, string? patchPath ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			return ClassicPatchService.Apply( lpakFile, patchPath );
		}
	}
}
