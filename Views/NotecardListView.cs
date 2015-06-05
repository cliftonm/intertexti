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
	/// <summary>
	/// Used by "all notecards" and "orphan notecards", might also be used in the future for other flat lists, such as "checked notecards", etc.
	/// Basically, this base class handles all the UI updates when nodes get added, removed, titles change, dates change, etc.
	/// </summary>
	public abstract class NotecardListView : PaneView
	{
		public ApplicationModel Model { get; protected set; }
		public DxTreeList TreeView { get; protected set; }

		public NotecardListView()
		{
		}

		public override void EndInit()
		{
			Model.NotecardAdded += AddNode;
			Model.NotecardDeleted += RemoveNode;
			Model.AssociationAdded += AssociationAdded;
			Model.AssociationRemoved += AssociationRemoved;
			Model.TagsChanged += TagsChanged;
			Model.ActiveNotecardChanged += ActiveNotecardChanged;

			base.EndInit();
		}

		protected void ActiveNotecardChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(t => t.Tag == rec).IfNotNull(t => TreeView.SelectedNode = t);
		}

		protected override void WhenHandleDestroyed(object sender, EventArgs e)
		{
			Model.NotecardAdded -= AddNode;
			Model.NotecardDeleted -= RemoveNode;
			Model.AssociationAdded -= AssociationAdded;
			Model.AssociationRemoved -= AssociationRemoved;
			Model.TagsChanged -= TagsChanged;
			Model.ActiveNotecardChanged -= ActiveNotecardChanged;

			// Unwire all the events that we are watching in this view.
			Model.ForEachNotecard(r =>
			{
				UnwireEvents(r);
			});

			base.WhenHandleDestroyed(sender, e);
		}

		public void Clear()
		{
			TreeView.Clear();
		}

		/// <summary>
		/// Override this method in the derived class for the specific implementation required to populate the list.
		/// </summary>
		public virtual void RefreshView()
		{
			// Wire all the events that we are watching in this view.
			Model.ForEachNotecard(r =>
			{
				WireEvents(r);
			});
		}

		/// <summary>
		/// When a node is added by the user, we add it here and start tracking title, TOC, and date field values.
		/// </summary>
		protected virtual void AddNode(NotecardRecord r)
		{
			TreeView.AddNode(null, new object[] { r.GetTitle(), r.DateCreated, r.DateModified, r.DateLastViewed }, r, r.IsChecked);
			WireEvents(r);
		}

		/// <summary>
		/// When a node is removed by the user, we remove the node and the event wireups.
		/// </summary>
		/// <param name="r"></param>
		protected virtual void RemoveNode(NotecardRecord r)
		{
			// For this view, we have a 1:1 map of nodes to notecard records, so we don't need a callback for every node in the tree that was removed.
			TreeView.RemoveNodesWithTag(r);
			UnwireEvents(r);
		}

		protected virtual void TitleChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(n => n.Tag == rec).IfNotNull(t => t[0] = rec.GetTitle());
		}

		protected virtual void TableOfContentsChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(n => n.Tag == rec).IfNotNull(t=>t[0] = rec.GetTitle());
		}

		protected virtual void DateCreatedChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(n => n.Tag == rec).IfNotNull(t => t[1] = rec.DateCreated);
		}

		protected virtual void DateModifiedChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(n => n.Tag == rec).IfNotNull(t => t[2] = rec.DateModified);
		}

		protected virtual void DateViewedChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().FirstOrDefault(n => n.Tag == rec).IfNotNull(t=>t[3] = rec.DateLastViewed);
		}

		protected virtual void AssociationAdded(NotecardRecord parent, NotecardRecord child)
		{
		}

		protected virtual void AssociationRemoved(NotecardRecord parent, NotecardRecord child)
		{
		}

		protected virtual void TagsChanged(NotecardRecord rec)
		{
		}

		protected virtual void CheckStateChanged(NotecardRecord rec)
		{
			TreeView.Hierarchy().Where(t => t.Tag == rec).ForEach(n => n.Checked = rec.IsChecked);
		}

		protected void WireEvents(NotecardRecord r)
		{
			r.TitleChanged += TitleChanged;
			r.TableOfContentsChanged += TableOfContentsChanged;
			r.DateCreatedChanged += DateCreatedChanged;
			r.DateModifiedChanged += DateModifiedChanged;
			r.DateViewedChanged += DateViewedChanged;
			r.CheckStateChanged += CheckStateChanged;
		}

		protected void UnwireEvents(NotecardRecord r)
		{
			r.TitleChanged -= TitleChanged;
			r.TableOfContentsChanged -= TableOfContentsChanged;
			r.DateCreatedChanged -= DateCreatedChanged;
			r.DateModifiedChanged -= DateModifiedChanged;
			r.DateViewedChanged -= DateViewedChanged;
			r.CheckStateChanged -= CheckStateChanged;
		}
	}
}

