using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Linq;
using System.IO;
using System.Text;

using Clifton.Tools.Data;
using Clifton.Tools.Strings.Extensions;
using Clifton.Tools.Xml;

namespace Intertexti.Models
{
	public class ApplicationModel
	{
		protected bool isDirty;

		public delegate void DirtyModelDlgt(bool dirtyState);
		public delegate void NotecardDeletedDlgt(NotecardRecord rec);
		public delegate void NotecardAddedDlgt(NotecardRecord rec);
		public delegate void TagsChangedDlgt(NotecardRecord rec);
		public delegate void AssociationAddedDlgt(NotecardRecord recParent, NotecardRecord recChild);
		public delegate void AssociationRemovedDlgt(NotecardRecord recParent, NotecardRecord recChild);
		public delegate void ActiveNotecardChangedDlgt(NotecardRecord rec);

		public event DirtyModelDlgt ModelIsDirty;
		public event NotecardDeletedDlgt NotecardDeleted;
		public event NotecardAddedDlgt NotecardAdded;
		public event TagsChangedDlgt TagsChanged;
		public event AssociationAddedDlgt AssociationAdded;
		public event AssociationRemovedDlgt AssociationRemoved;
		public event ActiveNotecardChangedDlgt ActiveNotecardChanged;

		public int NumNotecards
		{
			get { return this["Notecards"].Count(); }
		}

		public bool IsDirty 
		{
			get { return isDirty; }
			set
			{
				isDirty = value;
				ModelIsDirty.IfNotNull(e => e(isDirty));
			}
		}

		protected NotecardRecord activeNotecardRecord;

		public NotecardRecord ActiveNotecardRecord 
		{
			get	{ return activeNotecardRecord;}
			set
			{
				activeNotecardRecord = value;
				ActiveNotecardChanged.IfNotNull(e => e(activeNotecardRecord));
			}
		}

		public bool HasFilename { get { return !String.IsNullOrEmpty(filename); } }

		protected DataSet dataSet;
		protected string filename;

		protected EnumerableRowCollection<DataRow> this[string tableName] { get { return dataSet.Tables[tableName].AsEnumerable(); } }

		public ApplicationModel()
		{
			dataSet = SchemaHelper.CreateSchema();
		}

		/// <summary>
		/// Cancels any changes, which currently simply sets the IsDirty flag to false.
		/// </summary>
		public void CancelChanges()
		{
			IsDirty = false;
		}

		public void NewModel()
		{
			dataSet = SchemaHelper.CreateSchema();
			filename = String.Empty;
			IsDirty = false;
		}

		public void LoadModel(string filename)
		{
			this.filename = filename;
			dataSet = SchemaHelper.CreateSchema();
			dataSet.ReadXml(filename, XmlReadMode.IgnoreSchema);
			IsDirty = false;
		}

		public void SaveModel()
		{
			dataSet.WriteXml(filename, XmlWriteMode.WriteSchema);
			IsDirty = false;
		}

		public void SaveModelAs(string filename)
		{
			this.filename = filename;
			dataSet.WriteXml(filename, XmlWriteMode.WriteSchema);
			IsDirty = false;
		}

		public NotecardRecord NewNotecard()
		{
			IsDirty = true;
			DataRow row = NewRow("Notecards");
			ActiveNotecardRecord = NotecardRecord.GetRecord(this, row);
			NotecardAdded.IfNotNull(t => t(ActiveNotecardRecord));

			return ActiveNotecardRecord;
		}

		public List<NotecardRecord> GetOpenNotecards()
		{
			List<NotecardRecord> openNotecards = new List<NotecardRecord>();

			this["Notecards"].Where(t => t.Field<bool>("IsOpen")).ForEach(t => openNotecards.Add(NotecardRecord.GetRecord(this, t)));

			return openNotecards;
		}

		public void Associate(NotecardRecord parent, NotecardRecord child)
		{
			DataRow row = dataSet.Tables["NotecardReferences"].NewRow();
			row["NotecardParentID"] = parent.ID;
			row["NotecardChildID"] = child.ID;
			dataSet.Tables["NotecardReferences"].Rows.Add(row);
			AssociationAdded.IfNotNull(e => e(parent, child));
		}

		/// <summary>
		/// Deletes the active notecard from the dataset.
		/// </summary>
		public void DeleteNotecard()
		{
			this["Notecards"].Where(t => t.Field<int>("ID") == ActiveNotecardRecord.ID).Single().Delete();
			NotecardDeleted.IfNotNull(t => t(ActiveNotecardRecord));
			ActiveNotecardRecord = null;
		}

		public void DeleteNotecard(NotecardRecord rec)
		{
			this["Notecards"].Where(t => t.Field<int>("ID") == rec.ID).Single().Delete();
			NotecardDeleted.IfNotNull(t => t(rec));
		}

		public void DeleteReference(NotecardRecord parent, NotecardRecord child)
		{
			IsDirty = true;
			this["NotecardReferences"].First(t => (t.Field<int>("NotecardParentID") == parent.ID) && (t.Field<int>("NotecardChildID") == child.ID)).Delete();
			AssociationRemoved.IfNotNull(e => e(parent, child));
		}

		public List<NotecardRecord> GetReferences()
		{
			List<NotecardRecord> references = this["Notecards"].Join(this["NotecardReferences"].Where(t => t.Field<int>("NotecardParentID") == ActiveNotecardRecord.ID),
			pk => pk.Field<int>("ID"),
			fk => fk.Field<int>("NotecardChildID"),
			(pk, fk) => NotecardRecord.GetRecord(this, pk)).ToList();

			return references;
		}

		public List<NotecardRecord> GetReferences(NotecardRecord rec)
		{
			List<NotecardRecord> references = this["Notecards"].Join(this["NotecardReferences"].Where(t => t.Field<int>("NotecardParentID") == rec.ID),
			pk => pk.Field<int>("ID"),
			fk => fk.Field<int>("NotecardChildID"),
			(pk, fk) => NotecardRecord.GetRecord(this, pk)).ToList();

			return references;
		}

		public List<NotecardRecord> GetReferencedFrom()
		{
			List<NotecardRecord> references = this["Notecards"].Join(this["NotecardReferences"].Where(t => t.Field<int>("NotecardChildID") == ActiveNotecardRecord.ID),
			pk => pk.Field<int>("ID"),
			fk => fk.Field<int>("NotecardParentID"),
			(pk, fk) => NotecardRecord.GetRecord(this, pk)).ToList();

			return references;
		}

		public List<NotecardRecord> GetReferencedFrom(NotecardRecord rec)
		{
			List<NotecardRecord> references = this["Notecards"].Join(this["NotecardReferences"].Where(t => t.Field<int>("NotecardChildID") == rec.ID),
			pk => pk.Field<int>("ID"),
			fk => fk.Field<int>("NotecardParentID"),
			(pk, fk) => NotecardRecord.GetRecord(this, pk)).ToList();

			return references;
		}

		/// <summary>
		/// Returns root-level notecard records, which are notecards that are not referenced by (no child ID) other notecards.
		/// </summary>
		/// <returns></returns>
		public List<NotecardRecord> GetRootNotecards()
		{
			List<NotecardRecord> rootRecs = this["Notecards"].LeftExcludingJoin(
				this["NotecardReferences"], 
				pk => pk.Field<int>("ID"), 
				fk => fk.Field<int>("NotecardChildID"), 
				(pk, fk) => pk).Select(t => NotecardRecord.GetRecord(this, t)).ToList();

			return rootRecs;
		}

		public void ForEachNotecard(Action<NotecardRecord> action)
		{
			this["Notecards"].ForEach(t => action(NotecardRecord.GetRecord(this, t)));
		}

		/// <summary>
		/// Returns the tags associated with the active record.
		/// </summary>
		public string GetTags()
		{
			var tagList = this["Metadata"].Where(t => t.Field<int>("NotecardID") == ActiveNotecardRecord.ID).Select(t => t.Field<string>("Tag"));
			string ret = String.Join(";", tagList.ToArray());

			return ret;
		}

		/// <summary>
		/// Returns the tags as a string array for the specified notecard.
		/// </summary>
		/// <param name="rec"></param>
		/// <returns></returns>
		public string[] GetTags(NotecardRecord rec)
		{
			var tagList = this["Metadata"].Where(t => t.Field<int>("NotecardID") == rec.ID).Select(t => t.Field<string>("Tag"));

			return tagList.ToArray();
		}

		/// <summary>
		/// Adds a tag to the specified notecard record.
		/// </summary>
		public string AddTagIfUnique(NotecardRecord rec, string tag)
		{
			string[] tagList = GetTags(rec);
			string allTags = String.Join(";", tagList);

			if (!tagList.Contains(tag))
			{
				allTags = allTags + (allTags.IsBlank() ? "" : ";") + tag;
				SetTags(rec, allTags); 
			}

			return allTags;
		}

		public void SetTags(string tags)
		{
			SetTags(ActiveNotecardRecord, tags);
		}

		/// <summary>
		/// Sets the tags for the active record.
		/// </summary>
		public void SetTags(NotecardRecord rec, string tags)
		{
			bool changed = tags != String.Join(";", GetTags(rec));

			if (changed)
			{
				var tagList = tags.Split(';').Select(t => t.Trim());
				var currentTags = this["Metadata"].Where(t => t.Field<int>("NotecardID") == rec.ID).Select(t => t.Field<string>("Tag"));

				// Remove tags no longer in the tag list.
				// Force ToArray so we get a copy, as we may be updating the table rows.
				currentTags.ToArray().ForEach(t =>
					{
						if (!tagList.Contains(t))
						{
							var deletedRows = this["Metadata"].Where(r => (r.Field<int>("NotecardID") == rec.ID) && (r.Field<string>("Tag") == t));
							// Force ToArray so we get a copy, as we may be updating the table rows.
							deletedRows.ToArray().ForEach(r => dataSet.Tables["Metadata"].Rows.Remove(r));
						}
					});

				// Add new tags.
				tagList.ForEach(t =>
					{
						if ((!currentTags.Contains(t)) && (!String.IsNullOrEmpty(t)))
						{
							DataRow row = dataSet.Tables["Metadata"].NewRow();
							row["NotecardID"] = rec.ID;
							row["Tag"] = t;
							dataSet.Tables["Metadata"].Rows.Add(row);
						}
					});

				IsDirty = true;
				TagsChanged.IfNotNull(e => e(rec));
			}
		}

		/// <summary>
		/// Creates and adds a new row to the table - this works only when there are no required field values other than the ID.
		/// </summary>
		protected DataRow NewRow(string tableName)
		{
			DataRow row = dataSet.Tables["Notecards"].NewRow();
			dataSet.Tables["Notecards"].Rows.Add(row);

			return row;
		}

		public bool HasLastViewedNotecard
		{
			get	{return dataSet.Tables["ApplicationData"] != null; }
		}

		public NotecardRecord LastViewedNotecard
		{
			get
			{
				NotecardRecord rec = null;
				int lastID = dataSet.Tables["ApplicationData"].Rows[0].Field<int>("LastViewedNotecardID");
				// TODO: For some reason (after creating a new deck and adding notecards) the record ID was not correct!
				this["Notecards"].SingleOrDefault(r => r.Field<int>("ID") == lastID).IfNotNull(row=>rec=NotecardRecord.GetRecord(this, row));

				return rec;
			}
		}

		public void UpdateLastViewed(NotecardRecord rec)
		{
			DataTable t = dataSet.Tables["ApplicationData"];
			DataRow row;

			if (t.Rows.Count == 0)
			{
				row = t.NewRow();
				t.Rows.Add(row);
			}
			else
			{
				row = t.Rows[0];
			}

			row["LastViewedNotecardID"] = rec.ID;
		}
	}
}
