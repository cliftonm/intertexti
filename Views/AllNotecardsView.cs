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
	public class AllNotecardsView : NotecardListView
	{
		public override string MenuName { get { return "mnuAllNotecards"; } }

		public AllNotecardsView()
		{
		}

		public override void RefreshView()
		{
			TreeView.Clear();

			Model.ForEachNotecard(r =>
				{
					TreeView.AddNode(null, new object[] { r.TableOfContents, r.DateCreated, r.DateModified, r.DateLastViewed }, r, r.IsChecked);
				});

			base.RefreshView();
		}
	}
}

