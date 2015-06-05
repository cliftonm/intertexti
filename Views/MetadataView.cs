using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

using Intertexti.Controls;
using Intertexti.DevExpressControls;

namespace Intertexti.Views
{
	public class MetadataView : PaneView
	{
		public DxLabeledTextEdit tbURL { get; protected set; }
		public DxLabeledTextEdit tbTOC { get; protected set; }
		public DxLabeledTextEdit tbTags { get; protected set; }
		public DxLabeledTextEdit tbTitle { get; protected set; }
		public ProgressBar pbProgress { get; protected set; }

		public override string MenuName { get { return "mnuMetadata"; } }

		public void SetFocusOnTOCField()
		{
			tbTOC.Focus();
		}

		public void UpdateURL(string url)
		{
			tbURL.TextBoxText = url;

			Type t = this.GetType();
			System.Reflection.PropertyInfo pi = t.GetProperty("ContextMenuStrip");
		}

		public void UpdateTOC(string toc)
		{
			tbTOC.TextBoxText = toc;
		}

		public void UpdateTags(string tags)
		{
			tbTags.TextBoxText = tags;
		}

		public void UpdateTitle(string title)
		{
			tbTitle.TextBoxText = title;
		}

		public void SetProgressBar(int current, int max)
		{
			if ((current == 0) && (max == 0))
			{
				pbProgress.Visible = false;
			}
			else
			{
				pbProgress.Visible = true;
				pbProgress.Maximum = max;
				pbProgress.Value = current;
			}
		}
	}
}
