using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Linq;

namespace Void.ServiceModel
{
    public class DataContractSurrogateLink : IDataContractSurrogate
    {
        private IDataContractSurrogate _head;
        private IDataContractSurrogate _tail;

        public static IDataContractSurrogate Chain(IEnumerable<IDataContractSurrogate> chain)
        {
            return chain.Aggregate((aggregate, current) => new DataContractSurrogateLink(current, aggregate));
        }

        public DataContractSurrogateLink(IDataContractSurrogate tail, IDataContractSurrogate head)
        {
            _tail = tail;
            _head = head;
        }

        #region chainable methods

        public virtual Type GetDataContractType(Type requestedType)
        {
            return _tail.GetDataContractType(_head.GetDataContractType(requestedType));
        }

        public virtual object GetObjectToSerialize(object obj, Type targetType)
        {
            return _tail.GetObjectToSerialize(_head.GetObjectToSerialize(obj, targetType), targetType);
        }

        public virtual object GetDeserializedObject(object obj, Type targetType)
        {
            return _tail.GetDeserializedObject(_head.GetDeserializedObject(obj, targetType), targetType);
        }

        public virtual void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
            _head.GetKnownCustomDataTypes(customDataTypes);
            _tail.GetKnownCustomDataTypes(customDataTypes);
        }

        public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            return _tail.ProcessImportedType(_head.ProcessImportedType(typeDeclaration, compileUnit), compileUnit);
        }

        #endregion

        #region mutually exlusive methods.

        public virtual object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
        {
            var headCustomDataToExport = _head.GetCustomDataToExport(memberInfo, dataContractType);
            var tailCustomDataToExport = _tail.GetCustomDataToExport(memberInfo, dataContractType);

            if (headCustomDataToExport != null && tailCustomDataToExport != null)
            {
                throw new Exception(string.Format("Both {0} and {1} surrogates responded to GetCustomDataToExport for memberInfo {2} with dataContractType {3}",
                    _head.GetType(), _tail.GetType(), memberInfo, dataContractType));
            }

            if (headCustomDataToExport != null)
            {
                return headCustomDataToExport;
            }

            return tailCustomDataToExport;
        }

        public virtual object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            var headCustomDataToExport = _head.GetCustomDataToExport(clrType, dataContractType);
            var tailCustomDataToExport = _tail.GetCustomDataToExport(clrType, dataContractType);

            if (headCustomDataToExport != null && tailCustomDataToExport != null)
            {
                throw new Exception(string.Format("Both {0} and {1} surrogates responded to GetCustomDataToExport for clrType {2} with dataContractType {3}",
                    _head.GetType(), _tail.GetType(), clrType, dataContractType));
            }

            if (headCustomDataToExport != null)
            {
                return headCustomDataToExport;
            }

            return tailCustomDataToExport;
        }

        public virtual Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            var headType = _head.GetReferencedTypeOnImport(typeName, typeNamespace, customData);
            var tailType = _tail.GetReferencedTypeOnImport(typeName, typeNamespace, customData);

            if(headType != null && tailType != null)
            {
                throw new Exception(string.Format("Both {0} and {1} surrogates responded to GetReferencedTypeOnImport for type {2}.{3}", 
                    _head.GetType(), _tail.GetType(), typeNamespace, typeName));
            }

            if(headType != null)
            {
                return headType;
            }

            return tailType;
        }

        #endregion
    }
}