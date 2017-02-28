using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace TfsPermissionVisualizer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(Guids.guidTfsPermissionVisualizerPackagePkgString)]
    [ProvideBindingPath]
    public sealed class TfsPermissionVisualizerPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public TfsPermissionVisualizerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandId = new CommandID(Guids.guidTfsPermissionVisualizerPackageCmdSet, (int)PkgCmdIdList.GENERATE_GRAPH_PERMISSIONS);
                MenuCommand menuItem = new MenuCommand(this.MenuItemCallback, menuCommandId );
                mcs.AddCommand( menuItem );
            }
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            TeamProjectPicker picker = new TeamProjectPicker(TeamProjectPickerMode.MultiProject, false);
            if (picker.ShowDialog() != DialogResult.OK)
                return;

            ProjectInfo[] projectsInfo = picker.SelectedProjects;
            TfsTeamProjectCollection tfsTeamProjecttollection = picker.SelectedTeamProjectCollection;

            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Debug.Assert(dte2 != null, "dte2 != null");

            if (projectsInfo.Length == 0)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                IServiceProvider serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2);

                VsShellUtilities.ShowMessageBox(serviceProvider,
                    "At least one team project must be selected",
                    "Tfs Permission Visualizer Error:",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            string dgmlTempFilePath = System.IO.Path.GetTempFileName() + ".dgml";

            CommonMessagePump msgPump = new CommonMessagePump
            {
                AllowCancel = false,
                EnableRealProgress = false,
                WaitTitle = "Building Permission graph...",
                WaitText = "Please wait while we are building security groups Permission graph."
            };


            System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(() =>
            {
                TfsPermissionGraphGenerator generator = new TfsPermissionGraphGenerator();
                XDocument xDocument = generator.GenerateDependencyGraph(tfsTeamProjecttollection, projectsInfo);
                xDocument.Save(dgmlTempFilePath);
            });

            // ReSharper disable once SuspiciousTypeConversion.Global
            // ReSharper disable once PossibleInvalidCastException
            CommonMessagePumpExitCode exitCode = msgPump.ModalWaitForHandles(((IAsyncResult)task).AsyncWaitHandle);
    
            dte2.ItemOperations.OpenFile(dgmlTempFilePath);
        }
    }
}
