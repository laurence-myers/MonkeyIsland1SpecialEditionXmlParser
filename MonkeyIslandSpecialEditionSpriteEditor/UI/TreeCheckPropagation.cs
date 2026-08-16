using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Two-way checkbox propagation for a <see cref="TreeView"/> with checkboxes: a node's whole
	/// subtree follows its checked state, and each ancestor is checked exactly when at least one
	/// of its own children is checked. So checking the only frame in a group checks the group and
	/// the branch above it, and unchecking the last checked child clears them again.
	/// </summary>
	internal static class TreeCheckPropagation
	{
		/// <summary>
		/// Applies the propagation for a node whose checkbox has just changed: its subtree follows it
		/// and every ancestor is recomputed.
		/// </summary>
		public static void Apply( TreeNode node )
		{
			foreach( TreeNode child in node.Nodes )
			{
				SetCheckedRecursive( child, node.Checked );
			}
			RefreshAncestors( node );
		}

		/// <summary>
		/// Sets a node's checkbox and propagates both ways, at any depth: the subtree follows the new
		/// value and every ancestor is checked exactly when one of its children is. Use this instead of
		/// hand-rolling the ancestor recompute at a single level.
		/// </summary>
		public static void SetChecked( TreeNode node, bool value )
		{
			node.Checked = value;
			Apply( node );
		}

		/// <summary>
		/// Recomputes every ancestor of a node from its children (checked exactly when at least one
		/// child is checked), leaving the node and its subtree untouched. Use after editing leaves
		/// directly (soloing one sprite among siblings) so the branch above stays consistent.
		/// </summary>
		public static void RefreshAncestors( TreeNode node )
		{
			for( var ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent )
			{
				ancestor.Checked = AnyChildChecked( ancestor );
			}
		}

		/// <summary>
		/// Sets a node and its whole subtree to the given checked state.
		/// </summary>
		public static void SetCheckedRecursive( TreeNode node, bool value )
		{
			node.Checked = value;
			foreach( TreeNode child in node.Nodes )
			{
				SetCheckedRecursive( child, value );
			}
		}

		private static bool AnyChildChecked( TreeNode node )
		{
			foreach( TreeNode child in node.Nodes )
			{
				if( child.Checked )
				{
					return true;
				}
			}
			return false;
		}
	}
}
