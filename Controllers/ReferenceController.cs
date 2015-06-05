using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class ReferenceController<T> : ViewController<T> where T : IReferenceView
	{
		public void Clear()
		{
			View.Clear();
		}

		public void SelectNode(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Right)
			{
				View.TreeView.SelectedNode = View.TreeView.GetNodeAt(args.X, args.Y);
			}
		}
	}
}
