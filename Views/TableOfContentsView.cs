using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DevExpress.XtraTreeList.Nodes;

using Intertexti.Controls;
using Intertexti.DevExpressControls;
using Intertexti.Models;

using Clifton.Tools.Data;

namespace Intertexti.Views
{
	public class TableOfContentsView : PaneView
	{
		public delegate void NotificationDlgt();
		
		public event NotificationDlgt Opening;
		public event NotificationDlgt Closing;

		public ApplicationModel Model { get; protected set; }
		public DxTreeList TreeView { get; protected set; }
		public override string MenuName { get { return "mnuTOC"; } }
		public ToolStripMenuItem MenuNewNotecard { get; protected set; }

		protected List<int> encounteredRecords;

		public bool HasSelectedNotecard
		{
			get	{ return TreeView.LastClickNode != null;}
		}

		public TableOfContentsView()
		{
			encounteredRecords = new List<int>();
		}

		public override void EndInit()
		{
			Model.ActiveNotecardChanged += ActiveNotecardChanged;
			Model.AssociationAdded += AssociationAdded;
			Model.AssociationRemoved += AddAssociationRemoved;
			Model.NotecardAdded += NotecardAdded;
			Model.NotecardDeleted += NotecardDeleted;
			Opening.IfNotNull().Then(() => Opening());
			base.EndInit();
		}

		protected override void WhenHandleDestroyed(object sender, EventArgs e)
		{
			Model.ActiveNotecardChanged -= ActiveNotecardChanged;
			Model.AssociationAdded -= AssociationAdded;
			Model.AssociationRemoved -= AddAssociationRemoved;
			UnwireAllNotecardEvents();
			Closing.IfNotNull().Then(() => Closing());
			Program.AppState.SaveState("TOC");
			base.WhenHandleDestroyed(sender, e);
		}

		public void RefreshView()
		{
			encounteredRecords = new List<int>();
			List<NotecardRecord> rootRecs = Model.GetRootNotecards();
			TreeView.Clear();
			PopulateTree(TreeView, rootRecs);
			WireUpAllNotecardEvents();
		}

		protected void WireUpAllNotecardEvents()
		{
			Model.ForEachNotecard(r =>
			{
				WireUpNotecardEvents(r);
			});
		}

		protected void UnwireAllNotecardEvents()
		{
			Model.ForEachNotecard(r =>
			{
				UnwireNotecardEvents(r);
			});

		}

		protected void WireUpNotecardEvents(NotecardRecord r)
		{
			r.TableOfContentsChanged += TableOfContentsChanged;
			r.CheckStateChanged += CheckStateChanged;
			r.DateModifiedChanged += DateModifiedChanged;
			r.DateViewedChanged += DateViewedChanged;
		}

		protected void UnwireNotecardEvents(NotecardRecord r)
		{
			r.TableOfContentsChanged -= TableOfContentsChanged;
			r.CheckStateChanged -= CheckStateChanged;
			r.DateModifiedChanged -= DateModifiedChanged;
			r.DateViewedChanged -= DateViewedChanged;
		}

		public void Clear()
		{
			TreeView.Clear();
		}

		public void ExpandAll()
		{
			TreeView.LastClickNode.IfNotNull(t => t.ExpandAll());
		}

		public void CollapseChildren()
		{
			TreeView.LastClickNode.IfNotNull(t => t.Hierarchy().ForEach(n=>n.Expanded=false));
		}

		// TODO: Should be in the controller since it changes the model state.
		public void CheckAll()
		{
			TreeView.LastClickNode.IfNotNull(t =>
				{
					((NotecardRecord)t.Tag).IsChecked = true;
					t.Hierarchy().ForEach(n => ((NotecardRecord)n.Tag).IsChecked = true);
				});
		}

		// TODO: Should be in the controller since it changes the model state.
		public void UncheckAll()
		{
			TreeView.LastClickNode.IfNotNull(t =>
			{
				((NotecardRecord)t.Tag).IsChecked = false;
				t.Hierarchy().ForEach(n => ((NotecardRecord)n.Tag).IsChecked = false);
			});
		}

		public List<NotecardRecord> GetNotecardRecordsOfCurrentNode()
		{
			List<NotecardRecord> recs = new List<NotecardRecord>();
			TreeView.LastClickNode.Hierarchy().ForEach(n => recs.Add((NotecardRecord)n.Tag));

			return recs;
		}

		public NotecardRecord GetSelectedNotecard()
		{
			return TreeView.LastClickNode.Tag as NotecardRecord;
		}

		public void UpdateTree()
		{
			DxTreeList tvNew = new DxTreeList();
			// Columns are necessary in order for the TreeListNode[index] to be non-null.
			// TODO: Copy column definitions from source tree.
			tvNew.TreeListColumns.Add(new DxTreeListColumn() { Caption = "Title", Visible = true, VisibleIndex = 0, Width = 100 });
			tvNew.TreeListColumns.Add(new DxTreeListColumn() { Caption = "Date Created", Visible = true, VisibleIndex = 0, Width = 100 });
			tvNew.TreeListColumns.Add(new DxTreeListColumn() { Caption = "Date Modified", Visible = true, VisibleIndex = 0, Width = 100 });
			tvNew.TreeListColumns.Add(new DxTreeListColumn() { Caption = "Date Last Viewed", Visible = true, VisibleIndex = 0, Width = 100 });
			// EndInit initializes the columns for the tree view.
			tvNew.EndInit();
			List<NotecardRecord> rootRecs = Model.GetRootNotecards();
			PopulateTree(tvNew, rootRecs);
			IndexView.MergeTrees(TreeView, tvNew, 
				(a, b) => a[0].ToString().CompareTo(b[0].ToString()),
				(tln) => new object[] {tln[0], tln[1], tln[2], tln[3]}
			);
		}

		protected void AddAssociationRemoved(NotecardRecord recParent, NotecardRecord recChild)
		{
			UpdateTree();
		}

		protected void AssociationAdded(NotecardRecord recParent, NotecardRecord recChild)
		{
			UpdateTree();
		}

		protected void TableOfContentsChanged(NotecardRecord rec)
		{
			UpdateTree();
		}

		protected void CheckStateChanged(NotecardRecord rec)
		{
			// Populate a list before iterating because setting the value changes the iterator.
			TreeView.Hierarchy().Where(t => t.Tag == rec).ToList().ForEach(n => n.Checked = rec.IsChecked);
		}

		protected void DateModifiedChanged(NotecardRecord rec)
		{
			// Populate a list before iterating because setting the value changes the iterator.
			TreeView.Hierarchy().Where(t => t.Tag == rec).ToList().ForEach(n => n.SetValue(2, rec.DateModified));
		}

		protected void DateViewedChanged(NotecardRecord rec)
		{
			// Populate a list before iterating because setting the value changes the iterator.
			TreeView.Hierarchy().Where(t => t.Tag == rec).ToList().ForEach(n => n.SetValue(3, rec.DateLastViewed));
		}

		protected void NotecardAdded(NotecardRecord rec)
		{
			WireUpNotecardEvents(rec);
		}

		protected void NotecardDeleted(NotecardRecord rec)
		{
			UnwireNotecardEvents(rec);
			UpdateTree();
		}

		protected void ActiveNotecardChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(t => t.Tag == rec).IfNotNull(t => TreeView.SelectedNode = t);
		}

		protected void PopulateTree(DxTreeList TreeView, List<NotecardRecord> rootRecs)
		{
			// Top-most nodes are those with a table of contents field value but are not referenced by other notecards 
			// (which would make them child records) unless the reference is self-referential (which we then ignore in
			// the child population process.)
			rootRecs.Where(r => !String.IsNullOrEmpty(r.TableOfContents)).OrderBy(r => r.TableOfContents).ForEach(r =>
				{
					encounteredRecords.Add(r.ID);
					object tn = TreeView.AddNode(null, new object[] {r.TableOfContents, r.DateCreated, r.DateModified, r.DateLastViewed}, r, r.IsChecked);
					PopulateChildren(TreeView, tn, r);
					// Remove the record, so that other parent nodes can also reference the child.
					encounteredRecords.Remove(r.ID);
				});
		}

		protected void PopulateChildren(DxTreeList TreeView, object parent, NotecardRecord rec)
		{
			List<NotecardRecord> childRecs = Model.GetReferences(rec);

			childRecs.Where(r=>(!String.IsNullOrEmpty(r.TableOfContents)) && (!encounteredRecords.Contains(r.ID))).OrderBy(r => r.TableOfContents).ForEach(r =>
				{
					encounteredRecords.Add(r.ID);
					object tn = TreeView.AddNode(parent, new object[] { r.TableOfContents, r.DateCreated, r.DateModified, r.DateLastViewed }, r, r.IsChecked);
					// Recurse into grandchildren, etc.
					PopulateChildren(TreeView, tn, r);
					// Remove the record, so that other parent nodes can also reference the child.
					encounteredRecords.Remove(r.ID);
				});
		}
	}
}
