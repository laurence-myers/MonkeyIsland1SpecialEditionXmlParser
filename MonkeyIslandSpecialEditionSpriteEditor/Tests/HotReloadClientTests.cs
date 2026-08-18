using MonkeyIslandSpecialEditionSpriteEditor.PlayTest;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class HotReloadClientTests
	{
		// These run with no mise-hotreload.dll injected in the test process, so the named event does
		// not exist. The contract is a graceful no-op (false), never an exception — that is what lets
		// TestInGameCommand fall back to the walk-out/in play-test loop.

		[Test]
		public void IsAvailable_WhenDllNotInjected_ReturnsFalseWithoutThrowing()
		{
			Assert.That( HotReloadClient.IsAvailable(), Is.False );
		}

		[Test]
		public void TrySignal_WhenDllNotInjected_ReturnsFalseWithoutThrowing()
		{
			Assert.That( HotReloadClient.TrySignal(), Is.False );
		}

		[Test]
		public void EventName_MatchesTheNameTheDllCreates()
		{
			// Must stay in sync with EVENT_NAME in se-file-hook/reload.c.
			Assert.That( HotReloadClient.EventName, Is.EqualTo( "Local\\MISE_HotReload" ) );
		}
	}
}
