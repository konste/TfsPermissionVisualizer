using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using CommandLine;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace TfsPermissionVisualizer
{
    static class ConsoleEntryPoint
    {
        [STAThread]
        static int Main(string[] args)
        {
            // C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe with "/rootsuffix Exp"
            // or
            // Start Project with -c https://dev.bittitan.com/tfs/BitTitan -p SendBus -f TfsPoking.dgml

            Options options = new Options();

            Parser parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.HelpWriter = Console.Error;
                settings.ParsingCulture = CultureInfo.InvariantCulture;
            });

            bool result = parser.ParseArguments(args, options);

            if (!result)
            {
                ConsoleEntryPoint.Fail();
                return -1;
            }

            Uri collectionUri = new Uri(options.Collection);
            TfsTeamProjectCollection tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri, CredentialCache.DefaultCredentials);
            tfsTeamProjectCollection.EnsureAuthenticated();
            ICommonStructureService commonStructureService = tfsTeamProjectCollection.GetService<ICommonStructureService>();
            IEnumerable<ProjectInfo> projectInfoList = commonStructureService.ListAllProjects();

            if (options.Projects.Any())
            {
                projectInfoList = projectInfoList.Where(pil => options.Projects.Contains(pil.Name)).ToArray();
            }

            TfsPermissionGraphGenerator generator = new TfsPermissionGraphGenerator();
            XDocument xDocument = generator.GenerateDependencyGraph(tfsTeamProjectCollection, projectInfoList);
            string dgmlTempFilePath = (String.IsNullOrEmpty(options.OutputFile)) ? "TfsPermissionVisualizer.dgml" : options.OutputFile;
            xDocument.Save(dgmlTempFilePath);
            return 0;
        }

        /// <summary>
        /// Sends a failure message of the ERROR output and wait for a press on the enter key.
        /// </summary>
        /// <param name="message">message about the failure</param>
        private static void Fail(string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Console.Error.WriteLine(message);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
