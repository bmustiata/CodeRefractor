using CodeRefractor.ClosureCompute;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.FrontEnd.SimpleOperations;
using CodeRefractor.FrontEnd.SimpleOperations.Casts;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.TypeInfoWriter;
using CodeRefractor.Util;
using Ninject;

namespace CodeRefractor.CodeWriter.BasicOperations
{
    public class CppCastRelatedOperations
    {
        private readonly LinkingData _linkingData;
        private static string BoxingTemplate = @"
template<class T>
struct BoxedT : public System_Object
{
	T Data;
};

template<class T>
" + TypeNamerUtils.StdSharedPtr + @"<System_Object> box_value(T value, int typeId){
	auto result = std::make_shared<BoxedT< T > >();
	result->_typeId = typeId;
	result->Data = value;
	return result;
}

template<class T>
T unbox_value(" + TypeNamerUtils.StdSharedPtr + @"<System_Object> value){
	auto resultObject = value.get();
	auto castedUnboxing = (BoxedT<T>*)resultObject;
	return castedUnboxing->Data;
}";

        [Inject]
        public CppCastRelatedOperations(LinkingData linkingData)
        {
            _linkingData = linkingData;
        }

        private void HandleIsInstance(IsInstance operation, CodeOutput bodySb, ClosureEntities crRuntime)
        {
            _linkingData.IsInstTable.GenerateInstructionCode(operation, bodySb, crRuntime);
        }

        private void HandleUnbox(Unboxing unboxing, CodeOutput bodySb, ClosureEntities closureEntities)
        {
            var typeDescription = unboxing.AssignedTo.ComputedType();
            bodySb
                .AppendFormat("{0} = unbox_value<{2}>({1});",
                    unboxing.AssignedTo.Name,
                    unboxing.Right.Name,
                    typeDescription.GetClrType(closureEntities).ToDeclaredVariableType(EscapingMode.Stack));
        }

        private void HandleBox(Boxing boxing, CodeOutput bodySb, TypeDescriptionTable typeTable, ClosureEntities closureEntities)
        {


            TypeDescription typeDescription = boxing.Right.ComputedType();
            bodySb
                .AppendFormat("{0} = box_value<{2}>({1}, {3});",
                    boxing.AssignedTo.Name,
                    boxing.Right.Name,
                    typeDescription.GetClrType(closureEntities).ToDeclaredVariableType(EscapingMode.Stack),
                    typeTable.GetTypeId(typeDescription.GetClrType(closureEntities)));
        }


        private void HandleCastClass(ClassCasting casting, CodeOutput bodySb, ClosureEntities closureEntities)
        {
            var typeDescription = casting.AssignedTo.ComputedType();
            bodySb
                .AppendFormat("{0} = std::static_pointer_cast<{2}>({1});",
                    casting.AssignedTo.Name,
                    casting.Value.Name,
                    typeDescription.GetClrType(closureEntities).ToDeclaredVariableType(EscapingMode.Stack));
        }

        public bool HandleCastRelatedOperations(TypeDescriptionTable typeTable, ClosureEntities crRuntime,
            LocalOperation operation, CodeOutput bodySb, OperationKind opKind)
        {
            switch (opKind)
            {
                case OperationKind.Box:
                    var boxing = crRuntime.FindFeature("Boxing");
                    if (!boxing.IsUsed)
                    {
                        boxing.IsUsed = true;
                        boxing.Declarations.Add(BoxingTemplate);
                    }
                    HandleBox((Boxing)operation, bodySb, typeTable, crRuntime);
                    break;

                case OperationKind.CastClass:
                    HandleCastClass((ClassCasting)operation, bodySb, crRuntime);
                    break;

                case OperationKind.Unbox:
                    HandleUnbox((Unboxing)operation, bodySb, crRuntime);
                    break;

                case OperationKind.IsInstance:
                    HandleIsInstance((IsInstance)operation, bodySb, crRuntime);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}