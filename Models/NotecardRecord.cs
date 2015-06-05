using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Linq;
using System.Text;

using Clifton.Tools.Data;

namespace Intertexti.Models
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// If the value has changed, sets the model as dirty, updates row's field, and returns true, otherwise simply returns false.
		/// </summary>
		public static bool UpdateField<T>(this DataRow row, ApplicationModel model, string fieldName, T value)
		{
			bool ret = false;

			if (!row[fieldName].Equals(value))
			{
				// If all we're doing is converting a DBNull.Value to an empty string, return false.
				if ((row[fieldName] != DBNull.Value) || (value.ToString() != String.Empty))
				{
					// TODO: Here's a nice way we could implement undo / redo buffering!
					// Except it wouldn't handle creating new notecards or deleting existing ones.
					model.IsDirty = true;
					row.SetField<T>(fieldName, value);
					ret = true;
				}
			}

			return ret;
		}
	}

	public class NotecardRecord
	{
		public delegate void TitleChangedDlgt(NotecardRecord rec);
		public delegate void TableOfContentsChangedDlgt(NotecardRecord rec);
		public delegate void DateCreatedChangedDlgt(NotecardRecord rec);
		public delegate void DateModifiedChangedDlgt(NotecardRecord rec);
		public delegate void DateViewedChangedDlgt(NotecardRecord rec);
		public delegate void CheckStateChangedDlgt(NotecardRecord rec);

		public event TitleChangedDlgt TitleChanged;
		public event TableOfContentsChangedDlgt TableOfContentsChanged;
		public event DateCreatedChangedDlgt DateCreatedChanged;
		public event DateModifiedChangedDlgt DateModifiedChanged;
		public event DateViewedChangedDlgt DateViewedChanged;
		public event CheckStateChangedDlgt CheckStateChanged;

		protected ApplicationModel model;

		public int ID { get { return row.Field<int>("ID"); } }
		public bool IsDirty { get; set; }

		public string TableOfContents
		{
			get { return row.Field<string>("TableOfContents"); }
			set {row.UpdateField<string>(model, "TableOfContents", value).Then(() => TableOfContentsChanged.IfNotNull(e => e(this))); }
		}

		public string URL
		{
			get { return row.Field<string>("URL"); }
			set { row.UpdateField<string>(model, "URL", value); }
		}

		public string HTML
		{
			get { return row.Field<string>("HTML"); }
			set { row.UpdateField<string>(model, "HTML", value); }
		}

		public bool IsOpen
		{
			get { return row.Field<bool?>("IsOpen").GetValueOrDefault(false); }
			set { row.UpdateField<bool>(model, "IsOpen", value); }
		}

		public bool IsChecked
		{
			get { return row.Field<bool?>("IsChecked").GetValueOrDefault(false); }
			set { row.UpdateField<bool>(model, "IsChecked", value).Then(() => CheckStateChanged.IfNotNull(e => e(this))); }
		}

		public string Title
		{
			get { return row.Field<string>("Title"); }
			set { row.UpdateField<string>(model, "Title", value).Then(() => TitleChanged.IfNotNull(e => e(this))); }
		}

		public DateTime? DateCreated
		{
			get { return row.Field<DateTime?>("DateCreated"); }
			set { row.UpdateField<DateTime?>(model, "DateCreated", value).Then(() => DateCreatedChanged.IfNotNull(e=>e(this))); }
		}

		public DateTime? DateModified
		{
			get { return row.Field<DateTime?>("DateModified"); }
			set { row.UpdateField<DateTime?>(model, "DateModified", value).Then(() => DateModifiedChanged.IfNotNull(e => e(this)));	}
		}

		public DateTime? DateLastViewed
		{
			get { return row.Field<DateTime?>("DateLastViewed"); }
			set { row.UpdateField<DateTime?>(model, "DateLastViewed", value).Then(() => DateViewedChanged.IfNotNull(e => e(this)));	}
		}

		public bool Deleted
		{
			get { return (row.RowState == DataRowState.Deleted) || (row.RowState == DataRowState.Detached); }
		}

		protected DataRow row;

		protected NotecardRecord(ApplicationModel model, DataRow row)
		{
			this.model = model;
			this.row = row;
			rowRecordMap[row] = this;
		}

		protected static Dictionary<DataRow, NotecardRecord> rowRecordMap = new Dictionary<DataRow, NotecardRecord>();

		/// <summary>
		/// Create a new NotecardRecord only if the data row hasn't yet been mapped.
		/// </summary>
		public static NotecardRecord GetRecord(ApplicationModel model, DataRow row)
		{
			NotecardRecord rec;

			if (!rowRecordMap.TryGetValue(row, out rec))
			{
				rec = new NotecardRecord(model, row);
			}

			return rec;
		}

		/// <summary>
		/// Returns the table of contents entry if it exists, otherwise, default title passed in, 
		/// unless it's blank, in which case we default to the URL.
		/// </summary>
		public string GetTitle()
		{
			string ret = Title;

			if (!String.IsNullOrEmpty(TableOfContents))
			{
				ret = TableOfContents;
			}
			else if (String.IsNullOrEmpty(Title))
			{
				ret = URL;
			}

			return ret ?? String.Empty;
		}
	}
}
