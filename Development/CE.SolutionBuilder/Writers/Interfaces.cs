// Interfaces.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

namespace CE.SolutionBuilder.Writers
{
    public interface ISolutionWriter
    {
        /// <summary>
        /// Writes the solution to a format implemented in Write method.
        /// </summary>
        /// <param name="rootPath">The root path of where source is located for determining relative paths</param>
        /// <param name="solution">The solution to write</param>
        bool Write(string rootPath, string targetFrameworks, ISolution solution);
    }
}
