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
            get { return ((DocumentDbSession) GetSessionIfProxy().Session)._interceptor; }
        }

        public SqlServerObjectStoreInspector SqlBackingStore
        {
            get
            {
                if (Session is DocumentDbSession)
                {
                    return new SqlServerObjectStoreInspector((SqlServerObjectStore) ((DocumentDbSession) Session)._backingStore);
                }

                throw new DiagnosticsException(
                    string.Format("Session must be of type {0} for Diagnostics to be able get backingStore field",
                                  typeof (DocumentDbSession)));
            }
        }
    }

    public class SqlServerObjectStoreInspector
    {
        public readonly SqlServerObjectStore ObjectStore;

        public SqlServerObjectStoreInspector(SqlServerObjectStore objectStore)
        {
            ObjectStore = objectStore;
        }

        public SqlServerDocumentDb DocumentDb
        {
            get { return ObjectStore._store; }
        }

        public SqlServerDocumentDbConfig DocumentDbConfig
        {
            get { return ObjectStore._config; }
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
