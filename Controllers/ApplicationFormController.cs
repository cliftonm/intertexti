using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using WeifenLuo.WinFormsUI.Docking;

using Clifton.ApplicationStateManagement;
using Clifton.Collections.Generic;
using Clifton.Tools.Data;
using Clifton.Tools.Strings.Extensions;
using Clifton.Tools.Xml;

using Intertexti.Actions;
using Intertexti.DevExpressControls;
using Intertexti.Models;
using Intertexti.Views;

namespace Intertexti.Controllers
{
	public class ApplicationFormController : ViewController<ApplicationFormView>
	{
		// If this gets any larger, we might consider some sort of a collection bag.
		public NotecardController ActiveDocumentController { get; protected set; }
		public MetadataController MetadataController { get; protected set; }
		public IndexController IndexController { get; protected set; }
		public ReferencedFromController ReferencedFromController { get; protected set; }
		public ReferencesController ReferencesController { get; protected set; }
		public TableOfContentsController TableOfContentsController { get; protected set; }
		public AllNotecardsController AllNotecardsController { get; protected set; }
		public OrphanNotecardsController OrphanNotecardsController { get; protected set; }
		public IMruMenu MruMenu { get; protected set; }

		/// <summary>
		/// Set to true when the user clicks on the "X" or presses ALT-F4, to prevent
		/// additional message boxes when the notecards are in edit mode and to allow
		/// the Closing handler to deal with that situation.
		/// </summary>
		public bool ApplicationClosing { get; set; }
		/// <summary>
		/// True when a deck is being loaded.  Used to prevent updating notecard "viewed" date and creating a dirty model, which is an annoyance when
		/// nothing else has changed.
		/// </summary>
		public bool LoadingDeck { get; protected set; }
		public string CurrentFilename { get; protected set; }

		public bool TrialVersionExceededNotecardLimit
		{
			get
			{
#if TRIAL_VERSION
				if (ApplicationModel.NumNotecards >= 20)
				{
					MessageBox.Show("The trial version allows only 20 notecards notecards to be created.\r\n\r\nPlease register for the CTP version.", "Trial Version", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					return true;
				}

				return false;
#else
				return false;
#endif
			}
		}

		public List<NotecardController> ActiveNotecardControllers
		{
			get
			{
				List<NotecardController> items = new List<NotecardController>();
				documentControllerMap.Values.ForEach(t=>items.Add((NotecardController)t));

				return items;
			}
		}

		protected DiagnosticDictionary<IDockContent, NotecardController> documentControllerMap;

		public ApplicationFormController()
		{
			documentControllerMap = new DiagnosticDictionary<IDockContent, NotecardController>("DocumentControllerMap");
			RegisterUserStateOperations();
		}

		protected void RegisterUserStateOperations()
		{
			Program.AppState.Register("Form", () =>
				{
					return new List<State>()
						{
							new State("X", View.Location.X),
							new State("Y", View.Location.Y),
							new State("W", View.Size.Width),
							new State("H", View.Size.Height),
							new State("WindowState", View.WindowState.ToString()),
							new State("Last Opened", CurrentFilename),
						};

				},
				state =>
				{
					// Silently handle exceptions for when we add state items that are part of the state file until we 
					// save the state.  This allows us to add new state information without crashing the app on startup.
					Program.Try(() => View.Location = new Point(state.Single(t => t.Key == "X").Value.to_i(), state.Single(t => t.Key == "Y").Value.to_i()));
					Program.Try(() => View.Size = new Size(state.Single(t => t.Key == "W").Value.to_i(), state.Single(t => t.Key == "H").Value.to_i()));
					Program.Try(() => View.WindowState = state.Single(t => t.Key == "WindowState").Value.ToEnum<FormWindowState>());
					Program.Try(() => CurrentFilename = state.Single(t => t.Key == "Last Opened").Value);
				});
		}

		public override void EndInit()
		{
			Program.AppState.RestoreState("Form");
			ApplicationModel.AssociationAdded += AssociationAdded;
			ApplicationModel.AssociationRemoved -= AssociationRemoved;
		}

		public void RegisterDocumentController(IDockContent content, NotecardController controller)
		{
			documentControllerMap[content] = controller;
		}

		public NotecardRecord CreateNewNotecard()
		{
			NewDocument("notecard.xml");
			NotecardRecord notecard = ApplicationModel.NewNotecard();
			notecard.DateCreated = DateTime.Now;
			notecard.IsOpen = true;
			ActiveDocumentController.SetNotecardRecord(notecard);
			MetadataController.IfNotNull(t => t.UpdateTitle("Notecard"));
			ReferencesController.IfNotNull(t => t.Clear());
			ReferencedFromController.IfNotNull(t => t.Clear());

			return notecard;
		}

		public NotecardRecord CreateAndEditNotecard()
		{
			NotecardRecord rec = null;

			if (!TrialVersionExceededNotecardLimit)
			{
				rec = CreateNewNotecard();
				Application.DoEvents();
				ActiveDocumentController.EditNotecardHtml();
			}

			return rec;
		}

		public void OpenLastDeck()
		{
			OpenDeck(CurrentFilename);
		}

		public void OpenDeck(string filename)
		{
			ApplicationModel.LoadModel(filename);
			List<NotecardRecord> openNotecards = ApplicationModel.GetOpenNotecards();
			LoadingDeck = true;
			CloseAllDocuments();
			// Set the last document as the active one.
			ActiveDocumentController.IfNotNull(t => t.IsActive());
			TableOfContentsController.IfNotNull(f => f.Refresh());
			IndexController.IfNotNull(f => f.Refresh());
			AllNotecardsController.IfNotNull(f => f.Refresh());
			OrphanNotecardsController.IfNotNull(f => f.Refresh());
			SetCaption(filename);
			OpenNotecardDocuments(openNotecards);

			// Select the last viewed notecard.
			ApplicationModel.HasLastViewedNotecard.Then(() =>
				{
					ApplicationModel.LastViewedNotecard.IfNotNull(rec => OpenANotecard(rec));
				});

			LoadingDeck = false;
		}

		public void SetMenuCheckedState(string menuName, bool state)
		{
			View.SetMenuCheckState(menuName, state);
		}

		public void SetMenuEnabledState(string menuName, bool state)
		{
			View.SetMenuEnabledState(menuName, state);
		}

		public void PaneClosed(PaneView pane)
		{
			if (pane is MetadataView)
			{
				MetadataController = null;
			}
			else if (pane is IndexView)
			{
				IndexController = null;
			}
			else if (pane is ReferencesView)
			{
				ReferencesController = null;
			}
			else if (pane is ReferencedFromView)
			{
				ReferencedFromController = null;
			}
			else if (pane is TableOfContentsView)
			{
				TableOfContentsController = null;
			}
			else if (pane is AllNotecardsView)
			{
				AllNotecardsController = null;
			}
			else if (pane is OrphanNotecardsView)
			{
				OrphanNotecardsController = null;
			}
			else
			{
				throw new ApplicationException("Unknown pane : " + pane.GetType().FullName);
			}
		}

		/// <summary>
		/// Returns true if OK to proceed with the process.
		/// </summary>
		public bool CheckDirtyModel()
		{
			bool ret = true;

			if (ApplicationModel.IsDirty || IsAnyNotecardInEditMode())
			{
				DialogResult res = MessageBox.Show("Changes have been made to your notecards.  Save the changes before proceeding ?", "Changes Have Been Made", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Hand);

				switch (res)
				{
					case DialogResult.Yes:
						SaveHtmlBeingEdited();
						Save(this, EventArgs.Empty);
						break;

					case DialogResult.No:
						CancelHtmlBeingEdited();
						ApplicationModel.CancelChanges();
						// Do nothing.
						break;

					case DialogResult.Cancel:
						ret = false;
						break;
				}
			}

			return ret;
		}

		public void RefreshTOCAndIndex()
		{
			InternalRefreshTOCAndIndex();
		}

		// This method is here because it is common to several panes: TOC, all notecards, orphan notecards.
		public void NodeCheckedStateChanged(object sender, object tag, bool state)
		{
			NotecardRecord rec = tag as NotecardRecord;

			if (rec != null)
			{
				rec.IsChecked = state;
			}
		}

		public void OpenANotecard(NotecardRecord rec, Action<NotecardController> onDocumentLoaded=null)
		{
			if (!rec.IsOpen)
			{
				NewDocument("notecard.xml");
				((NotecardController)ActiveDocumentController).SetNotecardRecord(rec);
				((NotecardController)ActiveDocumentController).ShowDocument(onDocumentLoaded);
				// Post a messge into the Windows' message queue, because otherwise, as we are 
				// changing the tree view that is sourcing this event, we can get a .NET framework / Windows
				// corruption (there is an unmanaged call made by the TreeView control!)
				Program.PostMessageUpdateIndexTrees();
				rec.IsOpen = true;
			}
			else
			{
				// Don't open a new document, select the one that is already open.
				var kvp = documentControllerMap.Single(t => ((NotecardController)t.Value).NotecardRecord == rec);
				IDockContent content = kvp.Key;
				content.DockHandler.Show();
				onDocumentLoaded.IfNotNull(e => e(kvp.Value));
			}
		}

		/// <summary>
		/// Closes a notecard associated with the given notecard record.
		/// </summary>
		public void CloseANotecard(NotecardRecord rec)
		{
			// Check that the notecard is not already closed - when closing children of a TOC node, it is possible to re-close the
			// same notecard if the notecard is referenced in different branches of the hierarchy.
			ActiveNotecardControllers.FirstOrDefault(t => t.NotecardRecord == rec).IfNotNull(t => t.Close());
		}

		public void DisableCutCopyPaste()
		{
			View.SetMenuEnabledState("mnuCut", false);
			View.SetMenuEnabledState("mnuCopy", false);
			View.SetMenuEnabledState("mnuPaste", false);
			View.SetMenuEnabledState("mnuDelete", false);
			View.SetMenuEnabledState("mnuSelectAll", false);
		}

		public void EnableCopyOnly()
		{
			View.SetMenuEnabledState("mnuCut", false);
			View.SetMenuEnabledState("mnuCopy", true);
			View.SetMenuEnabledState("mnuPaste", false);
			View.SetMenuEnabledState("mnuDelete", false);
			View.SetMenuEnabledState("mnuSelectAll", true);
		}

		public void EnableCutCopyPaste()
		{
			View.SetMenuEnabledState("mnuCut", true);
			View.SetMenuEnabledState("mnuCopy", true);
			View.SetMenuEnabledState("mnuPaste", true);
			View.SetMenuEnabledState("mnuDelete", false);
			View.SetMenuEnabledState("mnuSelectAll", true);
		}

		public void EnableCutCopyPasteDel()
		{
			View.SetMenuEnabledState("mnuCut", true);
			View.SetMenuEnabledState("mnuCopy", true);
			View.SetMenuEnabledState("mnuPaste", true);
			View.SetMenuEnabledState("mnuDelete", true);
			View.SetMenuEnabledState("mnuSelectAll", true);
		}

		public void DeleteSelectedNotecard(NotecardRecord rec)
		{
			ApplicationClosing = true;
			CloseANotecard(rec);
			ApplicationClosing = false;
			ApplicationModel.DeleteNotecard(rec);
		}

		public bool ConfirmDelete(NotecardRecord rec)
		{
			DialogResult res = MessageBox.Show("Are you sure you wish to delete the notecard " + rec.GetTitle() + " ?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

			return res == DialogResult.Yes;
		}

		/// <summary>
		/// Update the references and references by whenever an association is changed.  An active notecard record
		/// should always exists when associations are added or removed.
		/// </summary>
		protected void AssociationAdded(NotecardRecord parent, NotecardRecord child)
		{
			ActiveDocumentController.IfNotNull(t => t.UpdateReferences());
		}

		/// <summary>
		/// Update the references and references by whenever an association is changed.  An active notecard record
		/// should always exists when associations are added or removed.
		/// </summary>
		protected void AssociationRemoved(NotecardRecord parent, NotecardRecord child)
		{
			ActiveDocumentController.IfNotNull(t => t.UpdateReferences());
		}

		protected void About(object sender, EventArgs args)
		{
			Form form = Program.InstantiateFromResource<Form>(Intertexti.Properties.Resources.about, null);
			form.ShowDialog();
		}

		protected void New(object sender, EventArgs args)
		{
			if (CheckDirtyModel())
			{
				ApplicationModel.NewModel();
				CloseAllDocuments();
				MetadataController.IfNotNull(f => f.Clear());
				TableOfContentsController.IfNotNull(f => f.Clear());
				IndexController.IfNotNull(f => f.Clear());
				ReferencesController.IfNotNull(f => f.Clear());
				ReferencedFromController.IfNotNull(f => f.Clear());
				AllNotecardsController.IfNotNull(f => f.Clear());
				OrphanNotecardsController.IfNotNull(f => f.Clear());
				SetCaption(String.Empty);
			}
		}

		protected void Open(object sender, EventArgs args)
		{
			if (CheckDirtyModel())
			{
				OpenFileDialog ofd = new OpenFileDialog();
				ofd.RestoreDirectory = true;
				ofd.CheckFileExists = true;
				ofd.Filter = "Intertexti (.intertexti)|*.intertexti";
				ofd.Title = "Load Intertexti Dataset";
				DialogResult res = ofd.ShowDialog();

				if (res == DialogResult.OK)
				{
					MruMenu.AddFile(ofd.FileName);
					OpenDeck(ofd.FileName);
				}
			}
		}

		protected void Save(object sender, EventArgs args)
		{
			if (!ApplicationModel.HasFilename)
			{
				SaveAs(sender, args);
			}
			else
			{
				ApplicationModel.SaveModel();
			}
		}

		protected void SaveAs(object sender, EventArgs args)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.RestoreDirectory = true;
			sfd.CheckPathExists = true;
			sfd.Filter = "Intertexti (.intertexti)|*.intertexti";
			sfd.Title = "Load Intertexti Dataset";
			DialogResult res = sfd.ShowDialog();

			if (res == DialogResult.OK)
			{
				ApplicationModel.SaveModelAs(sfd.FileName);
				MruMenu.AddFile(sfd.FileName);
				SetCaption(sfd.FileName);
			}
		}

		protected void Exit(object sender, EventArgs args)
		{
			CheckDirtyModel().Then(() => View.Close());
		}

		protected void Closing(object sender, CancelEventArgs args)
		{
			CheckDirtyModel().Then(() =>
				{
					SaveLayout();
					Program.AppState.SaveState("Form");
				}).Else(() => args.Cancel = true);
		}

		/// <summary>
		/// The first time the form is displayed, try loading the last opened deck.
		/// </summary>
		protected void Shown(object sender, EventArgs args)
		{
			DisableCutCopyPaste();

			Program.Try(() =>
			{
				OpenLastDeck();
			});
		}

		/// <summary>
		/// When the application is activated (selected), load the last document and set the focus to the browser or HTML editor.
		/// </summary>
		protected void Activated(object sender, EventArgs args)
		{
			ActiveDocumentController.IfNotNull(f => f.SetDocumentFocus());
		}

		protected void RestoreLayout(object sender, EventArgs args)
		{
			CloseAllDockContent();
			LoadTheLayout("defaultLayout.xml");
			RefreshTOCAndIndex();
		}

		protected void CloseAllNotecards(object sender, EventArgs args)
		{
			CloseAllDocuments();
		}

		protected void ShowCardMetadata(object sender, EventArgs args)
		{
			MetadataController.IfNull(() => 
				{
					NewPane("metadataPane.xml");
					MetadataController.Update();
				});
		}

		protected void ShowTableOfContents(object sender, EventArgs args)
		{
			TableOfContentsController.IfNull(() =>
				{
					NewPane("tableOfContentsPane.xml");
					TableOfContentsController.Refresh();
				});
		}

		protected void ShowIndex(object sender, EventArgs args)
		{
			IndexController.IfNull(() =>
				{
					NewPane("indexPane.xml");
					IndexController.Refresh();
				});
		}

		protected void ShowReferences(object sender, EventArgs args)
		{
			ReferencesController.IfNull(() =>
				{
					NewPane("linksToPane.xml");
					ReferencesController.Update();
				});
		}

		protected void ShowReferencedBy(object sender, EventArgs args)
		{
			ReferencedFromController.IfNull(() =>
				{
					NewPane("referencedByPane.xml");
					ReferencedFromController.Update();
				});
		}

		protected void ShowAllNotecards(object sender, EventArgs args)
		{
			AllNotecardsController.IfNull(() =>
				{
					NewPane("allNotecards.xml");
					AllNotecardsController.Refresh();
				});
		}

		protected void ShowOrphanNotecards(object sender, EventArgs args)
		{
			OrphanNotecardsController.IfNull(() =>
				{
					NewPane("orphanNotecards.xml");
					OrphanNotecardsController.Refresh();
				});
		}

		protected void LoadLayout(object sender, EventArgs args)
		{
			if (File.Exists("layout.xml"))
			{
				LoadTheLayout("layout.xml");
			}
			else
			{
				RestoreLayout(sender, args);
			}
		}

		/// <summary>
		/// Create a new notecard and begin editing.
		/// </summary>
		protected void NewNotecard(object sender, EventArgs args)
		{
			CreateAndEditNotecard();
		}

		protected void InternalRefreshTOCAndIndex()
		{
			IndexController.Refresh();
			TableOfContentsController.IfNotNull(f => f.Refresh());
		}

		protected void Refresh(object sender, EventArgs args)
		{
			RefreshTOCAndIndex();
		}

		protected void OpenNotecard(object sender, object tag)
		{
			NotecardRecord rec = tag as NotecardRecord;
			OpenANotecard(rec);
		}

		protected void CloseNotecard(object sender, EventArgs args)
		{
			ActiveDocumentController.IfNotNull(t => t.Close());
		}

		/// <summary>
		/// Deletes the active notecard record.  Contrast with DeleteSelectedNotecard, which is called from the table of contents rather than
		/// the menu or notecard itself.
		/// </summary>
		protected void DeleteNotecard(object sender, EventArgs args)
		{
			ActiveDocumentController.IfNotNull(doc =>
				{
					ConfirmDelete(doc.NotecardRecord).Then(() => doc.Delete());
				});
		}

		protected void DeleteOrphanNotecard(object sender, EventArgs args)
		{
			// If we're receiving this event, the orphan controller is available.
			NotecardRecord rec = OrphanNotecardsController.View.SelectedRecord;
			ConfirmDelete(rec).Then(() =>
				{
					CloseANotecard(rec);
					ApplicationModel.DeleteNotecard(rec);
				});
		}

		protected void EditNotecard(object sender, EventArgs args)
		{
			ActiveDocumentController.EditNotecardHtml();
		}

		/// <summary>
		/// Updates the notecard with the latest changes in the HTML editor and closes the editor.
		/// </summary>
		protected void SaveNotecardEdits(object sender, EventArgs args)
		{
			ActiveDocumentController.UpdateNotecardHtml();
			ActiveDocumentController.EndEditing();
		}

		/// <summary>
		/// Cancels the edits and closes the HTML editor.
		/// </summary>
		protected void CancelNotecardEdits(object sender, EventArgs args)
		{
			ActiveDocumentController.RevertNotecardHtml();
			ActiveDocumentController.EndEditing();
		}

		/// <summary>
		/// Save all changes to notecards that are currently in edit mode and end editing.
		/// </summary>
		protected void SaveHtmlBeingEdited()
		{
			ActiveNotecardControllers.Where(t => t.IsEditing).ForEach(t => 
				{
					t.UpdateNotecardHtml();
					t.EndEditing();
				});
		}

		/// <summary>
		/// Close all notecards in edit mode without saving changes.
		/// </summary>
		protected void CancelHtmlBeingEdited()
		{
			ActiveNotecardControllers.Where(t => t.IsEditing).ForEach(t =>
			{
				t.EndEditing();
			});
		}

		protected bool IsAnyNotecardInEditMode()
		{
			return ActiveNotecardControllers.Any(t => t.IsEditing);
		}

		protected void OpenNotecardDocuments(List<NotecardRecord> notecards)
		{
			notecards.ForEach(t =>
				{
					NewDocument("notecard.xml");
					((NotecardController)ActiveDocumentController).SetNotecardRecord(t);
					((NotecardController)ActiveDocumentController).ShowDocument();
					((NotecardController)ActiveDocumentController).SetNotecardTabTitle(t.Title);
				});
		}

		protected void ActiveDocumentChanged(object sender, EventArgs args)
		{
			DockPanel dockPanel = (DockPanel)sender;
			IDockContent content = dockPanel.ActiveDocument;
			// ActiveDocument may be null when closing a document.
			ActiveDocumentController = (content == null ? null : documentControllerMap[content]);

			// When opening from a file, the notecard record is not yet initialized.
			if ((ActiveDocumentController != null) && (ActiveDocumentController.NotecardRecord != null))
			{
				ActiveDocumentController.IsActive();
			}
			else
			{
				// The active record becomes null when there is no further document controller.
				ApplicationModel.ActiveNotecardRecord = null;
				View.SetMenuEnabledState("mnuPrint", false);
				MetadataController.IfNotNull(c => c.Update());
				DisableCutCopyPaste();
			}
		}

		protected void ContentRemoved(object sender, DockContentEventArgs e)
		{
			// Make sure we're closing a document, not a pane.
			if (e.Content is GenericDocument)
			{
				NotecardController controller = (NotecardController)documentControllerMap[e.Content];
				controller.Closed();
				documentControllerMap.Remove(e.Content);
			}
		}

		/// <summary>
		/// Sets the caption to the filename.
		/// </summary>
		protected void SetCaption(string filename)
		{
			CurrentFilename = filename;
			View.SetCaption(filename);
		}

		protected void LoadTheLayout(string layoutFilename)
		{
			View.DockPanel.LoadFromXml(layoutFilename, ((string persistString)=>
			{
				string typeName = persistString.LeftOf(',').Trim();
				string contentMetadata = persistString.RightOf(',').Trim();
				IDockContent container = InstantiateContainer(typeName, contentMetadata);
				InstantiateContent(container, contentMetadata);

				return container;
			}));
		}

		protected void SaveLayout()
		{
			// Close documents first, so we don't get dummy documents when we reload the layout.
			CloseAllDocuments();
			View.DockPanel.SaveAsXml("layout.xml");
		}

		protected IDockContent InstantiateContainer(string typeName, string metadata)
		{
			IDockContent container = null;

			if (typeName == typeof(GenericPane).ToString())
			{
				container = new GenericPane(metadata);
			}
			else if (typeName == typeof(GenericDocument).ToString())
			{
				container = new GenericDocument(metadata);
			}

			return container;
		}

		protected void InstantiateContent(object container, string filename)
		{
			Dictionary<string, string> fileResourceMap = new Dictionary<string, string>()
			{
				{"allNotecards.xml", Intertexti.Properties.Resources.allNotecards},
				{"indexPane.xml", Intertexti.Properties.Resources.indexPane},
				{"linksToPane.xml", Intertexti.Properties.Resources.linksToPane},
				{"metadataPane.xml", Intertexti.Properties.Resources.metadataPane},
				{"notecard.xml", Intertexti.Properties.Resources.notecard},
				{"orphanNotecards.xml", Intertexti.Properties.Resources.orphanNotecards},
				{"referencedByPane.xml", Intertexti.Properties.Resources.referencedByPane},
				{"tableOfContentsPane.xml", Intertexti.Properties.Resources.tableOfContentsPane},
			};

			if (!fileResourceMap.ContainsKey(filename))
			{
				System.Diagnostics.Debugger.Break();
			}

			Program.InstantiateFromResource<object>(fileResourceMap[filename], ((MycroParser mp) => 
			{
				mp.AddInstance("Container", container);
				mp.AddInstance("ApplicationFormController", this);
				mp.AddInstance("ApplicationModel", ApplicationModel);
			}));
		}

		protected void NewDocument(string filename)
		{
			GenericDocument doc = new GenericDocument(filename);
			InstantiateContent(doc, filename);
			doc.Show(View.DockPanel);
		}

		protected void NewPane(string filename)
		{
			GenericPane pane = new GenericPane(filename);
			InstantiateContent(pane, filename);
			pane.Show(View.DockPanel);
		}

		protected void CloseAllDockContent()
		{
			View.CloseAll();
		}

		protected void CloseAllDocuments()
		{
			View.CloseDocuments();
        }

		protected void RefreshBrowser(object sender, EventArgs args)
		{
			ActiveDocumentController.RefreshBrowser();
		}

		protected void RefreshBrowser2(object sender, EventArgs args)
		{
			ActiveDocumentController.RefreshBrowser2();
		}

		protected void Cut(object sender, EventArgs args)
		{
			Program.GetFocusedControl().IfNotNull(f =>
				{
					f.Is<TextBox>(t => t.Cut());
				}).Else(() => ActiveDocumentController.IfNotNull(f => f.View.Cut()));
		}

		// Menu copy handler.  If a control has focus and its an edit box, copy its text to the clipboard, 
		// otherwise if there's an active notecard instance, tell the notecard's view to do the copy to the clipboard.		
		protected void Copy(object sender, EventArgs args)
		{
			Program.GetFocusedControl().IfNotNull(ctrl =>
				{
					ctrl.Is<TextBox>(tb => tb.Copy());
				}).Else(() => ActiveDocumentController.IfNotNull(notecard => notecard.View.Copy()));
		}

		protected void Paste(object sender, EventArgs args)
		{
			Program.GetFocusedControl().IfNotNull(f =>
				{
					f.Is<TextBox>(t => t.Paste());
				}).Else(() => ActiveDocumentController.IfNotNull(f => f.View.Paste()));
		}

		protected void Delete(object sender, EventArgs args)
		{
			Program.GetFocusedControl().IfNotNull(f =>
			{
				f.Is<TextBox>(t =>
					{
						if (t.SelectedText == String.Empty)
						{
							// If nothing selected, then the "del" key behavior is to delete the character to the right of the cursor.
							// The simplest way to do this is to set the selection length to 1 character!
							t.SelectionLength = 1;
						}

						t.SelectedText = String.Empty;
					});
			}).Else(() => ActiveDocumentController.IfNotNull(f => f.View.Delete()));
		}

		protected void SelectAll(object sender, EventArgs args)
		{
			Program.GetFocusedControl().IfNotNull(f =>
				{
					f.Is<TextBox>(t => t.SelectAll());
				}).Else(() => ActiveDocumentController.IfNotNull(f => f.View.SelectAll()));
		}

		protected void Find(object sender, EventArgs args)
		{
			Form finder = Program.InstantiateFromResource<Form>(Intertexti.Properties.Resources.find,
				((MycroParser mp) =>
				{
					mp.AddInstance("ApplicationFormController", this);
					mp.AddInstance("ApplicationModel", ApplicationModel);
				}));

			// Show as a modeless dialog so user can still interact with the rest of the app during searches.
			finder.Show();
		}

		protected void Print(object sender, EventArgs args)
		{
			ActiveDocumentController.IfNotNull(d => d.Print());
		}
	}
}
