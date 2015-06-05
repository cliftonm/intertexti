using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WeifenLuo.WinFormsUI.Docking;

using Intertexti.Controllers;

namespace Intertexti.Actions
{
	public class RegisterDocumentController : DeclarativeAction
	{
		public ApplicationFormController App { get; protected set; }
		public IDockContent Container { get; protected set; }
		public NotecardController Controller { get; protected set; }

		public RegisterDocumentController()
		{
		}

		public override void EndInit()
		{
			App.RegisterDocumentController(Container, Controller);
		}
	}
}
