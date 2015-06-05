using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DevExpress.XtraTreeList.Nodes;

using Clifton.Tools.Data;

using Intertexti.DevExpressControls;
using Intertexti.lib;
using Intertexti.Models;

namespace Intertexti.Views
{
	public class SearchView : Form, IMycroParserInstantiatedObject, ISupportInitialize
	{
		protected DxTreeList treeView;
		protected Button btnPrevious;
		protected Button btnNext;
		protected ProgressBar pbNotecards;
		protected Dictionary<string, object> objectCollection;

		public ApplicationModel Model { get; protected set; }

		public Dictionary<string, object> ObjectCollection 
		{ 
			get {return objectCollection;}
			set 
			{
				objectCollection = value;
				treeView = (DxTreeList)ObjectCollection["treeView"];
				btnPrevious = (Button)ObjectCollection["btnPrevious"];
				btnNext = (Button)ObjectCollection["btnNext"];
				pbNotecards = (ProgressBar)ObjectCollection["pbNotecards"];
			}
		}

		public int Index
		{
			get { return treeView.Index; }
			set { treeView.Index = value; }
		}

		public void BeginInit()
		{
		}

		public void EndInit()
		{
			Model.NotecardDeleted += NotecardDeleted;
			HandleDestroyed += WhenHandleDestroyed;
		}

		public void Clear()
		{
			treeView.Clear();
		}

		public void ResetProgressBar(int max)
		{
			pbNotecards.Value = 0;
			pbNotecards.Minimum = 0;
			pbNotecards.Maximum = max;
		}

		public void SetProgressBar(int n)
		{
			pbNotecards.Value = n;
			Application.DoEvents();
		}

		public void EnablePreviousNext()
		{
			btnPrevious.Enabled = true;
			btnNext.Enabled = true;
		}

		public void DisablePreviousNext()
		{
			btnPrevious.Enabled = false;
			btnNext.Enabled = false;
		}

		public void BuildTree(List<Tuple<NotecardRecord, int>> hits)
		{
			foreach (var item in hits)
			{
				treeView.AddNode(null, new object[] {item.Item1.GetTitle(), item.Item2}, item.Item1, false);
			}
		}

		protected void NotecardDeleted(NotecardRecord rec)
		{
			foreach(TreeListNode n in treeView.Nodes)
			{
				if ((NotecardRecord)n.Tag == rec)
				{
					treeView.DeleteNode(n);
					break;
				}
			}
		}

		protected void WhenHandleDestroyed(object sender, EventArgs e)
		{
			Model.NotecardDeleted += NotecardDeleted;
			HandleDestroyed += WhenHandleDestroyed;
		}
	}
}
