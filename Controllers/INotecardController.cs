using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intertexti.Controllers
{
	public interface INotecardController
	{
		void IsActive();
		// void ShowContextMenu(int x, int y);
		void UpdateReferences();
		void SetDocumentText(string text);
		void NavigateToURL(string url);
		void ShowDocument(Action<NotecardController> onDocumentLoaded = null);
		void SetTitle(string title);
		void EditNotecardHtml();
		void UpdateNotecardHtml();
	}
}
