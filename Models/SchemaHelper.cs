﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;

using Clifton.Tools.Strings.Extensions;
using Clifton.Tools.Xml;

namespace Intertexti.Models
{
	public static class SchemaHelper
	{
		public static DataSet CreateSchema()
		{
			MycroParser mp = new MycroParser();
			// Instantiation of schemas using .NET classes needs some help.
			mp.CustomAssignProperty += new CustomAssignPropertyDlgt(CustomAssignProperty);
			mp.InstantiateClass += new InstantiateClassDlgt(InstantiateClass);
			mp.UnknownProperty += new UnknownPropertyDlgt(UnknownProperty);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(Intertexti.Properties.Resources.schema);
			// doc.Load("schema.xml");
			mp.Load(doc, "Schema", null);
			DataSet dataSet = (DataSet)mp.Process();

			return dataSet;
		}

		public static void CustomAssignProperty(object sender, CustomPropertyEventArgs pea)
		{
			if (pea.PropertyInfo.Name == "DataType")
			{
				Type t = Type.GetType(pea.Value.ToString());
				pea.PropertyInfo.SetValue(pea.Source, t, null);
				pea.Handled = true;
			}
			else if (pea.PropertyInfo.Name == "PrimaryKey")
			{
				pea.PropertyInfo.SetValue(pea.Source, new DataColumn[] { (DataColumn)pea.Value }, null);
				pea.Handled = true;
			}
		}

		public static void InstantiateClass(object sender, ClassEventArgs cea)
		{
			MycroParser mp = (MycroParser)sender;

			if (cea.Type.Name == "DataRelation")
			{
				string name = cea.Node.Attributes["Name"].Value;
				string childColumnRef = cea.Node.Attributes["ChildColumn"].Value;
				string parentColumnRef = cea.Node.Attributes["ParentColumn"].Value;
				DataColumn dcChild = (DataColumn)mp.GetInstance(childColumnRef.Between('{', '}'));
				DataColumn dcParent = (DataColumn)mp.GetInstance(parentColumnRef.Between('{', '}'));
				cea.Result = new DataRelation(name, dcParent, dcChild);
				cea.Handled = true;
			}
		}

		public static void UnknownProperty(object sender, UnknownPropertyEventArgs pea)
		{
			// Ignore these attributes.
			// TODO: add the element name into the args, so we can also test the element for which we want to ignore certain properties.
			if ((pea.PropertyName == "ChildColumn") || (pea.PropertyName == "ParentColumn"))
			{
				pea.Handled = true;
			}
		}
	}
}
