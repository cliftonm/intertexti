using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.Models;

namespace Intertexti.Views
{
	public class ReferencedFromView : ReferenceView
	{
		public override string MenuName { get { return "mnuReferencedBy"; } }

		public void UpdateView()
		{
			// TODO: Exclude existing referenced from's
			List<NotecardRecord> refs = Model.GetReferencedFrom();
			UpdateTree(refs);
		}
	}
}
