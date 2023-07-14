// ProjectConfiguration.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

namespace CE.SolutionBuilder
{
    public class ProjectConfiguration : Configuration, IProjectConfiguration
    {
        #region Constructor
        public ProjectConfiguration(IConfiguration solutionConfiguration,
            string config, string platform, bool on = true)
            : base(config, platform)
        {
            SolutionConfiguration = solutionConfiguration;
            On = on;
        }
        #endregion

        #region Public Properties
        public bool On { get; set; } = true;
        public IConfiguration SolutionConfiguration { get; }
        #endregion
    }
}
