using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Removes a previously applied walkbox patch, restoring the patched rooms to the user's
	/// pristine game data while leaving edits to other rooms intact. When the result is identical
	/// to the pristine pak, the loose override is deleted so the pak loads directly. The work
	/// lives in <see cref="ClassicPatchService"/> so the CLI shares it.
	/// </summary>
	public class RemoveClassicPatchCommand( LPAKFile? lpakFile, string? patchPath ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			return ClassicPatchService.Remove( lpakFile, patchPath );
		}
	}
}
