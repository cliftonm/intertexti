using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.DevExpressControls;

using Clifton.Windows.Forms;

using Intertexti.Controllers;

namespace Intertexti.Actions
{
	public class AssociatePopup : DeclarativeAction
	{
		public object Control { get; protected set; }
		public ContextMenuStrip ContextMenu { get; protected set; }

		public override void EndInit()
		{
			if (Control is DxTreeList)
			{
				((DxTreeList)Control).NodeRightClick += ContextMenuPopup;
			}
		}

		protected void ContextMenuPopup(object sender, object tag, Point mousePosition)
		{
			ContextMenu.Show((Control)Control, mousePosition);
		}
	}
}
