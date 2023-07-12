// SolutionWriter.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NDepend.Path;

namespace CE.SolutionBuilder.Writers
{
    internal class SolutionWriter : ISolutionWriter
    {
        #region Constructor
        internal SolutionWriter()
        {

        }
        #endregion

        #region Static Properties
        public static ISolutionWriter Default => new SolutionWriter();
        #endregion

        #region Public Methods
        public void Write(ISolution solution)
        {
            using (FileStream fileStream = File.Open(solution.FullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (StreamWriter writer = new StreamWriter(fileStream, Encoding.Default))
                {
                    WriteHeader(writer);
                    WriteProjects(writer, solution);
                    WriteGlobalSection(writer, solution);
                }
            }
        }
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
            writer.WriteLine("");
            writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            writer.WriteLine("# Visual Studio Version 17");
            writer.WriteLine("VisualStudioVersion = 17.6.33815.320");
            writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
        }
        private void WriteProjects(StreamWriter writer, ISolution solution)
        {
            if (solution.StartupProject != null)
                WriteProject(writer, solution.StartupProject, solution);

            foreach (var folder in solution.Folders)
                WriteFolders(writer, folder, solution);

            foreach (var project in solution.Projects)
            {
                if (project == solution.StartupProject) continue;       // Skip - already written
                WriteProject(writer, project, solution);
            }
        }
        private void WriteFolders(StreamWriter writer, IFolder folder, ISolution solution)
        {
            WriteFolder(writer, folder);
            WriteFolders(writer, folder.Folders);

            foreach (IProject project in folder.Projects)
            {
                if (project == solution.StartupProject) continue;
                WriteProject(writer, project, solution);
            }
        }
        private void WriteFolders(StreamWriter writer, IReadOnlyList<IFolder> folders)
        {
            foreach (IFolder folder in folders)
                WriteFolder(writer, folder);
        }
        private void WriteFolder(StreamWriter writer, IFolder folder)
        {
            string folderGuid = $"{{{folder.Guid.ToString().ToUpperInvariant()}}}";
            string project = $"Project(\"{solutionFolderGuid}\") = \"{folder.FolderName}\", \"{folderGuid}\"";
            writer.WriteLine(project);
            writer.WriteLine("EndProject");
        }
        private void WriteProject(StreamWriter writer, IProject project, ISolution solution)
        {
            var solutionAsolutePath = solution.FullPath.ToAbsoluteDirectoryPath();
            Environment.CurrentDirectory = $"{solutionAsolutePath}";

            var componentAsolutePath = project.ProjectFullPath.ToAbsoluteDirectoryPath();
            var componentRelativePath = componentAsolutePath.GetRelativePathFrom(solutionAsolutePath);
            var relativeProjectFilename = Path.Combine($"{componentRelativePath}", project.ProjectFile);

            string guid = $"{{{project.Guid.ToString().ToUpperInvariant()}}}";
            if (Path.GetExtension(project.ProjectFile) == ".csproj")
                writer.WriteLine($"Project(\"{cpsCsProjectGuid}\") = \"{project.ProjectName}\", \"{relativeProjectFilename}\", \"{guid}\"");
            else if (Path.GetExtension(project.ProjectFile) == ".vcxproj")
                writer.WriteLine($"Project(\"{vcProjectGuid}\") = \"{project.ProjectName}\", \"{relativeProjectFilename}\", \"{guid}\"");
            writer.WriteLine("EndProject");
        }
        private void WriteGlobalSection(StreamWriter writer, ISolution solution)
        {
            writer.WriteLine("Global");

            WritePreSolutionPlatforms(writer, solution);
            WritePostSolutionPlatforms(writer, solution);

            writer.WriteLine("EndGlobal");
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

        }
        #endregion

        #region Private Properties
        private List<IProject> PlatformProjects { get; } = new List<IProject>();
        #endregion

        #region Private Constants
        private const string solutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
        private const string vcProjectGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        private const string cpsCsProjectGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        #endregion
    }
}
