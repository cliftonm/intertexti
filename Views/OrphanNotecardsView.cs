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
	public class OrphanNotecardsView : NotecardListView
	{
		public override string MenuName { get { return "mnuOrphanNotecards"; } }

		public NotecardRecord SelectedRecord { get { return ((TreeListNode)TreeView.SelectedNode).Tag as NotecardRecord; } }

		public override void RefreshView()
		{
			TreeView.Clear();

			Model.ForEachNotecard(r =>
			{
				AddIfQualified(r);
			});

			base.RefreshView();
		}

		protected void AddIfQualified(NotecardRecord r)
		{
			// Notecards with a TOC entry or tags are not orphans, they will appear at the root level of the TOC.
			if ((String.IsNullOrEmpty(r.TableOfContents)) && (Model.GetTags(r).Length == 0))
			{
				// Only add the notecard if it is not referenced and has no references.
				if ((Model.GetReferences(r).Count == 0) && (Model.GetReferencedFrom(r).Count == 0))
				{
					TreeView.AddNode(null, new object[] { r.GetTitle(), r.DateCreated, r.DateModified, r.DateLastViewed }, r, r.IsChecked);
				}
			}
		}

		/// <summary>
		/// If a TOC entry is created, the record is no longer an orphan.
		/// If a TOC entry is blanked, the record may be an orphan if it has no references and no index.
		/// </summary>
		protected override void TableOfContentsChanged(NotecardRecord rec)
		{
			// If it has a TOC, then it's not orphaned.
			if (!String.IsNullOrEmpty(rec.TableOfContents))
			{
				// If it was orphaned, we can now remove it.  The orphan list is a 1:1 list of nodes and notecard records.
				TreeView.Hierarchy().SingleOrDefault(n => n.Tag == rec).IfNotNull(n => TreeView.Nodes.Remove(n));
			}
			// If it doesn't have a TOC, and doesn't have an index...
			else
			{
				TreeView.Hierarchy().SingleOrDefault(n => n.Tag == rec).IfNull(() => AddIfQualified(rec));
			}
		}

		/// <summary>
		/// Remove a notecard as an orphaned notecard if it has tags.
		/// </summary>
		protected override void TagsChanged(NotecardRecord rec)
		{
			if (Model.GetTags(rec).Length != 0)
			{
				TreeView.Hierarchy().SingleOrDefault(n => n.Tag == rec).IfNotNull(n => TreeView.Nodes.Remove(n));
			}
			else
			{
				TreeView.Hierarchy().SingleOrDefault(n => n.Tag == rec).IfNull(() => AddIfQualified(rec));
			}
		}

		protected override void AssociationAdded(NotecardRecord parent, NotecardRecord child)
		{
			// Remove orphans that are associated.
			// TODO: This does not test if we have associated notecards that, as a group, are orphaned!
			TreeView.Hierarchy().SingleOrDefault(n => n.Tag == parent).IfNotNull(n => TreeView.Nodes.Remove(n));
			TreeView.Hierarchy().SingleOrDefault(n => n.Tag == child).IfNotNull(n => TreeView.Nodes.Remove(n));
		}

		protected override void AssociationRemoved(NotecardRecord parent, NotecardRecord child)
		{
			AddIfQualified(parent);
			AddIfQualified(child);
		}
	}
}

