using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

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
