// Interfaces.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SolutionBuilder.Writers;
using System;
using System.Collections.Generic;

namespace CE.SolutionBuilder
{
    public interface IGuid
    {
        /// <summary>
        /// A unique identifier
        /// </summary>
        Guid Guid { get; set; }
    }

    public interface IConfiguration
    {
        /// <summary>
        /// The solution configuration.  Typeically Debug or Release
        /// </summary>
        string Config { get; }
        /// <summary>
        /// The solution platform.  Any CPU, x86 or x64 are typical values.
        /// </summary>
        string Platform { get; }
    }

    public interface IProjectConfiguration : IConfiguration
    {
        /// <summary>
        /// Specifies if the configuration is on and
        /// should be written to the solution file.
        /// The default is true.
        /// </summary>
        /// <value>true</value>
        bool On { get; set; }
        /// <summary>
        /// The solution configuration associated with this project's configuration
        /// </summary>
        IConfiguration SolutionConfiguration { get; }
    }

    public interface IFolder : IGuid
    {
        /// <summary>
        /// The name of the folder
        /// </summary>
        string FolderName { get; }

        /// <summary>
        /// Add a new folder.
        /// </summary>
        /// <param name="name">The name of the folder.</param>
        /// <returns>
        /// If the name already exists, returns the original folder.  
        /// Otherwise a new folder with the given name.
        /// </returns>
        IFolder AddFolder(string name);

        /// <summary>
        /// Searches for a folder given the name in this folder.
        /// </summary>
        /// <param name="name">The name of the folder to find.</param>
        /// <returns>Null if the folder with the name is not found, otherwise the folder.</returns>
        IFolder GetFolder(string name);

        /// <summary>
        /// Add a project to the folder given the name and full path and filename.  The project name must be uniques
        /// </summary>
        /// <param name="projectName">The name of the project</param>
        /// <param name="projectFullPath">The full patha nd filename of the project.</param>
        /// <param name="usePlatforms">Flags whether to add default configurations with x86 and x64 platforms (true) or just Any CPU (false, default).</param>
        /// <returns>An instance of the project with the given name and location.</returns>
        IProject AddProject(string projectName, string projectFullPath, bool usePlatforms = false);
        /// <summary>
        /// Adds a project to the folder.
        /// </summary>
        /// <param name="project">The project to add.  Cannot be null.</param>
        void AddProject(IProject project);
        /// <summary>
        /// Gets the project from the folder.  Project names must be unique.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        IProject GetProject(string projectName);
        /// <summary>
        /// The parent folder or null if in the root of the solution.
        /// </summary>
        IFolder Parent { get; }

        /// <summary>
        /// The folders in this folder.
        /// </summary>
        IReadOnlyList<IFolder> Folders { get; }
        /// <summary>
        /// The projects in this folder.
        /// </summary>
        IReadOnlyList<IProject> Projects { get; }
    }

    public interface IProject : IGuid
    {
        /// <summary>
        /// The list of configurations the project is
        /// associated with and will be mapped in the solution file.
        /// </summary>
        IReadOnlyList<IProjectConfiguration> Configurations { get; }
        /// <summary>
        /// Add a new configuration
        /// </summary>
        /// <param name="config">The configuration.  Typically Release or Debug.</param>
        /// <param name="platform">The platform.  Typically Any CPU, x86 or x64.</param>
        /// <returns>
        /// A new configuration instance for this configuration.
        /// If the configuration already exists, returns that configuration instance.
        /// </returns>
        IProjectConfiguration AddConfiguration(IConfiguration solutionConfiguration, string config, string platform, bool on = true);
        /// <summary>
        /// Removes a configuration from the project.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="platform"></param>
        void RemoveConfiguration(string config, string platform);
        /// <summary>
        /// Removes all configurations from the project.
        /// </summary>
        void ResetConfigurations();

        /// <summary>
        /// The parent folder the project is located in.
        /// If null, in the root of the solution.
        /// </summary>
        IFolder Parent { get; }

        /// <summary>
        /// The name of the project without extension.
        /// </summary>
        string ProjectName { get; }
        /// <summary>
        /// The project filename without the path.
        /// </summary>
        string ProjectFile { get; }
        /// <summary>
        /// The full path and filename of the project.
        /// </summary>
        string ProjectFullPath { get; set; }
    }

    public interface ISolution : IFolder
    {
        /// <summary>
        /// The full path of the solution
        /// </summary>
        string FullPath { get; }
        /// <summary>
        /// The configurations associated with the solution.
        /// </summary>
        IReadOnlyList<IConfiguration> Configurations { get; }
        /// <summary>
        /// Adds a new configuration to the solution.
        /// </summary>
        /// <param name="config">Typically Debug or Release.</param>
        /// <param name="platform">The platform for this configuration.</param>
        /// <returns>A new or existing configuration with the given configuration and platform.</returns>
        IConfiguration AddConfiguration(string config, string platform);
        /// <summary>
        /// Removes the configuration.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="platform"></param>
        void RemoveConfiguration(string config, string platform);
        /// <summary>
        /// Saves the solution given the writer to use.
        /// </summary>
        /// <param name="writer">The writer to use.</param>
        bool Save(string rootPath, string targetFrameworks, ISolutionWriter writer);
        /// <summary>
        /// Sets the given project as the startup project if there is no
        /// .vs file present when the solution opens.
        /// </summary>
        /// <param name="project">The project to make the startup project by default.  No/op if null.</param>
        void SetStartupProject(IProject project);
        /// <summary>
        /// The default startup project if not null.
        /// </summary>
        IProject StartupProject { get; }
    }
}
