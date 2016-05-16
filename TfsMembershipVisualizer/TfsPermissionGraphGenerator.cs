using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace TfsPermissionVisualizer
{
    public class TfsPermissionGraphGenerator
    {
        static void Main()
        {
            Uri collectionUri = new Uri("https://dev.bittitan.com/tfs/BitTitan");
            string[] projectUris = new string[] { //"vstfs:///Classification/TeamProject/1e35ba8e-16eb-4585-9025-bcff4a0050ae",
                //"vstfs:///Classification/TeamProject/938a9048-0923-472f-ba8c-ae05679a6dae",
                //"vstfs:///Classification/TeamProject/c9dc90e8-ab03-4cb3-a392-029ae7237318",
                //"vstfs:///Classification/TeamProject/05495f9e-4221-4e03-9641-43180ee3c60e",
                //"vstfs:///Classification/TeamProject/3a56187d-c011-4ce1-8f06-e414952e7b6f",
                "vstfs:///Classification/TeamProject/9ba2ea43-472b-4a6f-9812-b2316eded59e" // SendBus
                //"vstfs:///Classification/TeamProject/74ea25fa-7d73-4df7-a1f3-37de1b368f50",
                //"vstfs:///Classification/TeamProject/0ee485f4-6160-4c67-9660-a633868a3a25",
                //"vstfs:///Classification/TeamProject/c46f33c7-8667-4162-8474-3b4c435966ba"
            };

            TfsPermissionGraphGenerator generator = new TfsPermissionGraphGenerator();
            TfsTeamProjectCollection tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(collectionUri);

            ICommonStructureService4 commonStructureService = tfsTeamProjectCollection.GetService<ICommonStructureService4>();
            ProjectInfo[] projectInfoArray = projectUris.Select(projectUri => commonStructureService.GetProject(projectUri)).ToArray();

            XDocument xDocument = generator.GenerateDependencyGraph(tfsTeamProjectCollection, projectInfoArray);
            string dgmlTempFilePath = "TfsPoking.dgml";
            xDocument.Save(dgmlTempFilePath);
        }

        public const string IDENTITY_TYPE_TFS = "Microsoft.TeamFoundation.Identity";
        public const string IDENTITY_TYPE_WINDOWS = "System.Security.Principal.WindowsIdentity";

        public XDocument GenerateDependencyGraph(TfsTeamProjectCollection tfsTeamProjectCollection, ProjectInfo[] projectInfoArray)
        {
            DirectedGraph directedGraph = new DirectedGraph();
            
            List<string> projectUriList = projectInfoArray.Select(p => p.Uri).ToList();
            foreach (ProjectInfo projectInfo in projectInfoArray)
            {
                TeamProjectScanner.ScanUsers(tfsTeamProjectCollection, projectInfo);
                TeamProjectScanner.ScanPermissions(tfsTeamProjectCollection, projectInfo, directedGraph);
            }
            return directedGraph.ProduceGraph();
        }
    }
}
