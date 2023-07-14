// Folder.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CE.SolutionBuilder
{
    public class Folder : IFolder
    {
        #region Constructor
        public Folder(IFolder parent, string name)
        {
            Parent = parent;
            FolderName = name;

            if (Parent != null)
                Guid = Guid.NewGuid();
            else
                Guid = Guid.Empty;
        }
        #endregion

        #region Public Methods
        public IFolder AddFolder(string name)
        {
            var folder = GetFolder(name);
            if (folder == null)
            {
                folder = new Folder(this, name);
                Folderz.Add(folder);
            }
            return folder;
        }
        public IFolder GetFolder(string name)
        {
            return Folderz.FirstOrDefault(f => f.FolderName == name);
        }

        public IProject AddProject(string projectName, string projectFullPath, bool usePlatforms = false)
        {
            var project = GetProject(projectName);
            if (project == null)
            {
                project = new Project(this, projectName, projectFullPath);
                project.AddConfiguration(new Configuration("Debug", "x64"), "Debug", "Any CPU");
                project.AddConfiguration(new Configuration("Debug", "x86"), "Debug", "Any CPU");
                project.AddConfiguration(new Configuration("Release", "x64"), "Release", "Any CPU");
                project.AddConfiguration(new Configuration("Release", "x86"), "Release", "Any CPU");

                if (usePlatforms)
                {
                    project.ResetConfigurations();

                    project.AddConfiguration(new Configuration("Debug", "x64"), "Debug", "x64");
                    project.AddConfiguration(new Configuration("Debug", "x86"), "Debug", "x86");
                    project.AddConfiguration(new Configuration("Release", "x64"), "Release", "x64");
                    project.AddConfiguration(new Configuration("Release", "x86"), "Release", "x86");
                }
                Projectz.Add(project);
            }
            return project;
        }
        public void AddProject(IProject project)
        {
            if (project == null) return;
            if (GetProject(project.ProjectName) == null)
                Projectz.Add(project);
        }
        public IProject GetProject(string projectName)
        {
            return Projectz.FirstOrDefault(p => p.ProjectName == projectName);
        }
        #endregion

        #region Public Properties
        public Guid Guid { get; set; }
        public string FolderName { get; }
        public IFolder Parent { get; }
        public IReadOnlyList<IFolder> Folders => Folderz.AsReadOnly();
        public IReadOnlyList<IProject> Projects => Projectz.AsReadOnly();
        #endregion

        #region Protected Properties
        protected List<IFolder> Folderz { get; } = new List<IFolder>();
        protected List<IProject> Projectz { get; } = new List<IProject>();
        #endregion
    }
}
