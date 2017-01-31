using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;

namespace Composable.System.Globalization
{
    ///<summary>
    /// Changes the current thread's CurrentCulture and CurrentUICulture when constructed and returns it to the original value when disposed.
    /// </summary>
    public class ThreadCultureScope : IDisposable //todo: tests
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        ///<summary>Changes the current thread's CurrentCulture and CurrentUICulture to a culture created from the supplied culture name. </summary>
        public ThreadCultureScope(string cultureName) : this(new CultureInfo(cultureName))
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(cultureName));
        }

        ///<summary>Changes the current thread's CurrentCulture and CurrentUICulture to supplied CultureInfo instance.. </summary>
        public ThreadCultureScope(CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);

            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        ///<summary>Restores the current thread's CurrentCulture and CurrentUICulture to their original values.</summary>
        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
