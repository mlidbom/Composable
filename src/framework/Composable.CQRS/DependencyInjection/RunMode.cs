using System;
using Composable.Testing;
using NCrunch.Framework;

namespace Composable.DependencyInjection
{
    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;

        public static readonly IRunMode Production = new RunMode(isTesting: false);

        public RunMode(bool isTesting)
        {
            _isTesting = isTesting;
        }
    }
}
