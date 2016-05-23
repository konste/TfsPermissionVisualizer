using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Services.Identity;
using IdentityDescriptor = Microsoft.TeamFoundation.Framework.Client.IdentityDescriptor;

namespace TfsPermissionVisualizer
{
    public static class TeamProjectScanner
    {
        internal static readonly Dictionary<string, TeamFoundationIdentity> IdToIdentity = new Dictionary<string, TeamFoundationIdentity>();
        private static readonly Dictionary<string, TeamFoundationIdentity> DisplayNameToIdentity = new Dictionary<string, TeamFoundationIdentity>();

        public static void ScanUsers(TfsTeamProjectCollection tfsTeamProjectCollection, ProjectInfo projectInfo)
        {
            IIdentityManagementService identityManagementService = tfsTeamProjectCollection.GetService<IIdentityManagementService>();

            TeamFoundationIdentity[] projectSecurityGroups = identityManagementService.ListApplicationGroups(
                 projectInfo.Uri, ReadIdentityOptions.None);

            // Get projectValidUsersIdentity Identifier
            TeamFoundationIdentity projectValidUsersIdentity = projectSecurityGroups
                 .FirstOrDefault(psg => psg.GetProperty("SpecialType").ToString().Equals("EveryoneApplicationGroup"));

            if (projectValidUsersIdentity == null)
                return;

            // Expand projectValidUsersIdentity from Identifier to get the list of Members 
            projectValidUsersIdentity = identityManagementService.ReadIdentity(
                IdentitySearchFactor.Identifier,
                projectValidUsersIdentity.Descriptor.Identifier,
                MembershipQuery.Expanded, // Number of Members: Direct gives 20, Expanded gives 111, ExpandedDown gives 111, ExpandedUp gives 0.
                ReadIdentityOptions.None);

            // Expand projectValidUsersIdentity with Members into collection of identities 
            TeamFoundationIdentity[] projectUsersIdentities = identityManagementService.ReadIdentities(projectValidUsersIdentity.Members,
                MembershipQuery.Direct,
                ReadIdentityOptions.None);

            // First build map from Identifier (which is part of the Descriptor) to Identity object.
            foreach (TeamFoundationIdentity userIdentity in projectUsersIdentities)
            {
                TeamFoundationIdentity currentIdentity = userIdentity;

                if (!currentIdentity.BelongsToProject(projectInfo.Uri))
                    continue;

                TeamProjectScanner.IdToIdentity[currentIdentity.Descriptor.Identifier] = currentIdentity;
                TeamProjectScanner.DisplayNameToIdentity[currentIdentity.DisplayName] = currentIdentity;

                //Debug.WriteLine("{0}: Members = {1}, MemberOf = {2}", currentIdentity.DisplayName(), currentIdentity.Members.Length, currentIdentity.MemberOf.Length);
            }
        }

        public static void ScanPermissions(TfsTeamProjectCollection tfsTeamProjectCollection, ProjectInfo projectInfo, DirectedGraph directedGraph)
        {
            VersionControlServer vcs = tfsTeamProjectCollection.GetService<VersionControlServer>();
            TeamProject[] teamProjects = vcs.GetAllTeamProjects(true);
            TeamProject teamProject = teamProjects.ToList().FirstOrDefault(tp => tp.Name == projectInfo.Name);
            if (teamProject == null)
                return;

            ItemSecurity[] itemSecurityArray = vcs.GetPermissions(new string[] { teamProject.ServerItem }, RecursionType.Full);

            foreach (ItemSecurity itemSecurity in itemSecurityArray)
            {
                string vcsPath = itemSecurity.ServerItem;
                string vcsPathGraphId = "vcsPath_" + vcsPath;
                directedGraph.AddNode(vcsPathGraphId, vcsPath, "Version Control Folder", null, "Expanded");

                foreach (AccessEntry accessEntry in itemSecurity.Entries)
                {
                    string[] accessEntryArray = new string[4]
                    {
                        accessEntry.Allow.Length > 0 ? "Allow: " + string.Join(", ", accessEntry.Allow) + " " : string.Empty,
                        accessEntry.AllowInherited.Length > 0
                            ? "Allow Inherited: " + string.Join(", ", accessEntry.AllowInherited) + " "
                            : string.Empty,
                        accessEntry.Deny.Length > 0 ? "Deny: " + string.Join(", ", accessEntry.Deny) + " " : string.Empty,
                        accessEntry.DenyInherited.Length > 0
                            ? "Deny Inherited: " + string.Join(", ", accessEntry.DenyInherited) + " "
                            : string.Empty
                    };

                    string accessEntryDescription = string.Join(" ", accessEntryArray);
                    string identityDisplayName = accessEntry.IdentityName;
                    TeamProjectScanner.ExpandContainers(vcsPathGraphId /*accessEntryGraphId*/, identityDisplayName, accessEntryDescription, directedGraph);
                }
            }
        }

        private static void ExpandContainers(string parentIdentityNameGraphId, string identityDisplayName, string accessEntryDescription, DirectedGraph directedGraph)
        {
            TeamFoundationIdentity identity;
            TeamProjectScanner.DisplayNameToIdentity.TryGetValue(identityDisplayName, out identity);
            if (identity == null)
                return; // Skip unknown identities

            string identityNameGraphId = parentIdentityNameGraphId + "_" + identity.UniqueName;
            string identityCategory = identity.Category();
            string groupState = (identity.IsContainer) ? "Expanded" : null;
            string label = identityDisplayName + " " + accessEntryDescription;
            if (identity.IsContainer)
            {
                directedGraph.AddNode(identityNameGraphId, label, identityCategory, null, groupState, new Tuple<string, string>("MaxWidth", "300"));
            }
            else
            {
                directedGraph.AddNode(identityNameGraphId, label, identityCategory, null, groupState);
            }
            directedGraph.AddLink(parentIdentityNameGraphId, identityNameGraphId, "Contains");

            foreach (TeamFoundationIdentity childIdentity in identity.Children())
            {
                TeamProjectScanner.ExpandContainers(identityNameGraphId, childIdentity.DisplayName, string.Empty, directedGraph);
            }
        }

        /*
        ISecurityService securityService = tfsTeamProjectCollection.GetService<ISecurityService>();
        ReadOnlyCollection<SecurityNamespace> securityNamespaces = securityService.GetSecurityNamespaces();
        //SecurityNamespace projectSecurityNamespace = securityNamespaces.First(ssn => ssn.Description.Name == "Project");
        //SecurityNamespace vcsPrivilegesSecurityNamespace = securityNamespaces.First(ssn => ssn.Description.Name == "VersionControlPrivileges");
        SecurityNamespace securityNamespace = securityService.GetSecurityNamespace(SecurityConstants.RepositorySecurityNamespaceGuid);


        string securityToken = "$PROJECT:" + projectInfo.Uri;
        List<IdentityDescriptor> identityDescriptorList = TeamProjectScanner.IdToIdentity.Values.Select(identity => identity.Descriptor).ToList();
        AccessControlList access = securityNamespace.QueryAccessControlList(securityToken, identityDescriptorList, true);

        if (
            access.AccessControlEntries.Any(
                acl => ((acl.ExtendedInfo.EffectiveAllow & 128) == 128 || (acl.ExtendedInfo.InheritedAllow & 128) == 128)))
        {
            //return true;
        }
        //return false;        

        return; // Debugging
        */
        /*
                    foreach (TeamFoundationIdentity userIdentity in TeamProjectScanner.IdToIdentity.Values)
                    {
                        string uniqueName = userIdentity.UniqueName;

                        string[] userEffectiveGlobalPermissions = vcs.GetEffectiveGlobalPermissions(uniqueName);
                        if (userEffectiveGlobalPermissions.Any())
                        {
                            string userEffectiveGlobalPermissionsLabel = string.Join(", ", userEffectiveGlobalPermissions);
                            string category1 = "Effective Global Permissions";
                            categorieSet.Add(category1);
                            string childNodeId1 = userIdentity.UniqueDisplayName() + "_userEffectiveGlobalPermissions";
                            nodes.AddNode(childNodeId1,
                                userEffectiveGlobalPermissionsLabel,
                                category1,
                                null,
                                null,
                                new Tuple<string, string>("MaxWidth", "150"));
                            links.AddLink(userIdentity.UniqueDisplayName(), childNodeId1, "Contains");
                        }

                        string[] userEffectivePermissions = vcs.GetEffectivePermissions(uniqueName, teamProject.ServerItem);
                        if (userEffectivePermissions.Any())
                        {
                            string userEffectivePermissionsLabel = string.Join(", ", userEffectivePermissions);
                            string category2 = "Effective Permissions";
                            categorieSet.Add(category2);
                            string childNodeId2 = userIdentity.UniqueDisplayName() + "_userEffectivePermissions";
                            nodes.AddNode(childNodeId2,
                                userEffectivePermissionsLabel,
                                category2,
                                null,
                                null,
                                new Tuple<string, string>("MaxWidth", "150"));
                            links.AddLink(userIdentity.UniqueDisplayName(), childNodeId2, "Contains");
                        }
                    }
        */
        /*
                     //IIdentityManagementService identityManagementService = tfsTeamProjectCollection.GetService<IIdentityManagementService>();
                    //ISecurityService securityService = tfsTeamProjectCollection.GetService<ISecurityService>();
                    //TfsTeamService teamService = tfsTeamProjectCollection.GetService<TfsTeamService>();
                    ItemSecurity[] actualPermission = vcs.GetPermissions(new string[] { teamProject.ServerItem }, RecursionType.Full);

                    string teamProjectAccountName = $"[{projectInfo.Name}]\\Project Valid Users";
                    TeamFoundationIdentity teamProjectValidUsers = identityManagementService.ReadIdentity(IdentitySearchFactor.AccountName,
                        teamProjectAccountName,
                        MembershipQuery.Expanded,
                        ReadIdentityOptions.IncludeReadFromSource);

                    foreach (IdentityDescriptor memberIdentityDescriptor in teamProjectValidUsers.Members)
                    {
                        TeamFoundationIdentity memberIdentity = identityManagementService.ReadIdentity(IdentitySearchFactor.Identifier,
                            memberIdentityDescriptor.Identifier,
                            MembershipQuery.Direct,
                            ReadIdentityOptions.None);

                        string userName = memberIdentity.UniqueName;
                        string[] permissions = vcs.GetEffectivePermissions(userName, teamProject.ServerItem);
                    }

                    string[] vcsPerm = vcs.GetEffectiveGlobalPermissions("CORP\\konstantine");

                    TeamFoundationIdentity userIdentity = identityManagementService.ReadIdentity(IdentitySearchFactor.AccountName,
                        "CORP\\konstantine",
                        MembershipQuery.Expanded,
                        ReadIdentityOptions.None);

                    ReadOnlyCollection<SecurityNamespace> securityNamespaces = securityService.GetSecurityNamespaces();
                    SecurityNamespace projectSecurityNamespace = securityNamespaces.First(ssn => ssn.Description.Name == "Project");

                    string securityToken = "$PROJECT:" + teamProject.ArtifactUri;
                    AccessControlList access = projectSecurityNamespace.QueryAccessControlList(securityToken,
                        new List<IdentityDescriptor> { userIdentity.Descriptor },
                        true);

                    if (
                        access.AccessControlEntries.Any(
                            acl => ((acl.ExtendedInfo.EffectiveAllow & 128) == 128 || (acl.ExtendedInfo.InheritedAllow & 128) == 128)))
                    {
                        //return true;
                    }
                    //return false;        
        */
    }

    public static class ExtensionMethods
    {
        public static string UniqueDisplayName(this TeamFoundationIdentity identity)
        {
            return identity.Descriptor.IdentityType == ExtensionMethods.IDENTITY_TYPE_TFS ? identity.DisplayName : identity.UniqueName;
        }

        private static string TeamProjectUri(this TeamFoundationIdentity identity, string projectUri)
        {
            string teamProject = null;
            if (identity.Descriptor.IdentityType == ExtensionMethods.IDENTITY_TYPE_TFS)
            { 
                teamProject = identity.GetProperty("Domain").ToString();
            }
            return teamProject;
        }

        public static string Category(this TeamFoundationIdentity identity)
        {
            string category = "Domain";
            if (identity.Descriptor.IdentityType == ExtensionMethods.IDENTITY_TYPE_TFS)
            {
                category = "TFS";
            }

            if (identity.IsContainer)
            {
                category += " Group";
            }
            else
            {
                category += " Account";
            }
            return category;
        }

        public static bool BelongsToProject(this TeamFoundationIdentity identity, string projectUri)
        {
            string teamProjectUri = identity.TeamProjectUri(projectUri);
            return teamProjectUri == null || teamProjectUri == projectUri;
        }
        public static IEnumerable<TeamFoundationIdentity> Children(this TeamFoundationIdentity identity)
        {
            foreach (IdentityDescriptor identityDescriptor in identity.Members)
            {
                TeamFoundationIdentity child;
                TeamProjectScanner.IdToIdentity.TryGetValue(identityDescriptor.Identifier, out child);
                if (child != null)
                    yield return child;
            }
        }
        public static IEnumerable<TeamFoundationIdentity> Parents(this TeamFoundationIdentity identity)
        {
            foreach (IdentityDescriptor identityDescriptor in identity.MemberOf)
            {
                TeamFoundationIdentity parent;
                TeamProjectScanner.IdToIdentity.TryGetValue(identityDescriptor.Identifier, out parent);
                if (parent != null)
                    yield return parent;
            }
        }

        private const string IDENTITY_TYPE_TFS = "Microsoft.TeamFoundation.Identity";
        private const string IDENTITY_TYPE_WINDOWS = "System.Security.Principal.WindowsIdentity";
    }
}
