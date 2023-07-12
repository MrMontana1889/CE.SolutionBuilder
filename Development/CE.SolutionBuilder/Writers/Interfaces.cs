// Interfaces.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

namespace CE.SolutionBuilder.Writers
{
    public interface ISolutionWriter
    {
        /// <summary>
        /// Writes the solution to a format implemented in Write method.
        /// </summary>
        /// <param name="solution">The solution to write</param>
        void Write(ISolution solution);
    }
}
