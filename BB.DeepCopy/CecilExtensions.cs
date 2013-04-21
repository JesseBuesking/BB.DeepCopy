using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace BB.DeepCopy
{
    public static class CecilExtensions
    {
        public static void AddI(this Collection<Instruction> instructions, OpCode opcode,
            MethodReference methodReference)
        {
            instructions.Add(Instruction.Create(opcode, methodReference));
        }

        public static void AddI(this Collection<Instruction> instructions, OpCode opcode,
            TypeReference typeReference)
        {
            instructions.Add(Instruction.Create(opcode, typeReference));
        }

        public static void AddI(this Collection<Instruction> instructions, OpCode opcode,
            VariableDefinition variableDefinition)
        {
            instructions.Add(Instruction.Create(opcode, variableDefinition));
        }

        public static void AddI(this Collection<Instruction> instructions, OpCode opcode,
            Instruction instruction)
        {
            instructions.Add(Instruction.Create(opcode, instruction));
        }

        public static void AddI(this Collection<Instruction> instructions, OpCode opcode,
            FieldDefinition fieldDefinition)
        {
            instructions.Add(Instruction.Create(opcode, fieldDefinition));
        }

        public static void AddI(this Collection<Instruction> instructions, OpCode opcode)
        {
            instructions.Add(Instruction.Create(opcode));
        }

        public static VariableDefinition AddV(this Collection<VariableDefinition> variables,
            TypeDefinition typeDefinition)
        {
            VariableDefinition variable = new VariableDefinition(typeDefinition);
            variables.Add(variable);
            return variable;
        }

        public static VariableDefinition AddV(this Collection<VariableDefinition> variables,
            TypeReference typeReference)
        {
            VariableDefinition variable = new VariableDefinition(typeReference);
            variables.Add(variable);
            return variable;
        }

        public static bool IsPrimitiveObject(this TypeReference typeReference)
        {
            return typeReference.IsPrimitive ||
                new[] {"Object", "Random", "String", "Type"}.Contains(typeReference.Name);
        }

        public static MethodReference GetEmptyConstructor(this TypeDefinition type,
            ModuleDefinition moduleDefinition)
        {
            var typeEmptyConstructor = type.GetConstructors()
                .FirstOrDefault(c => (null == c.Parameters) || (0 >= c.Parameters.Count));

            return null != typeEmptyConstructor ? moduleDefinition.Import(typeEmptyConstructor) : null;
        }

        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
//            if (self.GenericParameters.Count != arguments.Length)
//                throw new ArgumentException();

            var instance = new GenericInstanceType(self);
            instance.GenericArguments.Clear();
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = self.DeclaringType.MakeGenericType(arguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParam in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));

            return reference;
        }

        public static bool TryGetMethodReference(this ModuleDefinition moduleDefinition, Type type,
            string methodName, out MethodReference methodReference)
        {
            try
            {
                MethodInfo methodInfo = type.GetMethod(methodName);
                methodReference = moduleDefinition.Import(methodInfo);
                return null != methodReference;
            }
            catch (Exception ex)
            {
                methodReference = null;
                return false;
            }
        }

        public static IEnumerable<TypeDefinition> TypesWithInterface(this ModuleDefinition moduleDefinition,
            string interfaceName)
        {
            return moduleDefinition.Types.Where(type => type.HasInterface(interfaceName));
        }

        public static IEnumerable<MethodDefinition> AbstractMethods(this TypeDefinition type)
        {
            return type.Methods.Where(x => x.IsAbstract);
        }

        public static string GetWithinLTGT(string name)
        {
            int start = name.IndexOf('<') + 1;
            int end = name.IndexOf('>', start) - 1;
            return name.Substring(start, end);
        }

        public static IEnumerable<MethodDefinition> MethodsWithBody(this TypeDefinition type)
        {
            return type.Methods.Where(x => x.Body != null);
        }

        public static bool TryGetMethod(this TypeDefinition type, string methodName,
            out MethodDefinition methodDefinition)
        {
            try
            {
                methodDefinition = null;

                if (null == type)
                    throw new ArgumentNullException("type");

                if (null == type.Methods)
                    return false;

                var methodDefinitions = type.Methods.Where(x => methodName == x.Name).ToList();
                if (1 == methodDefinitions.Count)
                    methodDefinition = methodDefinitions[0];
                else
                {
                    methodDefinition = methodDefinitions
                        .SingleOrDefault(x =>
                            {
                                var typeDefinition = x.ReturnType as TypeDefinition;
                                return typeDefinition != null && !(typeDefinition.IsInterface || typeDefinition.IsAbstract);
                            });
                }
                
                return null != methodDefinition;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Type {0} already has a DeepCopy method. Aborting.\n{1}", type, ex));
            }
        }

        public static IEnumerable<PropertyDefinition> AbstractProperties(this TypeDefinition type)
        {
            return
                type.Properties.Where(x =>
                    (x.GetMethod != null && x.GetMethod.IsAbstract) ||
                        (x.SetMethod != null && x.SetMethod.IsAbstract));
        }

        public static IEnumerable<PropertyDefinition> ConcreteProperties(this TypeDefinition type)
        {
            return
                type.Properties.Where(x =>
                    (x.GetMethod == null || !x.GetMethod.IsAbstract) &&
                        (x.SetMethod == null || !x.SetMethod.IsAbstract));
        }

        public static bool HasInterface(this TypeDefinition type, string interfaceName)
        {
            return type.Interfaces.Any(i => i.FullName.Contains(interfaceName));
        }

        public static bool TryGetAttribute(this ICustomAttributeProvider value, string attributeName,
            out CustomAttribute result)
        {
            result = value.CustomAttributes.SingleOrDefault(a => attributeName == a.AttributeType.Name);
            return null != result;
        }

        public static CustomAttribute GetNullGuardAttribute(this ICustomAttributeProvider value)
        {
            return value.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "NullGuardAttribute");
        }

        public static bool IsProperty(this MethodDefinition method)
        {
            return method.IsSpecialName && (method.Name.StartsWith("set_") || method.Name.StartsWith("get_"));
        }

        public static bool AllowsNull(this ICustomAttributeProvider value)
        {
            return
                value.CustomAttributes.Any(
                    a => a.AttributeType.Name == "AllowNullAttribute" || a.AttributeType.Name == "CanBeNullAttribute");
        }

        public static bool ContainsAllowNullAttribute(this ICustomAttributeProvider definition)
        {
            var customAttributes = definition.CustomAttributes;

            return customAttributes.Any(x => x.AttributeType.Name == "AllowNullAttribute");
        }

        public static void RemoveAllowNullAttribute(this ICustomAttributeProvider definition)
        {
            var customAttributes = definition.CustomAttributes;

            var attribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == "AllowNullAttribute");

            if (attribute != null)
            {
                customAttributes.Remove(attribute);
            }
        }

        public static void RemoveNullGuardAttribute(this ICustomAttributeProvider definition)
        {
            var customAttributes = definition.CustomAttributes;

            var attribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullGuardAttribute");

            if (attribute != null)
            {
                customAttributes.Remove(attribute);
            }
        }

        public static bool MayNotBeNull(this ParameterDefinition arg)
        {
            return !arg.AllowsNull() && !arg.IsOptional && arg.ParameterType.IsRefType() && !arg.IsOut;
        }

        public static bool IsRefType(this TypeReference arg)
        {
            if (arg.IsValueType)
            {
                return false;
            }
            var byReferenceType = arg as ByReferenceType;
            if (byReferenceType != null && byReferenceType.ElementType.IsValueType)
            {
                return false;
            }

            var pointerType = arg as PointerType;
            if (pointerType != null && pointerType.ElementType.IsValueType)
            {
                return false;
            }

            return true;
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider value)
        {
            return value.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");
        }

        public static bool IsAsyncStateMachine(this ICustomAttributeProvider value)
        {
            return value.CustomAttributes.Any(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
        }

        public static bool IsIteratorStateMachine(this ICustomAttributeProvider value)
        {
            // Only works on VB not C# but it's something.
            return value.CustomAttributes.Any(a => a.AttributeType.Name == "IteratorStateMachineAttribute");
        }

        public static bool IsIAsyncStateMachine(this TypeDefinition typeDefinition)
        {
            return typeDefinition.Interfaces.Any(x => x.Name == "IAsyncStateMachine");
        }

        public static bool IsAbstractClass(this TypeDefinition typeDefinition)
        {
            return
                null != typeDefinition &&
                !typeDefinition.IsInterface &&
                typeDefinition.IsAnsiClass &&
                typeDefinition.IsAbstract &&
                typeDefinition.IsBeforeFieldInit;
        }
    }
}