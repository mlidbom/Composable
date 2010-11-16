using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace Composable.System.ServiceModel
{
    public abstract class DataContractSurrogateAdapter : IDataContractSurrogate
    {
        public virtual Type GetDataContractType(Type requestedType)
        {
            return requestedType;
        }

        public virtual object GetObjectToSerialize(object obj, Type targetType)
        {
            return obj;
        }

        public virtual object GetDeserializedObject(object obj, Type targetType)
        {
            return obj;
        }

        public virtual object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
        {
            return null;
        }

        public virtual object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return null;
        }

        public virtual void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
        }

        public virtual Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            return null;
        }

        public virtual CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            return typeDeclaration;
        }
    }
}