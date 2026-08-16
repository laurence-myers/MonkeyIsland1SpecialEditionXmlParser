using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class TreeCheckPropagationTests
	{
		[Test]
		public void Apply_CheckingTheOnlyGrandchild_ChecksParentAndGrandparent()
		{
			var (root, group, frame) = MakeThreeLevels();
			frame.Checked = true;

			TreeCheckPropagation.Apply( frame );

			Assert.That( group.Checked, Is.True );
			Assert.That( root.Checked, Is.True );
		}

		[Test]
		public void Apply_UncheckingTheOnlyGrandchild_ClearsParentAndGrandparent()
		{
			var (root, group, frame) = MakeThreeLevels();
			root.Checked = group.Checked = frame.Checked = true;

			frame.Checked = false;
			TreeCheckPropagation.Apply( frame );

			Assert.That( group.Checked, Is.False );
			Assert.That( root.Checked, Is.False );
		}

		[Test]
		public void Apply_UncheckingOneOfTwoChildren_LeavesTheParentCheckedUntilTheLastGoes()
		{
			var root = new TreeNode();
			var group = new TreeNode();
			var frameA = new TreeNode();
			var frameB = new TreeNode();
			root.Nodes.Add( group );
			group.Nodes.Add( frameA );
			group.Nodes.Add( frameB );
			root.Checked = group.Checked = frameA.Checked = frameB.Checked = true;

			// uncheck one: the parent stays checked because a sibling is still checked
			frameA.Checked = false;
			TreeCheckPropagation.Apply( frameA );
			Assert.That( group.Checked, Is.True );
			Assert.That( root.Checked, Is.True );

			// uncheck the last: now the parent and grandparent clear
			frameB.Checked = false;
			TreeCheckPropagation.Apply( frameB );
			Assert.That( group.Checked, Is.False );
			Assert.That( root.Checked, Is.False );
		}

		[Test]
		public void Apply_CheckingAGrandparent_ChecksEveryDescendant()
		{
			var (root, group, frame) = MakeThreeLevels();
			var frame2 = new TreeNode();
			group.Nodes.Add( frame2 );

			root.Checked = true;
			TreeCheckPropagation.Apply( root );

			Assert.That( group.Checked, Is.True );
			Assert.That( frame.Checked, Is.True );
			Assert.That( frame2.Checked, Is.True );
		}

		[Test]
		public void Apply_UncheckingAGrandparent_ClearsEveryDescendant()
		{
			var (root, group, frame) = MakeThreeLevels();
			root.Checked = group.Checked = frame.Checked = true;

			root.Checked = false;
			TreeCheckPropagation.Apply( root );

			Assert.That( group.Checked, Is.False );
			Assert.That( frame.Checked, Is.False );
		}

		[Test]
		public void Apply_CheckingADeeplyNestedNode_ChecksTheWholeAncestorChain()
		{
			// room objects branch: root -> name group -> image instance -> chunk
			var root = new TreeNode();
			var nameGroup = new TreeNode();
			var instance = new TreeNode();
			var chunk = new TreeNode();
			root.Nodes.Add( nameGroup );
			nameGroup.Nodes.Add( instance );
			instance.Nodes.Add( chunk );

			chunk.Checked = true;
			TreeCheckPropagation.Apply( chunk );

			Assert.That( instance.Checked, Is.True );
			Assert.That( nameGroup.Checked, Is.True );
			Assert.That( root.Checked, Is.True );
		}

		[Test]
		public void SetChecked_UncheckingAGroup_ClearsItsSubtreeAndRecomputesTheEntityAbove()
		{
			// entity (root) > group > two frame sprites; both frames drawn
			var entity = new TreeNode();
			var group = new TreeNode();
			var spriteA = new TreeNode();
			var spriteB = new TreeNode();
			entity.Nodes.Add( group );
			group.Nodes.Add( spriteA );
			group.Nodes.Add( spriteB );
			entity.Checked = group.Checked = spriteA.Checked = spriteB.Checked = true;

			TreeCheckPropagation.SetChecked( group, false );

			Assert.That( spriteA.Checked, Is.False, "the subtree follows the group" );
			Assert.That( spriteB.Checked, Is.False );
			Assert.That( entity.Checked, Is.False, "the entity ancestor clears when its last group goes" );
		}

		[Test]
		public void RefreshAncestors_SoloingOneSpriteInAFrame_KeepsTheGroupAndEntityChecked()
		{
			// the "show only this frame in its group" path: soloing one sprite leaf must leave the
			// group and the entity node above it checked, not stranded unchecked (finding 3)
			var entity = new TreeNode();
			var group = new TreeNode();
			var spriteA = new TreeNode();
			var spriteB = new TreeNode();
			entity.Nodes.Add( group );
			group.Nodes.Add( spriteA );
			group.Nodes.Add( spriteB );
			// start from a hidden entity (a "game default" view left everything unchecked)
			entity.Checked = group.Checked = spriteA.Checked = spriteB.Checked = false;

			// solo spriteA among its siblings, then recompute the branch above it
			spriteA.Checked = true;
			spriteB.Checked = false;
			TreeCheckPropagation.RefreshAncestors( spriteA );

			Assert.That( spriteA.Checked, Is.True, "the soloed leaf is untouched" );
			Assert.That( group.Checked, Is.True, "the group is checked because a child is" );
			Assert.That( entity.Checked, Is.True, "the entity is checked because a frame is" );
		}

		[Test]
		public void RefreshAncestors_ClearsAncestorsWhenNoChildRemainsChecked()
		{
			var (root, group, frame) = MakeThreeLevels();
			root.Checked = group.Checked = true;
			frame.Checked = false;

			TreeCheckPropagation.RefreshAncestors( frame );

			Assert.That( group.Checked, Is.False );
			Assert.That( root.Checked, Is.False );
		}

		private static (TreeNode root, TreeNode group, TreeNode frame) MakeThreeLevels()
		{
			var root = new TreeNode();
			var group = new TreeNode();
			var frame = new TreeNode();
			root.Nodes.Add( group );
			group.Nodes.Add( frame );
			return (root, group, frame);
		}
	}
}
