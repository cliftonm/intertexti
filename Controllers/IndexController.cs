using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Intertexti.Models;
using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class IndexController : ViewController<IndexView>
	{
		public void Clear()
		{
			View.Clear();
		}

		public void Refresh()
		{
			View.RefreshView();
		}

		public void OpenNotecard(object sender, object tag)
		{
			NotecardRecord rec = (NotecardRecord)tag;
			ApplicationController.OpenANotecard(rec, nc =>
			{
				// When the document has loaded...
				nc.OnDocumentLoadedAction = null;
				nc.View.FindFirst(View.CurrentIndexText);
			});
		}
	}
}
