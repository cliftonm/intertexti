using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using WeifenLuo.WinFormsUI.Docking;

using Intertexti.Models;
using Intertexti.Views;

using Clifton.Tools.Data;

namespace Intertexti.Controllers
{
	public class MetadataController : ViewController<MetadataView>
	{
		public MetadataController()
		{
		}

		public void Clear()
		{
			UpdateURL(String.Empty);
			UpdateTOC(String.Empty);
			UpdateTags(String.Empty);
			UpdateTitle(String.Empty);
		}

		public void Update()
		{
			ApplicationModel.ActiveNotecardRecord.IfNotNull(rec =>
				{
					UpdateURL(rec.URL);
					UpdateTitle(rec.Title);
					UpdateTOC(rec.TableOfContents);
					UpdateTags(ApplicationModel.GetTags());
				}).Else(() =>
					{
						Clear();
					});
		}

		public void UpdateTitle(string title)
		{
			View.UpdateTitle(title);
		}

		public void UpdateURL(string url)
		{
			View.UpdateURL(url);
		}

		public void UpdateTags(string tags)
		{
			View.UpdateTags(tags);
		}

		public void SetProgressBar(int current, int max)
		{
			View.SetProgressBar(current, max);
		}

		protected void UpdateTOC(string toc)
		{
			View.UpdateTOC(toc);
		}

		// View events.

		protected void NavigateToURL(string url)
		{
			if (!String.IsNullOrEmpty(url))
			{
				if (!ApplicationController.TrialVersionExceededNotecardLimit)
				{
					NotecardController controller = ApplicationController.ActiveDocumentController;

					if (controller == null)
					{
						// Create a controller - none exists!
						ApplicationController.CreateNewNotecard();
						controller = ApplicationController.ActiveDocumentController;
					}

					// The controller might still be null in the trial version.
					if (controller != null)
					{
						controller.ClearNotecardHtml();
						controller.NavigateToURL(url);
					}
				}
			}
		}

		protected void SetTitle(string title)
		{
			ApplicationController.ActiveDocumentController.IfNotNull(f => f.SetTitle(title));
		}

		protected void SetTableOfContents(string toc)
		{
			ApplicationModel.ActiveNotecardRecord.IfNotNull(f => f.TableOfContents = toc);
		}

		protected void SetTags(string tags)
		{
			ApplicationModel.ActiveNotecardRecord.IfNotNull(f => ApplicationModel.SetTags(tags));
		}

		protected void Focused(object sender, EventArgs args)
		{
			ApplicationController.EnableCutCopyPasteDel();
		}
	}
}
