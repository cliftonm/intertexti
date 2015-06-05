using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;

using Clifton.ApplicationStateManagement;
using Clifton.Tools.Data;
using Clifton.Tools.Strings.Extensions;
using Clifton.Tools.Xml;

// Intertexti is Latin for "intertwined"

using Intertexti.Controllers;
using Intertexti.lib;
using Intertexti.Views;

namespace Intertexti
{
	static class Program
	{
		const int FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
		const int SET_FEATURE_ON_PROCESS = 0x00000002;

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32.dll")]
		static extern IntPtr GetFocus();

		[DllImport("urlmon.dll")]
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		static extern int CoInternetSetFeatureEnabled(
			int FeatureEntry,
			[MarshalAs(UnmanagedType.U4)] int dwFlags,
			bool fEnable);

		public static Form MainForm;
		public static uint UpdateIndexTreesMessage;
		
		public static StatePersistence AppState;

		// This is very snazzy, keeping it here for the moment.
		//static public Type GetDeclaredType<TSelf>(TSelf self)
		//{
		//	return typeof(TSelf);
		//}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			try
			{
				// Get rid of annoying browser navigation click sounds.
				// http://stackoverflow.com/questions/10456/howto-disable-webbrowser-click-sound-in-your-app-only
				CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);

				AppState = new StatePersistence();
				// RightClickWindowMessage = RegisterWindowMessage("IntertextiRightClick");
				UpdateIndexTreesMessage = RegisterWindowMessage("IntertextiUpdateIndexTrees");

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				AppState.ReadState("appState.xml");																	// Load the last application state.
				// MainForm = Instantiate<Form>("mainform.xml", null);													// Instantiate the form.
				MainForm = InstantiateFromResource<Form>(Intertexti.Properties.Resources.mainform, null);													// Instantiate the form.
				Application.Run(MainForm);
				AppState.WriteState("appState.xml");																// Save the application state.
			}
			catch (Exception ex)
			{
				while (ex.InnerException != null) ex = ex.InnerException;
				MessageBox.Show(ex.Message+"\r\n\r\nWe apologize for the problem.", "Fatal Problem Encountered", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static void PostMessageUpdateIndexTrees()
		{
			PostMessage(MainForm.Handle, UpdateIndexTreesMessage, (IntPtr)0, (IntPtr)0);
		}

		/// <summary>
		/// This is a strange helper needed to resolve the actual control that has focus when the
		/// user presses Ctrl+C, etc.  After adding menubar shortcuts, the menu handler now receives
		/// the event for TextBox controls, preventing the default behavior of the control.  We need
		/// to determine whether a TextBox has the focus and if so, call the appropriate methods for the
		/// desired behavior on the control rather than assuming that the user is copying/cutting/pasting from
		/// the HTML editor.  Interestingly, the HTML editor and browser capture these keyboard shortcuts and
		/// handle them, so the event doesn't even fire from the keyboard.  Our handler simply provides a menu-based
		/// activation of the function for these controls.  
		/// </summary>
		/// <returns></returns>
		public static Control GetFocusedControl()
		{
			IntPtr handle = GetFocus();
			Control focused = null;

			if (handle != IntPtr.Zero)
			{
				focused = Control.FromHandle(handle);
			}

			return focused;
		}

		public static void Try(Action f)
		{
			try
			{
				f();
			}
			catch(Exception ex)
			{
				// Silent catching of exceptions
				while (ex.InnerException != null) ex = ex.InnerException;
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
		}
/*
		public static T Instantiate<T>(string filename, Action<MycroParser> AddInstances)
		{
			MycroParser mp = new MycroParser();
			AddInstances.IfNotNull(t => t(mp));
			XmlDocument doc = new XmlDocument();
			doc.Load(filename);
			mp.Load(doc, "Form", null);
			T obj = (T)mp.Process();

			// Pass object collection to the instantiated class if it implements IMycroParserInstantiatedObject.
			if (obj is IMycroParserInstantiatedObject)
			{
				((IMycroParserInstantiatedObject)obj).ObjectCollection = mp.ObjectCollection;
			}

			return obj;
		}
*/
		public static T InstantiateFromResource<T>(string xml, Action<MycroParser> AddInstances)
		{
			MycroParser mp = new MycroParser();
			AddInstances.IfNotNull(t => t(mp));
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			mp.Load(doc, "Form", null);
			T obj = (T)mp.Process();

			// Pass object collection to the instantiated class if it implements IMycroParserInstantiatedObject.
			if (obj is IMycroParserInstantiatedObject)
			{
				((IMycroParserInstantiatedObject)obj).ObjectCollection = mp.ObjectCollection;
			}

			return obj;
		}
	}
}
