using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using mshtml;

using WeifenLuo.WinFormsUI.Docking;

// using WebKit;

using MSDN.Html.Editor;

using Clifton.Tools.Data;

using Intertexti.Controllers;
using Intertexti.Models;

namespace Intertexti.Views
{
	public class NotecardView : UserControl
	{
		public event EventHandler DocumentLoaded;
		public event EventHandler ViewClosed;

		protected WebBrowser browser;
		protected HtmlDocument document = null;
		protected IHTMLTxtRange textRange = null;

		public WebBrowser Browser
		{
			get { return browser; }
			protected set
			{
				browser = value;
				browser.ScriptErrorsSuppressed = true;
				browser.DocumentCompleted += OnDocumentCompleted;
				browser.ProgressChanged += OnProgressChanged;
				browser.StatusTextChanged += OnStatusTextChanged;
			}
		}

		public HtmlElement CurrentLink { get; protected set; }

		public string SelectedText
		{
			get
			{
				string ret = String.Empty;
				IHTMLDocument2 htmlDocument = browser.Document.DomDocument as IHTMLDocument2;
				IHTMLSelectionObject currentSelection = htmlDocument.selection;

				if (currentSelection != null)
				{
					IHTMLTxtRange range = currentSelection.createRange() as IHTMLTxtRange;

					if (range != null)
					{
						ret = range.text ?? String.Empty;
					}
				}

				return ret;
			}
		}

		public HtmlEditorControl HtmlEditor { get; protected set; }
		public ToolStripMenuItem ReferencesMenu { get; protected set; }
		public ToolStripMenuItem ReferencedByMenu { get; protected set; }
		public ToolStripMenuItem TagThisMenu {get; protected set;}
		public ToolStripMenuItem OpenInNewNotecardMenu { get; protected set; }

		// Required because this view is constructing menus dynamically.
		public ApplicationFormController ApplicationController { get; protected set; }
		public NotecardController Controller { get; protected set; }

		public NotecardView()
		{
			HandleDestroyed += WhenHandleDestroyed;
			CurrentLink = null;
		}

		public void Close()
		{
			((GenericDocument)Parent).Close();
		}

		/// <summary>
		/// Overrides the context menu, so we can display our menu instead.
		/// </summary>
		protected void OnContextMenuShowing(object sender, HtmlElementEventArgs e)
		{
			CreateDynamicReferences();
			UpdateTagMenuItem();
			OpenInNewNotecardMenu.Enabled = CurrentLink != null;
			Browser.ContextMenuStrip.Show(Browser, e.MousePosition);
			e.ReturnValue = false;
		}

		protected void UpdateTagMenuItem()
		{
			// Any current link takes precedence over any selected text.
			// This way, if there's selected text but the user right-clicks on a link, we get the link text
			// instead, which makes more sense from a usability perspective.
			if (CurrentLink != null)
			{
				TagThisMenu.Visible = true;
				TagThisMenu.Text = "Tag This Link";
				TagThisMenu.Tag = CurrentLink;
			}
			else if (SelectedText != String.Empty)
			{
				TagThisMenu.Visible = true;
				TagThisMenu.Text = "Tag This Selection";
				TagThisMenu.Tag = SelectedText;
			}
			else
			{
				TagThisMenu.Visible = false;
			}
		}

		public void Copy()
		{
			if (!Browser.Visible)
			{
				HtmlEditor.TextCopy();
			}
			else
			{
				Browser.Document.ExecCommand("Copy", false, null);
			}
		}

		public void Cut()
		{
			if (!Browser.Visible)
			{
				HtmlEditor.TextCut();
			}
		}

		public void Paste()
		{
			if (!Browser.Visible)
			{
				HtmlEditor.TextPaste();
			}
		}

		public void Delete()
		{
			if (!Browser.Visible)
			{
				HtmlEditor.TextDelete();
			}
		}

		public void SelectAll()
		{
			if (!Browser.Visible)
			{
				HtmlEditor.TextSelectAll();
			}
			else
			{
				Browser.Document.ExecCommand("SelectAll", false, null);
			}
		}

		public void BeginHtmlEditing()
		{
			HtmlEditor.InnerHtml = Browser.DocumentText;
			Browser.Visible = false;

			// Kludge to get HtmlEditor's panes to redraw to the size of the window.
			// TODO: There must be a better way to get the editor to position itself
			// correctly, but then again, looking at how bad the screen flickers when
			// resizing the window, there is definitely something fundamentally wrong here.
			HtmlEditor.Visible = true;
			HtmlEditor.Visible = false;
			HtmlEditor.Visible = true;
			HtmlEditor.Focus();
		}

		public void EndHtmlEditing()
		{
			// This is a crazy property.  You can set the text, but when reading the property,
			// it stays the same until some time "later".
			HtmlEditor.Visible = false;
			Browser.Visible = true;
		}

		public void SetFocus()
		{
			Program.Try(() => Browser.Visible.Then(() => Browser.Document.Focus()).Else(() => HtmlEditor.Focus()));
		}

		public void UpdateBrowserWithEditorHtml()
		{
			Browser.DocumentText = HtmlEditor.InnerHtml;
		}

		public void RevertEditorHtml()
		{
			HtmlEditor.InnerHtml = Browser.DocumentText;
		}

		// http://social.msdn.microsoft.com/Forums/en-US/Vsexpressvcs/thread/b8c9d5d5-8d85-49ee-91e3-0753a6ead023/
		public bool FindFirst(string text)
		{
			bool found = false;

			Program.Try(() =>
			{
				IHTMLDocument2 doc = browser.Document.DomDocument as IHTMLDocument2;
				IHTMLElement pElem = doc.body;
				IHTMLBodyElement pBodyelem = pElem as IHTMLBodyElement;
				textRange = pBodyelem.createTextRange();

				textRange.findText(text, 100000).Then(() =>
				{
					found = true;
					textRange.select();
					textRange.scrollIntoView(true);
				});
			});

			return found;
		}

		public void FindLast(string text)
		{
			Program.Try(() =>
			{
				FindFirst(text).Then(() => { while (FindNext(text)) { } });
				FindPrevious(text);
			});
		}

		public bool FindNext(string text)
		{
			bool found = false;

			Program.Try(() =>
			{
				IHTMLDocument2 doc = browser.Document.DomDocument as IHTMLDocument2;
				IHTMLElement pElem = doc.body;
				IHTMLBodyElement pBodyelem = pElem as IHTMLBodyElement;
				textRange.IfNotNull(r => r.collapse(false)).Else(() => textRange = pBodyelem.createTextRange());

				textRange.findText(text, 100000).Then(() =>
				{
					found = true;
					textRange.select();
					textRange.scrollIntoView(true);
				});

			});

			return found;
		}

		public bool FindPrevious(string text)
		{
			bool found = false;

			Program.Try(() =>
			{
				IHTMLDocument2 doc = browser.Document.DomDocument as IHTMLDocument2;
				IHTMLElement pElem = doc.body;
				IHTMLBodyElement pBodyelem = pElem as IHTMLBodyElement;
				textRange.IfNotNull(r => r.collapse(true)).Else(() => textRange = pBodyelem.createTextRange());

				textRange.findText(text, -100000).Then(() =>
				{
					found = true;
					textRange.select();
					textRange.scrollIntoView(true);
				});
			});

			return found;
		}

		public void CompleteDocumentHandler()
		{
			// TODO: This even may fire multiple times!
			// We need to always do this, even if the document == browser.Document, because otherwise, after saving edits, the context
			// menu reverts to the browser's context menu, not my context menu.
			document = browser.Document;
			browser.Document.ContextMenuShowing += OnContextMenuShowing;
			// Handle security, certification, and other errors silently.
			browser.Document.Window.Error += (o, e) => e.Handled = true;
			// TODO: Check, new text range because of new document ?
			// textRange = null;
			DocumentLoaded.IfNotNull(e => e(this, EventArgs.Empty));
			WireUpLinkEvents();
		}

		protected virtual void WhenHandleDestroyed(object sender, EventArgs args)
		{
			Browser.Dispose();
			HtmlEditor.Dispose();
			ViewClosed.IfNotNull(e => e(this, EventArgs.Empty));
		}

		/// <summary>
		/// Wires up the context menu handler once the Document is instantiated.
		/// </summary>
		protected void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs args)
		{
			if (browser.ReadyState == WebBrowserReadyState.Complete)
			{
				Debug.WriteLine("Browser Completed " + args.Url);
				CompleteDocumentHandler();
			}
		}

		protected void CreateDynamicReferences()
		{
			ReferencesMenu.DropDownItems.Clear();
			ReferencedByMenu.DropDownItems.Clear();
			List<NotecardController> activeNotecardControllers = ApplicationController.ActiveNotecardControllers;

			activeNotecardControllers.Where(t=>t.NotecardRecord != null).OrderBy(t=>t.NotecardRecord.GetTitle()).ForEach(t =>
				{
					// Don't include self in the reference list.
					if (t.NotecardRecord != ApplicationController.ActiveDocumentController.NotecardRecord)
					{
						ToolStripMenuItem item1 = new ToolStripMenuItem(t.NotecardRecord.GetTitle());
						item1.Tag = t;
						item1.Click += Controller.LinkReferences;
						ReferencesMenu.DropDownItems.Add(item1);

						ToolStripMenuItem item2 = new ToolStripMenuItem(t.NotecardRecord.GetTitle());
						item2.Tag = t;
						item2.Click += Controller.LinkReferencedFrom;
						ReferencedByMenu.DropDownItems.Add(item2);
					}
				});
		}

		protected void OnProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
		{
			if ((e.CurrentProgress == -1) || (e.MaximumProgress == e.CurrentProgress))
			{
				ApplicationController.MetadataController.IfNotNull(f => f.SetProgressBar(0, 0));
			}
			else if ((e.MaximumProgress > 0) && (e.CurrentProgress > 0) && (e.CurrentProgress < e.MaximumProgress))
			{
				ApplicationController.MetadataController.IfNotNull(f => f.SetProgressBar((int)e.CurrentProgress, (int)e.MaximumProgress));
			}
		}

		protected void OnStatusTextChanged(object sender, EventArgs args)
		{
			string status = Browser.StatusText;
			ApplicationController.View.BrowserStatus.Text = status;
		}

		protected void WireUpLinkEvents()
		{
			foreach (HtmlElement el in browser.Document.Links)
			{
				el.MouseEnter += (o, ex) => CurrentLink = ex.ToElement;
				el.MouseLeave += (o, ex) => CurrentLink = null;
			}
		}
	}
}
