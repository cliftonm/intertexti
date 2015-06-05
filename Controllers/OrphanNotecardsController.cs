using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class OrphanNotecardsController : ViewController<OrphanNotecardsView>
	{
		public void Clear()
		{
			View.Clear();
		}

		public void Refresh()
		{
			View.RefreshView();
		}
	}
}
