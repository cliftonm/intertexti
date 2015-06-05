using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Clifton.Tools.Data;

using Intertexti.Models;

namespace Intertexti.Views
{
	public class ReferencesView : ReferenceView
	{
		public override string MenuName { get { return "mnuReferences"; } }

		public void UpdateView()
		{
			// TODO: Exclude existing references.
			List<NotecardRecord> refs = Model.GetReferences();
			UpdateTree(refs);
		}
	}
}
