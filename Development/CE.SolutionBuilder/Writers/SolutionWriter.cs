// SolutionWriter.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using Microsoft.Build.Construction;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CE.SolutionBuilder.Writers
{
    public class SolutionWriter : ISolutionWriter
    {
        #region Static Properties
        public static ISolutionWriter Default => new SolutionWriter();
        #endregion

        #region Public Methods
        public bool Write(string rootPath, string targetFrameworks, ISolution solution)
        {
            RootPath = rootPath;
            TargetFrameworks = targetFrameworks;

            string solutionDirectory = Path.GetDirectoryName(solution.FullPath);
            if (!Directory.Exists(solutionDirectory))
                Directory.CreateDirectory(solutionDirectory);

            using (FileStream fileStream = File.Open(solution.FullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    WriteHeader(writer);
                    WriteProjects(writer, solution);
                    WriteGlobalSection(writer, solution);
                }
            }

            return true;
        }
        #endregion

        #region Protected Methods
        protected virtual void WriteProject(StreamWriter writer, IProject project, ISolution solution)
        {
            var solutionAsolutePath = Path.GetDirectoryName(solution.FullPath).ToAbsoluteDirectoryPath();
            Environment.CurrentDirectory = $"{solutionAsolutePath}";

            if (Path.GetExtension(project.ProjectFullPath) == VCXPROJ &&
                !TargetFrameworks.Contains(GetTargetFramework(project.ProjectFullPath)))
                return;

            var componentAsolutePath = Path.GetDirectoryName(project.ProjectFullPath).ToAbsoluteDirectoryPath();
            var componentRelativePath = componentAsolutePath.GetRelativePathFrom(solutionAsolutePath);
            var relativeProjectFilename = Path.Combine($"{componentRelativePath}", project.ProjectFile);

            var guid = GetProjectGuidFrom(project.ProjectFullPath);
            if (guid == Guid.Empty)
                guid = project.Guid;
            if (project.Guid != guid)
                project.Guid = guid;
            if (Path.GetExtension(project.ProjectFile) == ".csproj")
                writer.WriteLine($"Project(\"{cpsCsProjectGuid}\") = \"{Path.GetFileNameWithoutExtension(project.ProjectName)}\", \"{relativeProjectFilename}\", \"{{{guid.ToString().ToUpperInvariant()}}}\"");
            else if (Path.GetExtension(project.ProjectFile) == ".vcxproj")
                writer.WriteLine($"Project(\"{vcProjectGuid}\") = \"{Path.GetFileNameWithoutExtension(project.ProjectName)}\", \"{relativeProjectFilename}\", \"{{{guid.ToString().ToUpperInvariant()}}}\"");
            writer.WriteLine("EndProject");

            PlatformProjects.Add(project);
        }
        protected string GetTargetFramework(string filename)
        {
            if (filename.Contains(NET6))
                return NET6;
            if (filename.Contains(NET472))
                return NET472;

            return string.Empty;
        }
        #endregion

        #region Protected Properties
        protected string RootPath { get; private set; }
        protected string TargetFrameworks { get; private set; }
        #endregion

        #region Private Methods
        private void WriteHeader(StreamWriter writer)
        {
            /*
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.6.33815.320
            MinimumVisualStudioVersion = 10.0.40219.1
            */
            writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            writer.WriteLine("# Visual Studio Version 17");
            writer.WriteLine("VisualStudioVersion = 17.6.33815.320");
            writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
        }
        private void WriteProjects(StreamWriter writer, ISolution solution)
        {
            if (solution.StartupProject != null)
                WriteProject(writer, solution.StartupProject, solution);

            foreach (var project in solution.Projects)
            {
                if (project == solution.StartupProject) continue;       // Skip - already written
                WriteProject(writer, project, solution);
            }

            foreach (var folder in solution.Folders)
                WriteFolders(writer, folder, solution);
        }
        private void WriteFolders(StreamWriter writer, IFolder folder, ISolution solution)
        {
            WriteFolder(writer, folder);
            WriteFolders(writer, folder.Folders, solution);

            foreach (IProject project in folder.Projects)
            {
                if (project == solution.StartupProject) continue;
                WriteProject(writer, project, solution);
            }
        }
        private void WriteFolders(StreamWriter writer, IReadOnlyList<IFolder> folders, ISolution solution)
        {
            foreach (IFolder folder in folders)
                WriteFolders(writer, folder, solution);
        }
        private void WriteFolder(StreamWriter writer, IFolder folder)
        {
            string folderGuid = $"{{{folder.Guid.ToString().ToUpperInvariant()}}}";
            string project = $"Project(\"{solutionFolderGuid}\") = \"{folder.FolderName}\", \"{folder.FolderName}\", \"{folderGuid}\"";
            writer.WriteLine(project);
            writer.WriteLine("EndProject");
        }
        private Guid GetProjectGuidFrom(string projectPath)
        {
            ProjectRootElement project = ProjectRootElement.Open(Path.GetFullPath(projectPath));
            foreach (var propertyGroup in project.PropertyGroups)
            {
                foreach (var property in propertyGroup.Properties)
                {
                    if (property.ElementName == "ProjectGuid")
                        return new Guid(property.Value);
                }
            }

            return Guid.Empty;
        }
        private void WriteGlobalSection(StreamWriter writer, ISolution solution)
        {
            writer.WriteLine("Global");

            WritePreSolutionPlatforms(writer, solution);
            WritePostSolutionPlatforms(writer, solution);

            WritePreSolution(writer);

            writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
            foreach (var folder in solution.Folders)
                WriteNestedFolder(writer, folder);
            foreach (var project in solution.Projects)
                WriteNestedProject(writer, project);
            writer.WriteLine("\tEndGlobalSection");

            WritePostSolution(writer);

            writer.WriteLine("EndGlobal");
        }

        private void WritePostSolution(StreamWriter writer)
        {
            //GlobalSection(ExtensibilityGlobals) = postSolution
            //  SolutionGuid = {F1029B2A-DB0D-464B-BFCC-F124DD4CBDD1}
            //EndGlobalSection
            writer.WriteLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
            writer.WriteLine("\t\tSolutionGuid = {" + $"{Guid.NewGuid()}" + "}");
            writer.WriteLine("\tEndGlobalSection");
        }

        private void WritePreSolution(StreamWriter writer)
        {
            //GlobalSection(SolutionProperties) = preSolution
            //  HideSolutionNode = FALSE
            //EndGlobalSection
            writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
            writer.WriteLine("\t\tHideSolutionNode = FALSE");
            writer.WriteLine("\tEndGlobalSection");
        }

        private void WriteNestedFolder(StreamWriter writer, IFolder folder)
        {
            if (folder.Guid != Guid.Empty && (folder != null && folder.Parent.Guid != Guid.Empty))
            {
                string folderGuid = "{" + $"{folder.Guid}".ToUpperInvariant() + "}";
                string parent = "{" + $"{folder.Parent.Guid}".ToUpperInvariant() + "}";
                writer.WriteLine($"\t\t{folderGuid} = {parent}");
            }

            foreach (var f in folder.Folders)
                WriteNestedFolder(writer, f);
            foreach (var p in folder.Projects)
                WriteNestedProject(writer, p);
        }

        private void WriteNestedProject(StreamWriter writer, IProject project)
        {
            if (Path.GetExtension(project.ProjectFullPath) == VCXPROJ &&
                !TargetFrameworks.Contains(GetTargetFramework(project.ProjectFullPath)))
                return;

            if (project.Guid != Guid.Empty && project.Parent.Guid != Guid.Empty)
            {
                string projectGuid = "{" + $"{project.Guid}".ToUpperInvariant() + "}";
                string folderGuid = "{" + $"{project.Parent.Guid}".ToUpperInvariant() + "}";
                writer.WriteLine($"\t\t{projectGuid} = {folderGuid}");
            }
        }

        private void WritePreSolutionPlatforms(StreamWriter writer, ISolution solution)
        {
            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            foreach (var config in solution.Configurations)
                writer.WriteLine($"\t\t{config.Config}|{config.Platform} = {config.Config}|{config.Platform}");

            writer.WriteLine("\tEndGlobalSection");
        }

        private void WritePostSolutionPlatforms(StreamWriter writer, ISolution solution)
        {
            writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (var project in PlatformProjects)
                WriteProjectConfiguration(writer, project, solution);

            writer.WriteLine("\tEndGlobalSection");
        }

        private void WriteProjectConfiguration(StreamWriter writer, IProject project, ISolution solution)
        {
            string guid = $"{{{project.Guid}}}".ToUpperInvariant();

            foreach (var config in project.Configurations)
            {
                WritePlatformConfiguration(writer, guid, config.SolutionConfiguration, config, "ActiveCfg");

                if (config.On)
                    WritePlatformConfiguration(writer, guid, config.SolutionConfiguration, config, "Build.0");
            }
        }
        private void WritePlatformConfiguration(StreamWriter writer, string guid, IConfiguration solution, IProjectConfiguration config, string suffix)
        {
            writer.WriteLine($"\t\t{guid}.{solution.Config}|{solution.Platform}.{suffix} = {config.Config}|{config.Platform}");
        }
        #endregion

        #region Private Properties
        private List<IProject> PlatformProjects { get; } = new List<IProject>();
        #endregion

        #region Private Constants
        private const string solutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
        private const string vcProjectGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        private const string cpsCsProjectGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        protected const string CSPROJ = ".csproj";
        protected const string VCXPROJ = ".vcxproj";
        protected const string NET6 = "net6.0-windows";
        protected const string NET472 = "net472";
        #endregion
    }
}
