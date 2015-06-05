using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intertexti.Controllers
{
	public interface IDocumentController
	{
		void IsActive();
		void ShowContextMenu(int x, int y);
		void UpdateReferences();
	}
}
																		  