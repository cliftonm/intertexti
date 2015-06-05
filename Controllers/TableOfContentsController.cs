using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Intertexti.DevExpressControls;
using Intertexti.Models;
using Intertexti.Views;

using Clifton.ApplicationStateManagement;
using Clifton.Tools.Data;
using Clifton.Tools.Strings.Extensions;

namespace Intertexti.Controllers
{
	public class TableOfContentsController : ViewController<TableOfContentsView>
	{
		public TableOfContentsController()
		{
			RegisterUserStateOperations();
		}

		public void Clear()
		{
			View.Clear();
		}

		public void Refresh()
		{
			View.RefreshView();
		}

		protected void Opening()
		{
			Program.AppState.RestoreState("TOC");
		}

		protected void Closing()
		{
			Program.AppState.SaveState("TOC");
		}

		protected void ExpandAll(object sender, EventArgs args)
		{
			View.ExpandAll();
		}

		protected void CollapseChildren(object sender, EventArgs args)
		{
			View.CollapseChildren();
		}

		protected void CheckAll(object sender, EventArgs args)
		{
			View.CheckAll();
		}

		protected void UncheckAll(object sender, EventArgs args)
		{
			View.UncheckAll();
		}

		protected void OpenAll(object sender, EventArgs args)
		{
			View.GetNotecardRecordsOfCurrentNode().ForEach(r => ApplicationController.OpenANotecard(r));
		}

		protected void CloseAll(object sender, EventArgs args)
		{
			View.GetNotecardRecordsOfCurrentNode().ForEach(r => ApplicationController.CloseANotecard(r));
		}

		protected void DeleteNotecard(object sender, EventArgs args)
		{
			NotecardRecord rec = View.GetSelectedNotecard();
			ApplicationController.ConfirmDelete(rec).Then(() =>
			{
				ApplicationController.DeleteSelectedNotecard(rec);
			});
		}

		protected void ContextMenuOpening(object sender, CancelEventArgs args)
		{
			args.Cancel = !View.HasSelectedNotecard;
		}

		/// <summary>
		/// Create a new notecard that references the current notecard.
		/// </summary>
		protected void NewChildNotecard(object sender, EventArgs args)
		{
			NotecardRecord rec = View.GetSelectedNotecard();
			NotecardRecord newRec = ApplicationController.CreateAndEditNotecard();
			ApplicationModel.Associate(rec, newRec);
			ApplicationController.MetadataController.IfNotNull(c => c.View.SetFocusOnTOCField());
		}

		protected void RegisterUserStateOperations()
		{
			Program.AppState.Register("TOC", () =>
			{
				List<State> colInfo = new List<State>();
				int n = 0;

				foreach (DxTreeListColumn tlc in View.TreeView.GetTreeColumns())
				{
					colInfo.Add(new State("VisibleIndex" + n, tlc.VisibleIndex));
					colInfo.Add(new State("Width" + n, tlc.Width));
					colInfo.Add(new State("Visible" + n, tlc.Visible));
					colInfo.Add(new State("SortIndex" + n, tlc.SortIndex));
					colInfo.Add(new State("SortOrder" + n, tlc.SortOrder.ToString()));
					++n;
				}

				return colInfo;
			},
				state =>
				{
					for (int i = 0; i < 4; i++)
					{
						Program.Try(() =>
						{
							int idx = state.Single(t => t.Key == "VisibleIndex" + i).Value.to_i();
							int width = state.Single(t => t.Key == "Width" + i).Value.to_i();
							bool visible = state.Single(t => t.Key == "Visible" + i).Value.to_b();
							int sortIndex = state.Single(t => t.Key == "SortIndex" + i).Value.to_i();
							SortOrder sortOrder = state.Single(t => t.Key == "SortOrder" + i).Value.ToEnum<SortOrder>();
							View.TreeView.SetColumnInfo(i, idx, width, visible, sortIndex, sortOrder);
						});
					}
				}, true);
		}
	}
}
