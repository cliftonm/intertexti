using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Intertexti.Controllers;

namespace Intertexti.Actions
{
	public class SetDocumentText : DeclarativeAction
	{
		public INotecardController Controller { get; set; }
		public string Text { get; set; }

		public override void EndInit()
		{
			Controller.SetDocumentText(Text);
		}
	}
}
