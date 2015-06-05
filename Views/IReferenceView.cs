using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.Controls;
using Intertexti.DevExpressControls;

namespace Intertexti.Views
{
	public interface IReferenceView
	{
		DxTreeList TreeView { get; }
		void Clear();
	}
}
