﻿// Solution.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SolutionBuilder.Writers;
using System.Collections.Generic;
using System.Linq;

namespace CE.SolutionBuilder
{
    public class Solution : Folder, ISolution
    {
        #region Constructor
        public Solution(string fullPath, string name)
            : base(null, name)
        {
            FullPath = fullPath;

            AddConfiguration("Debug", "x64");
            AddConfiguration("Debug", "x86");
            AddConfiguration("Release", "x64");
            AddConfiguration("Release", "x86");
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Creates a new solution instance given the filename and name of the solution
        /// </summary>
        /// <param name="name">The display name of the solution.  Typically the filename without the extension.</param>
        /// <param name="filename">The full path and filename of the solution</param>
        /// <returns></returns>
        public static ISolution New(string name, string filename)
        {
            return new Solution(filename, name);
        }
        #endregion

        #region Public Methods
        public IConfiguration AddConfiguration(string config, string platform)
        {
            var configuration = Configs.FirstOrDefault(c => c.Config == config && c.Platform == platform);
            if (configuration == null)
            {
                configuration = new Configuration(config, platform);
                Configs.Add(configuration);
            }
            return configuration;
        }

        public void RemoveConfiguration(string config, string platform) => Configs.RemoveAll(c => c.Config == config && c.Platform == platform);

        public bool Save(string rootPath, string targetFrameworks, ISolutionWriter writer)
        {
            return writer.Write(rootPath, targetFrameworks, this);
        }
        public void SetStartupProject(IProject project)
        {
            if (project != null)
                StartupProject = project;
        }
        #endregion

        #region Public Properties
        public string FullPath { get; }
        public IReadOnlyList<IConfiguration> Configurations => Configs.AsReadOnly();
        public IProject StartupProject { get; private set; }
        #endregion

        #region Private Properties
        private List<IConfiguration> Configs { get; } = new List<IConfiguration>();
        #endregion
    }
}
