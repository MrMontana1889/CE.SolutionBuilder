// Configuration.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

namespace CE.SolutionBuilder
{
    internal class Configuration : IConfiguration
    {
        #region Constructor
        internal Configuration(string config, string platform)
        {
            Config = config;
            Platform = platform;
        }
        #endregion

        #region Public Properties
        public string Config { get; }
        public string Platform { get; }
        #endregion
    }
}
