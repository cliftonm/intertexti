using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Intertexti.Models;
using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class ReferencedFromController : ReferenceController<ReferencedFromView>
	{
		public void Update()
		{
			View.UpdateView();
		}

		public void DeleteReference(object sender, EventArgs args)
		{
			NotecardRecord parentRef = View.TreeView.SelectedNodeTag as NotecardRecord;

			if (parentRef != null)
			{
				NotecardRecord childRef = ApplicationController.ApplicationModel.ActiveNotecardRecord;
				ApplicationController.ApplicationModel.DeleteReference(parentRef, childRef);
				ApplicationController.ActiveDocumentController.UpdateReferences();
			}
		}
	}
}
