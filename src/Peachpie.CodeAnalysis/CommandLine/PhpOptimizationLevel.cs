using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Level of optimization.
    /// </summary>
    public enum PhpOptimizationLevel : int
    {
        /// <summary><see cref="OptimizationLevel.Debug"/></summary>
        Debug = 0,

        O1, O2, O3, O4, O5, O6, O7, O8, O9,

        Ox = Release,

        /// <summary><see cref="OptimizationLevel.Release"/></summary>
        Release = O9,
    }

    /// <summary>
    /// Helper methods for the <see cref="PhpOptimizationLevel"/>.
    /// </summary>
    internal static class PhpOptimizationLevelExtension
    {
        public static OptimizationLevel AsOptimizationLevel(this PhpOptimizationLevel level)
            => level != PhpOptimizationLevel.Debug ? OptimizationLevel.Release : OptimizationLevel.Debug;

        public static PhpOptimizationLevel AsPhpOptimizationLevel(this OptimizationLevel level)
            => level != OptimizationLevel.Debug ? PhpOptimizationLevel.Release : PhpOptimizationLevel.Debug;

        public static int GraphTransformationCount(this PhpOptimizationLevel level) => level.IsDebug() ? 0 : ((int)level - 1); // O2 .. O9

        public static bool IsDebug(this PhpOptimizationLevel level) => level == PhpOptimizationLevel.Debug;

        public static bool IsRelease(this PhpOptimizationLevel level) => level != PhpOptimizationLevel.Debug;
    }
}
