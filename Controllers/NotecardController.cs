using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

// using WebKit;

using Intertexti.Models;
using Intertexti.Views;

using Clifton.Tools.Data;
using Clifton.Tools.Strings.Extensions;

namespace Intertexti.Controllers
{
	public class NotecardController : ViewController<NotecardView>, INotecardController
	{
		public NotecardRecord NotecardRecord { get; protected set; }
		public bool IsEditing { get { return editing; } }
		public Action<NotecardController> OnDocumentLoadedAction { get; set; }

		protected bool editing;

		public NotecardController()
		{
		}

		public override void EndInit()
		{
			ApplicationController.MetadataController.IfNotNull(f => f.Clear());
			View.DocumentLoaded += OnDocumentLoaded;
			View.ViewClosed += OnViewClosed;
		}

		protected void OnViewClosed(object sender, EventArgs e)
		{
			View.ViewClosed -= OnViewClosed;
			View.DocumentLoaded -= OnDocumentLoaded;
		}

		protected void OnDocumentLoaded(object sender, EventArgs args)
		{
			OnDocumentLoadedAction.IfNotNull(e => e(this));
		}

		public void SetDocumentText(string text)
		{
			text = FixRelativeFilePaths(text);
			View.Browser.DocumentText = text;
		}

		public void SetNotecardTabTitle(string title)
		{
			((GenericDocument)View.Parent).Text = NotecardRecord.Title;
		}

		public void ShowDocument(Action<NotecardController> onDocumentLoaded=null)
		{
			OnDocumentLoadedAction = onDocumentLoaded;

			if (!(String.IsNullOrEmpty(NotecardRecord.HTML)))
			{
				View.Browser.Document.OpenNew(true);
				View.Browser.DocumentText = FixRelativeFilePaths(NotecardRecord.HTML);
			}
			else
			{
				NavigateToURL(NotecardRecord.URL);
			}
		}

		/// <summary>
		/// Clear the notecard's HTML, allowing a URL to take precedence.
		/// </summary>
		public void ClearNotecardHtml()
		{
			NotecardRecord.HTML = String.Empty;
		}

		public void NavigateToURL(string url)
		{
			if (url != null)
			{
				if ((View.Browser.Url == null) || (View.Browser.Url.ToString() != url))
				{
					if (IsEditing)
					{
						CancelHtmlEditing();
					}

					// This is strange behavior - the browser will change "file://dinner.htm" to "file://dinner.htm/" (note the ending '/')
					// which prevents the navigation, so we have to remove the trailing '/' in that case.  Weird.
					if (url.StartsWith("file:") && url.EndsWith("/"))
					{
						url = url.Remove(url.Length - 1);
					}

					View.Browser.Navigate(url);
					NotecardRecord.IfNotNull((t) => t.URL = url);
				}
			}
		}

		public void IsActive()
		{
			ApplicationModel.ActiveNotecardRecord = NotecardRecord;
			ApplicationController.MetadataController.IfNotNull(f => f.Update());
			((GenericDocument)View.Parent).Text = NotecardRecord.Title;
			UpdateReferences();
			ApplicationController.LoadingDeck.Else(() => 
				{
					NotecardRecord.DateLastViewed = DateTime.Now;
					ApplicationModel.UpdateLastViewed(NotecardRecord);
				});

			SetEditMenuState();
			ApplicationController.SetMenuEnabledState("mnuPrint", true);
		}

		public void UpdateReferences()
		{
			// Update references.
			ApplicationController.ReferencesController.IfNotNull(f => f.Update());
			// Update referenced from.
			ApplicationController.ReferencedFromController.IfNotNull(f => f.Update());
		}

		public void Closed()
		{
			NotecardRecord.Deleted.Else(() => NotecardRecord.IsOpen = false);
		}

		public void SetNotecardRecord(NotecardRecord record)
		{
			NotecardRecord = record;
			ApplicationModel.ActiveNotecardRecord = record;
		}

		public void LinkReferences(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			NotecardController refController = (NotecardController)item.Tag;
			// Create an association between this controller, as the parent, and the refController, as the child.
			ApplicationModel.Associate(NotecardRecord, refController.NotecardRecord);
		}

		public void LinkReferencedFrom(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			NotecardController refController = (NotecardController)item.Tag;
			// Create an association between this controller, as the child, and the refController, as the parent.
			ApplicationModel.Associate(refController.NotecardRecord, NotecardRecord);
		}

		public void TagSelection(object sender, EventArgs e)
		{
			View.TagThisMenu.Tag.IfNotNull(tag =>
				{
					string tagText = tag.ToString();
					tag.Is<HtmlElement>(el => tagText = el.InnerText);
					tagText.Trim();

					if (!String.IsNullOrEmpty(tagText))
					{
						string newTags = ApplicationModel.AddTagIfUnique(NotecardRecord, tagText);
						ApplicationController.MetadataController.UpdateTags(newTags);
					}
				});
		}

		public void OpenInNewNotecard(object sender, EventArgs e)
		{
			if (!ApplicationController.TrialVersionExceededNotecardLimit)
			{
				HtmlElement el = (HtmlElement)View.TagThisMenu.Tag;
				string url = el.GetAttribute("href");
				ApplicationController.CreateNewNotecard();
				ApplicationController.MetadataController.UpdateURL(url);
				NotecardController controller = ApplicationController.ActiveDocumentController;
				controller.ClearNotecardHtml();
				controller.NavigateToURL(url);
			}
		}

		public void SetTitle(string title)
		{
			((GenericDocument)View.Parent).Text = title;
			NotecardRecord.Title = title;
		}

		public void EditNotecardHtml()
		{
			editing = true;
			View.BeginHtmlEditing();
			SetEditMenuState();
		}

		/// <summary>
		/// Updates the notecard's HTML but keeps the editor open.
		/// </summary>
		public void UpdateNotecardHtml()
		{
			View.UpdateBrowserWithEditorHtml();
			NotecardRecord.HTML = View.HtmlEditor.InnerHtml;
			NotecardRecord.DateModified = DateTime.Now;
		}

		public void RevertNotecardHtml()
		{
			View.RevertEditorHtml();
		}

		public void EndEditing()
		{
			editing = false;
			View.EndHtmlEditing();
			View.SetFocus();
			SetEditMenuState();
		}

		/// <summary>
		/// Set the focus to either the browser or the document being edited.
		/// </summary>
		public void SetDocumentFocus()
		{
			View.SetFocus();
		}

		public void Close()
		{
			View.Close();
		}

		public void Delete()
		{
			editing.Then(()=>CancelHtmlEditing());
			ApplicationModel.DeleteNotecard();
			View.Close();
		}

		public void Print()
		{
			editing.Then(() => View.HtmlEditor.DocumentPrint()).Else(() => View.Browser.ShowPrintDialog());
		}

		public string GetHtml()
		{
			string ret = String.Empty;
			editing.Then(() => ret = View.HtmlEditor.InnerHtml).Else(() => ret = View.Browser.DocumentText);

			return ret;
		}

		/// <summary>
		/// Replace file paths with an absolute path to ".\images" relative to the Intertexti application path.  All images used
		/// in custom content must be placed in this folder.  
		/// TODO: At some point, allow the user to specify the image folder specific to an intertexti deck.
		/// </summary>
		protected string FixRelativeFilePaths(string text)
		{
			int start = 0;
			int startOfPath = -1;
			string appPath = Path.GetDirectoryName(Application.ExecutablePath);
			// Convert spaces to %20 and backs lashes with forward slashes.
			// Also note that we are forcing images into a sub-folder called "images".
			string url = "\"file:///" + appPath.Replace(" ", "%20").Replace('\\', '/')+"/images/";

			do
			{
				// The leading quote distinguishes between alt=file:/// and src="file:///
				startOfPath = text.IndexOf("\"file:///", start);

				if (startOfPath >= 0)
				{
					int endOfPath = text.IndexOf("\"", startOfPath + 1);

					if (endOfPath > startOfPath)
					{
						string fullPath = text.Substring(startOfPath, endOfPath - startOfPath);
						// Get rid of everything except what comes after the last '/', which is the image filename
						string imageFilename = fullPath.RightOfRightmostOf('/');
						string textLeft = text.Substring(0, startOfPath);
						string textRight = text.Substring(endOfPath);
						// This is our new absolute path, relative to where Intertexti is running.
						text = textLeft + url + imageFilename + textRight;
					}

					// Check for more file:/// entries
					start = startOfPath + 1;
				}
			} while (startOfPath >= 0);

			return text;
		}

		protected void Activated(object sender, EventArgs args)
		{
			SetDocumentFocus();
		}

		protected void SaveOffline(object sender, EventArgs args)
		{
			HtmlDocument doc = View.Browser.Document;
			HtmlAgilityPack.HtmlDocument adoc = new HtmlAgilityPack.HtmlDocument();
			adoc.LoadHtml(doc.Body.OuterHtml);
			int n=0;
			
			foreach (HtmlAgilityPack.HtmlNode img in adoc.DocumentNode.Descendants("img"))
			{
				img.Attributes["src"].IfNotNull(a =>
					{
						string src = a.Value;
						string location = DownloadImage(src, "c:\\temp\\testfolder\\" + n.ToString() + "." + src.RightOfRightmostOf('.'));
						++n;

						if (location != String.Empty)
						{
							a.Value = "file:///" + location;
						}
					});
			}

			//adoc.LoadHtml(View.Browser.DocumentText);
			adoc.Save("c:\\temp\\test2.html");
		}

		protected string DownloadImage(string imageUrl, string saveLocation)
		{
			// Remove illegal characters.  There most be others?
			saveLocation = saveLocation.Replace('?', '-');

            byte[] imageBytes;

			if (imageUrl.BeginsWith("//"))
			{
				imageUrl = "http:" + imageUrl;
			}

			WebRequest imageRequest;
			WebResponse imageResponse;

			try
			{
				imageRequest = WebRequest.Create(imageUrl);
				imageResponse = imageRequest.GetResponse();
			}
			catch (Exception ex)
			{
				MessageBox.Show(imageUrl, "Problem with...");
				return String.Empty;
			}

            Stream responseStream = imageResponse.GetResponseStream();

            using (BinaryReader br = new BinaryReader(responseStream ))
            {
                imageBytes = br.ReadBytes(500000);
                br.Close();
            }
            responseStream.Close();
            imageResponse.Close();

			try
			{
				FileStream fs = new FileStream(saveLocation, FileMode.Create);
				BinaryWriter bw = new BinaryWriter(fs);
				bw.Write(imageBytes);
				fs.Close();
				bw.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(saveLocation, "Problem with...");
				return String.Empty;
			}

			saveLocation = saveLocation.Replace('\\', '/');

			return saveLocation;
        }

		protected void SetEditMenuState()
		{
			editing.Then(() =>
				{
					ApplicationController.SetMenuEnabledState("mnuEditNotecard", false);
					ApplicationController.SetMenuEnabledState("mnuSaveNotecardChanges", true);
					ApplicationController.SetMenuEnabledState("mnuCancelNotecardChanges", true);
					ApplicationController.EnableCutCopyPasteDel();
				}).Else(() =>
					{
						ApplicationController.SetMenuEnabledState("mnuEditNotecard", true);
						ApplicationController.SetMenuEnabledState("mnuSaveNotecardChanges", false);
						ApplicationController.SetMenuEnabledState("mnuCancelNotecardChanges", false);
						ApplicationController.EnableCopyOnly();
					});
		}

		protected void Closing(object sender, FormClosingEventArgs args)
		{
			// If the user clicks on the close button ("X"), we get closing events, which we want to block
			// and let the form's Closing method handle saving changes.
			(IsEditing && !ApplicationController.ApplicationClosing).Then(() =>
			{
				DialogResult ret = MessageBox.Show("Save changes before closing the notecard ?", "Confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Hand);
				(ret == DialogResult.Yes).Then(() =>
					{
						UpdateNotecardHtml();
						EndEditing();
					});
				(ret == DialogResult.Cancel).Then(() => args.Cancel = true).Else(() => args.Cancel = false);
			});

		}

		protected void DocumentTitleChanged(object sender, EventArgs args)
		{
			string title = View.Browser.DocumentTitle;

			if (!String.IsNullOrEmpty(title))
			{
				SetTitle(title);
				ApplicationController.MetadataController.IfNotNull(f => f.UpdateTitle(title));
			}
		}

		protected void EditHtml(object sender, EventArgs args)
		{
			EditNotecardHtml();
		}

		/// <summary>
		/// This is actually an event sink.
		/// </summary>
		protected void SaveHtmlEditing()
		{
			UpdateNotecardHtml();
			EndEditing();
		}

		// This is actually an event sink.
		protected void CancelHtmlEditing()
		{
			RevertNotecardHtml();
			EndEditing();
		}

		protected void BrowserNavigating(object sender, WebBrowserNavigatingEventArgs e)
		{
			Debug.WriteLine("Browser Navigating: " + e.Url);
			UpdateUrl();
		}

		protected void BrowserNavigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			Debug.WriteLine("Browser Navigated: " + e.Url);
			UpdateUrl();
		}

		public void RefreshBrowser()
		{
			View.Browser.Refresh();
		}

		public void RefreshBrowser2()
		{
			// For certain websites, View.Browser.Document.Body is null!!!!
			// For example, navigate to github.com, which redirects to https://github.com
			// The document never fires the completed event and never shows the page, even though View.Browser.DocumentText has the correct HTML!

			string text = View.Browser.DocumentText;
			View.Browser.Document.OpenNew(true);
			View.Browser.Document.Write(text);
			// View.Browser.Refresh();
			View.CompleteDocumentHandler();
		}

		protected void UpdateUrl()
		{
			// e.Url will throw an exception if there is no URL.
			string url = View.Browser.Url == null ? String.Empty : View.Browser.Url.ToString();

			if (!String.IsNullOrEmpty(url) && (url != "about:blank"))
			{
				NotecardRecord.URL = url;
				ApplicationController.MetadataController.IfNotNull(f => f.UpdateURL(url));

				if (View.Browser.Document != null)
				{
					// This kludge handles entering a URL such as "github.com" that then redirects to "https://github.com" and for some reason, the
					// document completed never fires and the document body is null, even though the browser's DocumentText has the correct HTML!
					// *** UNFORTUNATELY, THIS KLUDGE DOES NOT WORK ***
					if (View.Browser.Document.Body == null)
					{
						// For some reason, this is the only thing that works, sleeping for 100ms, perhaps so that other threads can complete?
						System.Threading.Thread.Sleep(100);
						// RefreshBrowser2();
					}
				}
			}
		}

		protected void EditorFocused(object sender, EventArgs args)
		{
			ApplicationController.EnableCutCopyPasteDel();
		}

		protected void BrowserFocused(object sender, EventArgs args)
		{
			ApplicationController.EnableCopyOnly();
		}

		protected void SaveLocally(object sender, EventArgs args)
		{
			// Example: File.WriteAllText(path, browser.Document.Body.Parent.OuterHtml, Encoding.GetEncoding(browser.Document.Encoding));
		}
	}
}
