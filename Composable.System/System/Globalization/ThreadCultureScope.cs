using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;

namespace Composable.System.Globalization
{
    public class ThreadCultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public ThreadCultureScope(string languageCode) : this(new CultureInfo(languageCode))
        {
            
        }

        private ThreadCultureScope(CultureInfo cultureInfo)
        {
            Contract.Requires(cultureInfo != null);

            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
