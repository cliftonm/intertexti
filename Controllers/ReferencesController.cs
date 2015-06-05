using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.Models;
using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class ReferencesController : ReferenceController<ReferencesView>
	{
		public void Update()
		{
			View.UpdateView();
		}

		public void DeleteReference(object sender, EventArgs args)
		{
			NotecardRecord childRef = View.TreeView.SelectedNodeTag as NotecardRecord;

			if (childRef != null)
			{
				NotecardRecord parentRef = ApplicationController.ApplicationModel.ActiveNotecardRecord;
				ApplicationController.ApplicationModel.DeleteReference(parentRef, childRef);
				ApplicationController.ActiveDocumentController.UpdateReferences();
			}
		}
	}
}
