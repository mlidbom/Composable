using System;
using System.Reflection;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;

namespace Composable.CQRS.Testing.Diagnostics
{
    public abstract class Inspector
    {
        protected TField GetFieldValue<TField, TOwner>(TOwner owner, string fieldName)
            where TField : class 
        {
            var owningType = typeof(TOwner);
            var field = owningType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw DiagnosticsException.NoSuchField(owningType, fieldName);
            var value = field.GetValue(owner);
            if (value == null)
                throw new DiagnosticsException(string.Format("The {0} field {1} did not contain a value as expected", owningType, fieldName));
            var typedValue = value as TField;
            if (typedValue == null)
                throw DiagnosticsException.WrongTypeOfInstance(typeof(TField), value.GetType(), fieldName, owningType);

            return typedValue;

        }

        protected TField GetPropertyValue<TField, TOwner>(TOwner owner, string fieldName)
            where TField : class
        {
            var owningType = typeof(TOwner);
            var field = owningType.GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw DiagnosticsException.NoSuchField(owningType, fieldName);
            var value = field.GetValue(owner, null);
            if (value == null)
                throw new DiagnosticsException(string.Format("The {0} field {1} did not contain a value as expected", owningType, fieldName));
            var typedValue = value as TField;
            if (typedValue == null)
                throw DiagnosticsException.WrongTypeOfInstance(typeof(TField), value.GetType(), fieldName, owningType);

            return typedValue;

        }
    }

    public class DocumentDbSessionInspector : Inspector
    {
        private const string DocumentDbSessionBackingStoreField = "_backingStore";
        private const string DocumentDbSessionSessionProperty = "_backingStore";

        private readonly IDocumentDbSession _documentDb;

        public DocumentDbSessionInspector(IDocumentDbSession documentDb)
        {
            _documentDb = documentDb;
        }

        public DocumentDbSession GetSessionIfProxy()
        {
            if (_documentDb is DocumentDbSessionProxy)
            {
                return GetPropertyValue<DocumentDbSession, DocumentDbSessionProxy>((DocumentDbSessionProxy)_documentDb, DocumentDbSessionSessionProperty);
            }
            if (_documentDb is DocumentDbSession)
                return _documentDb as DocumentDbSession;

            throw new DiagnosticsException("IDocumentDb was neither of type DocumentDbSessionProxy nor DocumentDbSession and Diagnostics cannot extract a DocumentDbSession");
        }

        public SqlServerObjectStore GetBackingStore()
        {
            if( _documentDb is DocumentDbSession)
            {
                return GetFieldValue<SqlServerObjectStore, DocumentDbSession>((DocumentDbSession)_documentDb, DocumentDbSessionBackingStoreField);
            }

            throw new DiagnosticsException(string.Format("Session must be of type {0} for Diagnostics to be able get backingStore field", typeof(DocumentDbSession)));
        }
    }

    public class SqlServerObjectStoreInspector : Inspector
    {
        private const string SqlServerDocumentDbStoreField = "_store";
        private const string SqlServerDocumentDbConfigField = "_config";

        private readonly SqlServerObjectStore _objectStore;

        public SqlServerObjectStoreInspector(SqlServerObjectStore objectStore)
        {
            _objectStore = objectStore;
        }

        public SqlServerDocumentDb GetSqlServerDocumentDb()
        {
            return GetFieldValue<SqlServerDocumentDb, SqlServerObjectStore>(_objectStore, SqlServerDocumentDbStoreField);
        }

        public SqlServerDocumentDbConfig GetSqlServerDocumentDbConfig()
        {
            return GetFieldValue<SqlServerDocumentDbConfig, SqlServerObjectStore>(_objectStore, SqlServerDocumentDbConfigField);
        }
    }

    public static class DocumentDbSessionInspectorExtensions
    {
    }

    public class DiagnosticsException : Exception
    {
        public DiagnosticsException() : base() { }
        public DiagnosticsException(string message) : base(message) { }
        public DiagnosticsException(string message, Exception innerException) : base(message, innerException) { }

        public static DiagnosticsException WrongTypeOfInstance(Type expectedType, Type actualType, string fieldName, Type owningType)
        {
            return new DiagnosticsException(string.Format("Diagnostics expected field/property {0} from type {1} to contain an instance of type {2} but found {3}",
                                                          fieldName, owningType.FullName, expectedType.FullName, actualType.FullName));
        }

        public static DiagnosticsException NoSessionForProxy()
        {
            return new DiagnosticsException(string.Format("The DocumentDbProxy property Session did not contain an instance"));
        }

        public static DiagnosticsException NoSuchField(Type type, string fieldName)
        {
            return new DiagnosticsException(string.Format("Diagnostics tried to get field {0} from type {1} but failed", fieldName, type.FullName));
        }

        public static DiagnosticsException NoSuchProperty(Type type, string propertyName)
        {
            return new DiagnosticsException(string.Format("Diagnostics tried to get property {0} from type {1} but failed", propertyName, type.FullName));
        }
    }
}
