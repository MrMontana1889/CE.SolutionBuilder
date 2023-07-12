﻿// Project.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CE.SolutionBuilder
{
    internal class Project : IProject
    {
        #region Constructor
        internal Project(IFolder parent, string projectName, string projectFullPath)
        {
            Parent = parent;
            ProjectName = projectName;
            ProjectFullPath = projectFullPath;
        }
        #endregion

        #region Public Properties
        public Guid Guid { get; } = Guid.NewGuid();
        public IReadOnlyList<IProjectConfiguration> Configurations => Configs;
        public IFolder Parent { get; }
        public string ProjectName { get; }
        public string ProjectFile
        {
            get
            {
                if (!string.IsNullOrEmpty(ProjectFullPath))
                {
                    try { return Path.GetFileName(ProjectFullPath); }
                    catch { }
                }
                return null;
            }
        }
        public string ProjectFullPath { get; set; }
        #endregion

        #region Public Methods
        public IProjectConfiguration AddConfiguration(IConfiguration solutionConfiguration, string config, string platform)
        {
            Configs.Add(new ProjectConfiguration(solutionConfiguration, config, platform));
            return Configs.Last();
        }
        public void RemoveConfiguration(string config, string platform) => Configs.RemoveAll(c => c.Config == config && c.Platform == platform);
        public void ResetConfigurations()
        {
            Configs.Clear();
        }
        #endregion

        #region Private Properties
        private List<IProjectConfiguration> Configs { get; } = new List<IProjectConfiguration>();
        #endregion
    }
}
