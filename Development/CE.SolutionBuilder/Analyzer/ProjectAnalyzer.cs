// ProjectAnalyzer.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace CE.SolutionBuilder.Analyzer
{
    public class ProjectAnalyzer
    {
        /// <summary>
        /// Analyzes the given project and creates a solution containing
        /// all the project references if source is available.
        /// </summary>
        /// <param name="filename">The project file to analyze</param>
        /// <param name="rootPath">The root path to find the source</param>
        /// <returns>A solution object containing a basic structre of the referenced projects</returns>
        public ISolution AnalyzeProject(string solutionFile, string filename, string rootPath, params string[] searchPaths)
        {
            ISolution solution = Solution.New(Path.GetFileNameWithoutExtension(solutionFile), solutionFile);

            // Find all csproj and vcxproj files in the root path.  Exclude those in the ignoreFolders list.
            // Open each, get the AssemblyName and map from AssemblyName to ProjectRootElement.
            IDictionary<string, ProjectRootElement> assemblyToProject = FindSourceProjects(searchPaths);

            // Go through the current project (filename) and look at the assemblies.  Check to see if
            // an assembly is found in the assemblyToProject map.  If it does, then add that project
            // to the list.

            // For each project found, go through its references and if the assembly's source is found
            // in the map, add the project to the list.

            List<ProjectRootElement> referencedProjects = FindReferencedProjects(filename, assemblyToProject);

            ProjectRootElement projectAnalyzed = ProjectRootElement.Open(filename, ProjectCollection, true);
            referencedProjects.Insert(0, projectAnalyzed);

            IDictionary<string, ProjectRootElement> uniqueProjectRferences = new Dictionary<string, ProjectRootElement>(referencedProjects.Count);
            foreach (var referencedProject in referencedProjects)
            {
                if (!uniqueProjectRferences.ContainsKey(referencedProject.FullPath))
                    uniqueProjectRferences.Add(referencedProject.FullPath, referencedProject);
            }

            foreach (var item in uniqueProjectRferences)
            {
                string targetFramework = GetTargetFramework(item.Key);
                string ext = Path.GetExtension(item.Value.FullPath);

                solution.AddProject(Path.GetFileName(item.Key), item.Value.FullPath,
                    !string.IsNullOrEmpty(targetFramework) || ext == ".vcxproj" ||
                    item.Value.FullPath.Contains(".Test."));
            }

            return solution;
        }

        #region Private Methods
        private List<ProjectRootElement> FindReferencedProjects(string filename, IDictionary<string, ProjectRootElement> assemblyToProject)
        {
            var currentDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(filename);

                List<ProjectRootElement> referencedProjects = new List<ProjectRootElement>();

                ProjectRootElement project = ProjectRootElement.Open(filename, ProjectCollection, true);
                foreach (var itemGroup in project.ItemGroups)
                {
                    foreach (var item in itemGroup.Items)
                    {
                        if (item.ElementName == REFERENCEELEMENT)
                        {
                            string hintPath = string.Empty;
                            foreach (var meta in item.Metadata)
                            {
                                if (meta.ElementName == "HintPath")
                                {
                                    hintPath = meta.Value;
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(hintPath))
                            {
                                var assemblyName = Path.GetFileNameWithoutExtension(hintPath);
                                referencedProjects.AddRange(GetProjectFromAssemblyName(assemblyToProject, assemblyName));
                            }
                        }
                        else if (item.ElementName == PROJECTREFERENCEELEMENT)
                        {
                            var referencedProjectName = item.Include;
                            if (referencedProjectName.EndsWith("$(TargetFramework).vcxproj"))
                                referencedProjectName = referencedProjectName.Replace("$(TargetFramework)", $"{NET472}");

                            ProjectRootElement projectReference = ProjectRootElement.Open(referencedProjectName, ProjectCollection, true);
                            var assemblyName = GetAssemblyName(projectReference);
                            referencedProjects.AddRange(GetProjectFromAssemblyName(assemblyToProject, assemblyName));

                            if (referencedProjectName.Contains($"{NET472}"))
                                referencedProjectName = item.Include.Replace("$(TargetFramework)", $"{NET6}");
                            projectReference = ProjectRootElement.Open(referencedProjectName, ProjectCollection, true);
                            assemblyName = GetAssemblyName(projectReference);
                            referencedProjects.AddRange(GetProjectFromAssemblyName(assemblyToProject, assemblyName));
                        }
                    }
                }

                List<ProjectRootElement> projects = new List<ProjectRootElement>();
                foreach (var referencedProject in referencedProjects)
                {
                    projects.Add(referencedProject);
                    projects.AddRange(FindReferencedProjects(referencedProject.FullPath, assemblyToProject));
                }

                return projects;
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }

        private static ProjectRootElement[] GetProjectFromAssemblyName(IDictionary<string, ProjectRootElement> assemblyToProject, string assemblyName)
        {
            if (assemblyToProject.TryGetValue(assemblyName, out var referencedProject))
            {
                // The assembly name was mapped to a project.
                return new ProjectRootElement[] { referencedProject };
            }
            else
            {
                // The assembly name, as-is, was not found.  Check to see if this is a
                // C++ assembly that requires either net472 or net6.0-window in the
                // psuedo assemblyName.
                assemblyName = $"{assemblyName}.{NET472}";
                if (assemblyToProject.TryGetValue(assemblyName, out var cppReferencedProject))
                {
                    List<ProjectRootElement> projects = new List<ProjectRootElement>();
                    // The C++ project was found.  Also need to include the net6.0-windows project as well.
                    projects.Add(cppReferencedProject);

                    assemblyName = assemblyName.Replace($"{NET472}", $"{NET6}");
                    if (assemblyToProject.TryGetValue(assemblyName, out var cppReferencedProjectNET6))
                    {
                        projects.Add(cppReferencedProjectNET6);
                    }

                    return projects.ToArray();
                }
            }

            return new ProjectRootElement[] { };
        }

        private IDictionary<string, ProjectRootElement> FindSourceProjects(string[] searchPaths)
        {
            IDictionary<string, ProjectRootElement> assemblyToProject = new Dictionary<string, ProjectRootElement>();

            foreach (string folder in searchPaths)
            {
                var projects = Directory.GetFiles(folder, "*.csproj", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(folder, "*.vcxproj", SearchOption.AllDirectories));
                foreach (var projectFile in projects)
                {
                    if (projectFile.Contains(@"\Bentley."))
                        continue;
                    if (projectFile.Contains("Haestad.Arx"))
                        continue;
                    if (projectFile.Contains("Shanghai"))
                        continue;

                    ProjectRootElement project = null;
                    try { project = ProjectRootElement.Open(projectFile, ProjectCollection, true); }
                    catch { }
                    if (project == null)
                        continue;

                    var assemblyName = GetAssemblyName(project);
                    if (assemblyToProject.ContainsKey(assemblyName))
                    {
                        continue;
                    }
                    assemblyToProject.Add(assemblyName, project);
                }
            }

            return assemblyToProject;
        }
        /// <summary>
        /// Gets the assembly name for the given project file
        /// </summary>
        /// <param name="filename">The full path and filename of the project</param>
        /// <returns>
        /// The output assembly name, sans extension, and including the
        /// target framework if a C++ project
        /// </returns>
        /// <exception cref="ApplicationException">Thrown if the extension is not recognized</exception>
        private string GetAssemblyName(ProjectRootElement project)
        {
            string filename = project.FullPath;
            string ext = Path.GetExtension(filename);

            if (ext == CSPROJ)
            {
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == ASSEMBLYNAME)
                            return property.Value;
                    }
                }

                // AssemblyName is not specifically stated in project file.
                // Return the name of the project without the extension.
                return Path.GetFileNameWithoutExtension(filename);
            }
            else if (ext == VCXPROJ)
            {
                foreach (var propertyGroup in project.PropertyGroups)
                {
                    if (propertyGroup.Condition.Contains("Debug|"))
                        continue;

                    foreach (var property in propertyGroup.Properties)
                    {
                        if (property.ElementName == TARGETNAME)
                        {
                            string targetName = property.Value;
                            if (targetName.Contains(VAR_PROJECTNAME))
                            {
                                targetName = targetName.Replace(VAR_PROJECTNAME,
                                    Path.GetFileNameWithoutExtension(filename));
                            }
                            else if (targetName == VAR_TARGETNAME)
                            {
                                targetName = targetName.Replace(VAR_TARGETNAME,
                                    Path.GetFileNameWithoutExtension(filename));
                            }
                            string targetFramework = GetTargetFramework(filename);
                            if (!string.IsNullOrEmpty(targetFramework))
                                targetName = $"{targetName}.{targetFramework}";
                            return targetName;
                        }
                    }
                }

                return Path.GetFileNameWithoutExtension(filename);
            }

            throw new ApplicationException($"Unknown extension: {ext}");
        }
        private string GetTargetFramework(string filename)
        {
            if (filename.Contains(NET6, StringComparison.OrdinalIgnoreCase))
                return NET6;
            if (filename.Contains(NET472, StringComparison.OrdinalIgnoreCase))
                return NET472;

            return string.Empty;
        }
        #endregion

        #region Private Properties
        private ProjectCollection ProjectCollection { get; } = new ProjectCollection(ToolsetDefinitionLocations.Default);
        #endregion

        #region Protected Constants
        protected const string CSPROJ = ".csproj";
        protected const string VCXPROJ = ".vcxproj";
        protected const string NET6 = "net6.0-windows";
        protected const string NET472 = "net472";
        protected const string ASSEMBLYNAME = "AssemblyName";
        protected const string TARGETNAME = "TargetName";
        protected const string VAR_PROJECTNAME = "$(ProjectName)";
        protected const string VAR_TARGETNAME = "$(TargetName)";
        protected const string REFERENCEELEMENT = "Reference";
        protected const string PROJECTREFERENCEELEMENT = "ProjectReference";
        #endregion
    }
}
