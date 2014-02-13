using System;
using System.Reflection;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;

namespace Composable.CQRS.Testing.Diagnostics
{  
    public class DocumentDbSessionInspector
    {
        public readonly IDocumentDbSession Session;

        public DocumentDbSessionInspector(IDocumentDbSession session)
        {
            Session = session;
        }

        public bool IsProxy { get { return Session is DocumentDbSessionProxy; } }


        public DocumentDbSessionInspector GetSessionIfProxy()
        {
            if (IsProxy)
            {
                return new DocumentDbSessionInspector(((DocumentDbSessionProxy)Session).Session);
            }
            if (Session is DocumentDbSession)
                return this;

            throw new DiagnosticsException("IDocumentDb was neither of type DocumentDbSessionProxy nor DocumentDbSession and Diagnostics cannot extract a DocumentDbSession");
        }

        public IDocumentDbSessionInterceptor Interceptor
        {
            get { return ((DocumentDbSession) GetSessionIfProxy().Session).Interceptor; }
        }

    }


    public static class DocumentDbSessionInspectorExtensions
    {
    }

    public class DiagnosticsException : Exception
    {
        public DiagnosticsException(string message) : base(message) { }
    }
}
