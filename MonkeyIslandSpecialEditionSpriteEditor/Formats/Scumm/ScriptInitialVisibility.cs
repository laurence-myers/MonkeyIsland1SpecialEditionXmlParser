using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Derives which of a room's objects are drawn when the room is first entered, from the
	/// object visibility changes a static <see cref="ScriptScanner"/> pass recovered.
	///
	/// The scan is control-flow blind, so this is a heuristic, grounded in what the retail
	/// scripts actually do:
	/// <list type="bullet">
	/// <item>Objects the room's own scripts never touch are static scenery, drawn by default
	/// (e.g. the bar's patrons) - so an untouched object is Visible.</item>
	/// <item>Scripts mostly HIDE the puzzle overlays that should not show at the start - the
	/// closed safe, the un-thrown lever, the un-picked banana wall - with setState 0.</item>
	/// <item>The room ENTRY script runs on every entry and asserts the baseline, so it wins.</item>
	/// <item>Global/inventory scripts (give/take handlers) are kept for reference but do not
	/// decide a room's default, or every takeable item would read as hidden.</item>
	/// </list>
	/// Objects a room's scripts both show and hide (with no decisive entry script) are genuine
	/// plot-conditional "scenario" objects; they are hidden by default and flagged Ambiguous so
	/// the UI can mark them and a later pass can separate the scenarios.
	/// </summary>
	public static class ScriptInitialVisibility
	{
		public enum Verdict
		{
			/// <summary>Drawn in the initial view (untouched scenery, or a room script shows it).</summary>
			Visible,

			/// <summary>Hidden in the initial view (a room script sets state 0 and never shows it).</summary>
			Hidden,

			/// <summary>A room script both shows and hides it: plot-conditional. Hidden by default, flagged.</summary>
			Ambiguous,
		}

		/// <summary>
		/// Computes a verdict for every object number the room defines. Objects with no decisive
		/// evidence are <see cref="Verdict.Visible"/> (untouched scenery). Only the room's own
		/// entry/local/exit scripts decide; changes from other rooms' or global scripts are ignored
		/// for the verdict.
		/// </summary>
		/// <param name="roomNumber">The room whose objects to judge.</param>
		/// <param name="objectIds">Every object number defined in the room (from the SE room or the classic OBCDs).</param>
		/// <param name="changes">The object draw changes attributed to the room (typically <see cref="ClassicRoom.ObjectDrawChanges"/>).</param>
		public static IReadOnlyDictionary<int, Verdict> Compute( int roomNumber, IEnumerable<int> objectIds, IEnumerable<ObjectDrawChange> changes )
		{
			var verdicts = new Dictionary<int, Verdict>();

			// only this room's own entry/local/exit scripts speak to its default appearance; a
			// global script is merely stored in some room's LFLF and must never decide a room
			var ownChanges = changes
				.Where( c => c.SourceKind != ScriptSourceKind.Global && c.SourceRoom == roomNumber )
				.GroupBy( c => c.ObjectId )
				.ToDictionary( g => g.Key, g => g.OrderBy( c => c.Order ).ToList() );

			foreach( var objectId in objectIds.Distinct() )
			{
				List<ObjectDrawChange> objectChanges;
				if( !ownChanges.TryGetValue( objectId, out objectChanges ) )
				{
					// untouched by this room's scripts: static scenery, drawn by default
					verdicts[objectId] = Verdict.Visible;
					continue;
				}

				verdicts[objectId] = JudgeObject( objectChanges );
			}

			return verdicts;
		}

		private static Verdict JudgeObject( List<ObjectDrawChange> objectChanges )
		{
			// the entry script asserts the room's baseline every time it is entered, so its last
			// setState/drawObject wins outright
			var entryDecisive = objectChanges
				.Where( c => c.SourceKind == ScriptSourceKind.Entry
					&& ( c.Kind == ObjectDrawKind.SetState || c.Kind == ObjectDrawKind.Draw ) )
				.OrderBy( c => c.Order )
				.LastOrDefault();
			if( entryDecisive != null )
			{
				return entryDecisive.MakesVisible ? Verdict.Visible : Verdict.Hidden;
			}

			// otherwise weigh the local/exit scripts: only-shows -> visible, only-hides -> hidden,
			// both -> a plot-conditional object we cannot resolve statically. Both setState 0 and
			// the rare drawObject with an explicit state 0 hide, mirroring MakesVisible.
			var anyShows = objectChanges.Any( c =>
				( c.Kind == ObjectDrawKind.SetState || c.Kind == ObjectDrawKind.Draw ) && c.MakesVisible );
			var anyHides = objectChanges.Any( c =>
				( c.Kind == ObjectDrawKind.SetState || c.Kind == ObjectDrawKind.Draw ) && !c.MakesVisible );

			if( anyShows && anyHides )
			{
				return Verdict.Ambiguous;
			}
			if( anyShows )
			{
				return Verdict.Visible;
			}
			if( anyHides )
			{
				return Verdict.Hidden;
			}

			// only pickup/setOwner touched it (inventory mechanics, no state change): leave it drawn
			return Verdict.Visible;
		}
	}
}
