using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.Controls;
using Intertexti.DevExpressControls;
using Intertexti.Models;

namespace Intertexti.Views
{
	public abstract class ReferenceView : PaneView, IReferenceView
	{
		public ApplicationModel Model { get; protected set; }
		public DxTreeList TreeView { get; protected set; }

		public void Clear()
		{
			TreeView.Clear();
		}

		public void UpdateTree(List<NotecardRecord> refs)
		{
			TreeView.Clear();

			refs.ForEach(r =>
			{
				TreeView.AddNode(null, r.GetTitle(), r, false);
			});
		}
	}
}
