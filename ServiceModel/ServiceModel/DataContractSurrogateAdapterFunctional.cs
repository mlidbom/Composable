#region usings

using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Reflection;

#endregion

namespace Composable.System.ServiceModel
{
    public class DataContractSurrogateAdapterFunctional : DataContractSurrogateAdapter
    {
        public DataContractSurrogateAdapterFunctional()
        {
            GetCustomDataToExportFunc = base.GetCustomDataToExport;
            GetCustomDataToExportFunc2 = base.GetCustomDataToExport;
            GetDataContractTypeFunc = base.GetDataContractType;
            GetDeserializedObjectFunc = base.GetDeserializedObject;
            GetKnownCustomDataTypesFunc = base.GetKnownCustomDataTypes;
            GetObjectToSerializeFunc = base.GetObjectToSerialize;
            GetReferencedTypeOnImportFunc = base.GetReferencedTypeOnImport;
            ProcessImportedTypeFunc = base.ProcessImportedType;
        }

        public Func<Type, Type> GetDataContractTypeFunc { get; set; }
        public Func<MemberInfo, Type, object> GetCustomDataToExportFunc { get; set; }
        public Func<Type, Type, object> GetCustomDataToExportFunc2 { get; set; }
        public Func<object, Type, object> GetDeserializedObjectFunc { get; set; }
        public Action<Collection<Type>> GetKnownCustomDataTypesFunc { get; set; }
        public Func<object, Type, object> GetObjectToSerializeFunc { get; set; }
        public Func<string, string, object, Type> GetReferencedTypeOnImportFunc { get; set; }
        public Func<CodeTypeDeclaration, CodeCompileUnit, CodeTypeDeclaration> ProcessImportedTypeFunc { get; set; }


        public override Type GetDataContractType(Type requestedType)
        {
            return GetDataContractTypeFunc(requestedType);
        }

        public override object GetObjectToSerialize(object obj, Type targetType)
        {
            return GetObjectToSerializeFunc(obj, targetType);
        }

        public override object GetDeserializedObject(object obj, Type targetType)
        {
            return GetDeserializedObjectFunc(obj, targetType);
        }

        public override object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
        {
            return GetCustomDataToExportFunc(memberInfo, dataContractType);
        }

        public override object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return GetCustomDataToExportFunc(clrType, dataContractType);
        }

        public override void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
            GetKnownCustomDataTypesFunc(customDataTypes);
        }

        public override Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            return GetReferencedTypeOnImportFunc(typeName, typeNamespace, customData);
        }

        public override CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            return ProcessImportedTypeFunc(typeDeclaration, compileUnit);
        }
    }
}