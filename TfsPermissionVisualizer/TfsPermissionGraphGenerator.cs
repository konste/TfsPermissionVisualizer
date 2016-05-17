using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows.Forms.Design;
using System.Xml.Linq;
using CommandLine;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsPermissionVisualizer
{
    public class TfsPermissionGraphGenerator
    {
        public XDocument GenerateDependencyGraph(TfsTeamProjectCollection tfsTeamProjectCollection, IEnumerable<ProjectInfo> projectInfoArray)
        {
            DirectedGraph directedGraph = new DirectedGraph();
            
            foreach (ProjectInfo projectInfo in projectInfoArray)
            {
                TeamProjectScanner.ScanUsers(tfsTeamProjectCollection, projectInfo);
                TeamProjectScanner.ScanPermissions(tfsTeamProjectCollection, projectInfo, directedGraph);
            }
            return directedGraph.ProduceGraph();
        }
    }
}
