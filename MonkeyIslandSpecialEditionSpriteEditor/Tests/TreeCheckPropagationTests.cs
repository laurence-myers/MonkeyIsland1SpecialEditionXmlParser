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
