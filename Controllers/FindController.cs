using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Clifton.Tools.Data;

using Intertexti.Models;
using Intertexti.Views;

// When the user clicks on a found notecard, we want to select the text and also "find next" and "find previous" buttons, 
// which means highlighting the text in either the webbrowser or the HTML editor, depending on which is open.

namespace Intertexti.Controllers
{
	public class FindController : ViewController<SearchView>
	{
		public string SearchText 
		{ 
			get {return searchText;}
			set
			{
				if (searchText != value)
				{
					// If the search text changes, disable the previous/next buttons and clear the "found" list.
					searchText = value;
					searchNotecardController.IfNotNull().Then(() => NewSearch());
				}
			}
		}

		public bool CaseSensitiveSearch { get; set; }
		public bool SearchOnline { get; set; }

		protected string searchText;
		protected WebBrowser browser;
		protected List<Tuple<NotecardRecord, int>> hits;
		protected NotecardController searchNotecardController;

		public FindController()
		{
			browser = new WebBrowser();
			browser.ScriptErrorsSuppressed = true;
			browser.DocumentCompleted += OnDocumentCompleted;
		}

		/// <summary>
		/// Disables the prev/next buttons and clears the "found" list, used when the search string changes (the user is entering a new search)
		/// </summary>
		protected void NewSearch()
		{
			View.DisablePreviousNext();
			View.Clear();
		}

		protected void OpenAndFindFirst(object sender, object tag)
		{
			OpenNotecardAndFindFirstHit((NotecardRecord)tag);
		}

		protected void OpenNotecardAndFindLastHit(NotecardRecord rec)
		{
			ApplicationController.OpenANotecard(rec, nc =>
			{
				// When the document has loaded...
				nc.OnDocumentLoadedAction = null;
				nc.View.FindLast(SearchText);
				View.EnablePreviousNext();
				searchNotecardController = nc;
			});
		}

		protected void OpenNotecardAndFindFirstHit(NotecardRecord rec)
		{
			ApplicationController.OpenANotecard(rec, nc=>
			{
				// When the document has loaded...
				nc.OnDocumentLoadedAction = null;
				nc.View.FindFirst(SearchText);
				View.EnablePreviousNext();
				searchNotecardController = nc;
			});
		}

		protected void Previous(object sender, EventArgs args)
		{
			searchNotecardController.View.FindPrevious(SearchText).Else(() => SearchPreviousNotecard());
		}

		protected void Next(object sender, EventArgs args)
		{
			searchNotecardController.View.FindNext(SearchText).Else(() => SearchNextNotecard());
		}

		protected void SearchPreviousNotecard()
		{
			if (View.Index == 0)
			{
				View.Index = hits.Count - 1;
			}
			else
			{
				--View.Index;
			}

			OpenNotecardAndFindLastHit(hits[View.Index].Item1);
		}

		protected void SearchNextNotecard()
		{
			if (View.Index + 1 >= hits.Count)
			{
				// Wrap to first notecard.
				View.Index = 0;
			}
			else
			{
				// Open next notecard.
				++View.Index;
			}

			OpenNotecardAndFindFirstHit(hits[View.Index].Item1);
		}

		protected void PerformSearch(object sender, EventArgs args)
		{
			ClearResults();
			GetHits();
			ShowResults();
		}

		protected void CreateIndexEntries(object sender, EventArgs args)
		{
			ClearResults();
			GetHits();
			ShowResults();

			foreach (var item in hits)
			{
				NotecardRecord rec = item.Item1;
				CreateIndexEntry(rec, SearchText);
			}

			MessageBox.Show("'" + SearchText + "' has been indexed.", "Indices Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		protected void CreateIndexEntry(NotecardRecord rec, string text)
		{
			string[] tagList = ApplicationModel.GetTags(rec);
			string allTags = String.Join(";", tagList);
			allTags.Contains(SearchText).Else(() => ApplicationModel.SetTags(rec, allTags + ";" + text));
		}

		protected void CancelSearch(object sender, EventArgs args)
		{
			((Form)((Button)sender).Parent).Close();
		}

		protected void UpdateResults(NotecardRecord rec, int occurances)
		{
			hits.Add(new Tuple<NotecardRecord, int>(rec, occurances));
		}

		protected void ClearResults()
		{
			View.Clear();
		}

		protected void ShowResults()
		{
			View.BuildTree(hits);
		}

		protected void GetHits()
		{
			hits = new List<Tuple<NotecardRecord, int>>();
			int i = 0;
			View.ResetProgressBar(ApplicationModel.NumNotecards);

			// This needs to perform a search on:
			// notecards not in edit mode (search Html in the record)
			// notecards that are in edit mode (search html in the Html Editor)
			// URL's: must load the URL and search it if it isn't already opened in the application.
			ApplicationModel.ForEachNotecard(n =>
			{
				View.SetProgressBar(i++);

				if (n.IsOpen)
				{
					// If it's open, search the HTML editor if we're in edit mode.
					ApplicationController.ActiveNotecardControllers.SingleOrDefault(t => t.NotecardRecord == n).IfNotNull(c =>
					{
						int occurances = FindOccurances(SearchText, c.GetHtml());
						(occurances > 0).Then(() => UpdateResults(n, occurances));
					});
				}
				else
				{
					// If document is a URL, then search only if the user requested us to load the pages.
					if ( (!String.IsNullOrEmpty(n.URL)) && (SearchOnline) )
					{
					}
					// Orphan notecards can have null HTML.
					else if (!String.IsNullOrEmpty(n.HTML))
					{
						int occurances = FindOccurances(SearchText, n.HTML);
						(occurances > 0).Then(() => UpdateResults(n, occurances));
					}
				}
			});

			View.ResetProgressBar(ApplicationModel.NumNotecards);
		}

		protected int FindOccurances(string searchString, string html)
		{
			int occurances = 0;
			int startingIndex = 0;
			bool found = false;

			do
			{
				// Get the case sensitive or insensitive first match from the starting index position.
				int idx = html.IndexOf(searchString, startingIndex, CaseSensitiveSearch ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
				found = idx >= 0;

				if (found)
				{
					// If found, increment the number of occurances and update the starting index.
					++occurances;
					startingIndex = idx + searchString.Length;
				}
				
			} while (found);

			return occurances;
		}

		protected void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
		}
	}
}


