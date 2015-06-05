; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Intertexti"
#define MyAppVersion "0.1CTP"
#define MyAppPublisher "Intertexti"
#define MyAppURL "http://www.intertexti.com/"
#define MyAppExeName "Intertexti.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{7930654C-FA1C-46EB-89C7-D1D1F6E7E013}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
; was {pf} for Program Files
DefaultDirName={sd}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\Intertexti.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\DevExpress.Data.v10.1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\DevExpress.Utils.v10.1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\DevExpress.XtraEditors.v10.1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\DevExpress.XtraTreeList.v10.1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\Microsoft.mshtml.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\MSDN.HtmlEditorControl.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\WeifenLuo.WinFormsUI.Docking.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\release\CTP 0.1\appState.xml"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\release\CTP 0.1\defaultLayout.xml"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\release\CTP 0.1\layout.xml"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\release\CTP 0.1\user guide.intertexti"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Personal\trunk\Intertexti-VS2012\bin\x86\Debug\release\CTP 0.1\images\*"; DestDir: "{app}\images"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

