using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DevExpress.XtraTreeList.Nodes;

using Clifton.Tools.Data;

using Intertexti.Controls;
using Intertexti.DevExpressControls;
using Intertexti.Models;

namespace Intertexti.Views
{
	public class IndexView : PaneView
	{
		public ApplicationModel Model { get; protected set; }
		public DxTreeList TreeView { get; protected set; }
		public override string MenuName { get { return "mnuIndex"; } }
		public string CurrentIndexText { get { return TreeView.GetSelectedNodeText(1); } }

		public override void EndInit()
		{
			Model.TagsChanged += TagsChanged;
			Model.NotecardDeleted += NotecardDeleted;
			base.EndInit();
		}

		protected override void WhenHandleDestroyed(object sender, EventArgs e)
		{
			Model.TagsChanged -= TagsChanged;
			Model.NotecardDeleted -= NotecardDeleted;
			base.WhenHandleDestroyed(sender, e);
		}

		public void Clear()
		{
			TreeView.Clear();
		}

		public void RefreshView()
		{
			Dictionary<string, List<NotecardRecord>> tagRecordMap;

			TreeView.Clear();
			tagRecordMap = BuildTagRecordMap();

			// Sort the list by tag value.
			var orderedIndexList = tagRecordMap.OrderBy((item)=>item.Key);

			BuildTree(TreeView, orderedIndexList);
			// Sort by the title (the index name)
			TreeView.Columns[0].SortIndex = 0;
		}

		protected Dictionary<string, List<NotecardRecord>> BuildTagRecordMap()
		{
			Dictionary<string, List<NotecardRecord>> tagRecordMap = new Dictionary<string, List<NotecardRecord>>();

			// Build the view model, which is a list of references for tag item.
			Model.ForEachNotecard(rec =>
			{
				Model.GetTags(rec).Where(t=>!String.IsNullOrEmpty(t)).ForEach(t =>
				{
					List<NotecardRecord> records;

					if (!tagRecordMap.TryGetValue(t, out records))
					{
						records = new List<NotecardRecord>();
						tagRecordMap[t] = records;
					}

					records.Add(rec);
				});
			});

			return tagRecordMap;
		}

		protected void BuildTree(DxTreeList TreeView, IOrderedEnumerable<KeyValuePair<string, List<NotecardRecord>>> orderedIndexList)
		{									  
			orderedIndexList.ForEach(item =>
			{
				string text = (item.Value.Count == 1 ? item.Key + " (" + item.Value[0].GetTitle() + ")" : item.Key) ?? String.Empty;
				string indexEntry = item.Key ?? String.Empty;

				if (item.Value.Count == 1)
				{
					// Only one notecard for this index item, so set the node's tag to the notecard record.
					TreeView.AddNode(null, new object[] {text, indexEntry}, item.Value[0], false);
				}
				else if (item.Value.Count > 1)
				{
					object parent = TreeView.AddNode(null, text, null, false);

					// Multiple notecards for this index item, so create child nodes and set the node's tag to the associated notecard record.
					item.Value.ForEach(rec =>
					{
						TreeView.AddNode(parent, new object[] {rec.GetTitle(), indexEntry}, rec, false);
					});
				}
			});
		}

		protected void TagsChanged(NotecardRecord rec)
		{
			RefreshIndices();
		}

		protected void NotecardDeleted(NotecardRecord rec)
		{
			RefreshIndices();
		}

		protected void RefreshIndices()
		{
			// Get a new TreeView:
			Dictionary<string, List<NotecardRecord>> tagRecordMap = BuildTagRecordMap();

			// Sort the list by tag value.
			var orderedIndexList = tagRecordMap.OrderBy((item) => item.Key);
			DxTreeList tvNew = new DxTreeList();
			// A column is necessary in order for the TreeListNode[index] to be non-null.  Sigh.
			tvNew.TreeListColumns.Add(new DxTreeListColumn() { Caption = "Title", Visible = true, VisibleIndex = 0, Width = 100 });
			// EndInit initializes the columns for the tree view.
			tvNew.EndInit();
			BuildTree(tvNew, orderedIndexList);
			MergeTrees(TreeView, tvNew, 
				(a, b) => a[0].ToString().CompareTo(b[0].ToString()), 
				(tln) => new object[] {tln[0]});
		}

		public static void MergeTrees(DxTreeList dest, DxTreeList src, Func<TreeListNode, TreeListNode, int> comparer, Func<TreeListNode, object[]> getColumns)
		{
			TreeListNodes destNodes = dest.Nodes;
			TreeListNodes srcNodes = src.Nodes;
			MergeTrees(dest, destNodes, srcNodes, null, comparer, getColumns);
		}

		public static void MergeTrees(DxTreeList dest, TreeListNodes destNodes, TreeListNodes srcNodes, TreeListNode parent, Func<TreeListNode, TreeListNode, int> comparer, Func<TreeListNode, object[]> getColumns)
		{
			List<TreeListNode> destNodeList = Sort(destNodes, comparer);
			List<TreeListNode> srcNodeList = Sort(srcNodes, comparer);

			int destIdx = 0;
			int srcIdx = 0;
			bool more;

			do
			{
				more = Step(dest, destNodeList, srcNodeList, ref destIdx, ref srcIdx, destNodes, parent, comparer, getColumns);
			} while (more);
		}

		public static List<TreeListNode> Sort(TreeListNodes nodes, Func<TreeListNode, TreeListNode, int> comparer)
		{
			List<TreeListNode> sortedNodes = new List<TreeListNode>();
			nodes.ForEach(n => sortedNodes.Add(n));
			sortedNodes.Sort((a, b) => comparer(a, b));

			return sortedNodes;
		}

		public static bool Step(DxTreeList TreeView, List<TreeListNode> destNodes, List<TreeListNode> srcNodes, ref int destIdx, ref int srcIdx, TreeListNodes destNodeList, TreeListNode parent, Func<TreeListNode, TreeListNode, int> comparer, Func<TreeListNode, object[]> getColumns)
		{
			bool more = true;

			// Any destination nodes left to compare ?
			if (destIdx < destNodes.Count)
			{
				if (srcIdx < srcNodes.Count)
				{
					TreeListNode dn = destNodes[destIdx];
					TreeListNode sn = srcNodes[srcIdx];

					switch (comparer(dn, sn))
					{
						case -1:
							// The dest no longer is present in the source, so we need to delete it.
							destNodeList.Remove(dn);
							++destIdx;
							break;

						case 0:
							// They are equal, check child nodes.  This is the only place we need to check
							// child nodes, because when not equal, either the destination node will be deleted
							// or the source node and its children will be inserted.
							MergeTrees(TreeView, dn.Nodes, sn.Nodes, dn, comparer, getColumns);
							++destIdx;
							++srcIdx;
							break;

						case 1:
							// The source is not present in the destination, so we need to insert it.
							TreeListNode newNode = TreeView.AppendNode(getColumns(sn), parent, sn.Tag);
							CopyChildren(TreeView, newNode, sn, getColumns);
							++srcIdx;
							break;
					}
				}
				else
				{
					// Delete rest of destination nodes, as they aren't in the source.
					destNodeList.Remove(destNodes[destIdx]);
					++destIdx;
				}
			}
			else
			{
				if (srcIdx < srcNodes.Count)
				{
					// Add rest of src nodes
					TreeListNode sn = srcNodes[srcIdx];
					TreeListNode newNode = TreeView.AppendNode(getColumns(sn), parent, sn.Tag);
					CopyChildren(TreeView, newNode, sn, getColumns);
					++srcIdx;
				}
				else
				{
					more = false;
				}
			}

			return more;
		}

		public static void CopyChildren(DxTreeList TreeView, TreeListNode parent, TreeListNode source, Func<TreeListNode, object[]> getColumns)
		{
			source.Nodes.ForEach(t =>
				{
					TreeListNode child = TreeView.AppendNode(getColumns(t), parent, t.Tag);
					CopyChildren(TreeView, child, t, getColumns);
				});
		}
	}
}
