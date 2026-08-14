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
		/// Applies the propagation for a node whose checkbox has just changed.
		/// </summary>
		public static void Apply( TreeNode node )
		{
			foreach( TreeNode child in node.Nodes )
			{
				SetCheckedRecursive( child, node.Checked );
			}
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
