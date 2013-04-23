using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace BB.DeepCopy
{
    public class ModuleWeaver
    {
        private const string _deepCopyAttributeName = "DeepCopyMethodAttribute";

        private string _deepCopyMethodName = "";

        private static readonly string[] _ignorableInterfaces = new[]
            {
                "IEnumerable`1", "IEnumerable",
                "IList`1", "IList",
                "IComparable`1", "IComparable"
            };

        // Will log an informational message to MSBuild
        public Action<string> LogInfo
        {
            get;
            set;
        }

        // An instance of Mono.Cecil.ModuleDefinition for processing
        public ModuleDefinition ModuleDefinition
        {
            get;
            set;
        }

        // Init logging delegates to make testing easier
        public ModuleWeaver()
        {
            this.LogInfo = m => System.Diagnostics.Trace.WriteLine(m);
        }

        public void Execute()
        {
            this.Initialize();

            var baseSet = this.ModuleDefinition.Types
                .Where(x => !x.IsEnum)
                .Where(x => null != x)
                .ToList();

            if (0 >= baseSet.Count)
                return;

            this.AddMethodToInterfaces(baseSet);

            this.AddMethodToAbstracts(baseSet);

            var allTypes = baseSet
                .Where(x => !x.IsInterface)
                .Where(x => x.HasFields || x.HasProperties)
                .Where(x => !(!x.IsInterface && x.IsAbstract))
                .ToList();

            this.AddMethodShellToAllTypes(allTypes);

            this.FillMethodBodies(allTypes);

            this.ChangeExtensionMethodCallsToObjectCalls();

            this.Finalize();
        }

        private void Initialize()
        {
//            var assemblyName = this.ModuleDefinition.FullyQualifiedName;
//            var readerParameters = new ReaderParameters {ReadSymbols = true};
//            var writerParameters = new WriterParameters {WriteSymbols = true};
//            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyName, readerParameters);
//            if (1 < assemblyDefinition.Modules.Count)
//                throw new Exception("haven't handled multiple modules");
//
//            this.ModuleDefinition = assemblyDefinition.Modules[0];

            this.UpdateChosenDeepCloneMethod();
        }

        private void AddMethodToInterfaces(List<TypeDefinition> baseSet)
        {
            const MethodAttributes methodAttributes =
                MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.Abstract;

            var allInterfaces = baseSet
                .Where(x => x.IsInterface && x.IsAnsiClass && x.IsAbstract);

            // Add to interfaces first.
            foreach (var type in allInterfaces)
            {
                try
                {
                    var deepCopyMethod = new MethodDefinition(this._deepCopyMethodName, methodAttributes, type);
                    type.Methods.Add(deepCopyMethod);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format("Error adding {0} to interface {1}.", this._deepCopyMethodName, type), ex);
                }
            }
        }

        private void AddMethodToAbstracts(List<TypeDefinition> baseSet)
        {
            const MethodAttributes methodAttributes =
                MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot |
                    MethodAttributes.Abstract |
                    MethodAttributes.Virtual;

            var allAbstracts = baseSet
                .Where(x => x.IsAbstractClass() && !x.IsSealed);

            foreach (var type in allAbstracts)
            {
                try
                {
                    if (!type.HasInterfaces)
                        continue;

                    // Add the deep copy method to any interface that is valid.
                    foreach (var inter in type.Interfaces)
                    {
                        if (_ignorableInterfaces.Contains(inter.Name))
                            continue;

                        try
                        {
                            MethodDefinition interCopy;
                            if (!(inter as TypeDefinition).TryGetMethod(this._deepCopyMethodName, out interCopy))
                            {
                                throw new Exception(
                                    String.Format("Interface {0} is missing the {1} method.",
                                        inter, this._deepCopyMethodName));
                            }

                            var abstractDeepCopy = new MethodDefinition(
                                this._deepCopyMethodName, methodAttributes, interCopy.ReturnType);

                            type.Methods.Add(abstractDeepCopy);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(
                                String.Format("Error adding {0} to interface type {1}.",
                                    this._deepCopyMethodName, type), ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format("Error adding {0} to abstract class {1}.",
                            this._deepCopyMethodName, type), ex);
                }
            }
        }

        private void AddMethodShellToAllTypes(List<TypeDefinition> allTypes)
        {
            foreach (var type in allTypes)
            {
                try
                {
                    MethodAttributes methodAttributes =
                        MethodAttributes.Public |
                            MethodAttributes.HideBySig;

                    // Get the base type definition.
                    var baseDefinition = ModuleWeaver.GetBaseDefinition(type);

                    // Is the base type an abstract class?
                    var baseIsAbstractClass = baseDefinition.IsAbstractClass();

                    // If the base type is abstract, let's add the override modifier (if we need to).
                    this.HandlePotentialAbstractOverride(
                        baseIsAbstractClass, baseDefinition, ref methodAttributes);

                    // Create the deep copy method.
                    var deepCopyMethod = new MethodDefinition(this._deepCopyMethodName, methodAttributes, type);

                    // Update the return type to match the one on the base type override (if we need to).
                    this.HandleReturnType(baseIsAbstractClass, baseDefinition, deepCopyMethod);

                    // Add the new deep copy method to the object (finally!).
                    type.Methods.Add(deepCopyMethod);

                    // Add any deep copy methods that are required by intefaces being applied to 'type'.
                    this.AddDeepCopyMethodForInterfaces(type, deepCopyMethod);

//                    if (baseIsAbstractClass)
//                    {
//                        try
//                        {
////                            const MethodAttributes interAttributes =
////                                MethodAttributes.Private |
////                                MethodAttributes.Final |
////                                MethodAttributes.HideBySig |
////                                MethodAttributes.NewSlot |
////                                MethodAttributes.Virtual;
//                            const MethodAttributes interAttributes =
//                                MethodAttributes.Public |
//                                MethodAttributes.HideBySig |
//                                MethodAttributes.Virtual;
//
//                            MethodDefinition interCopy;
//                            if (!baseDefinition.TryGetMethod(this._deepCopyMethodName, out interCopy))
//                            {
//                                // No DeepCopy method to override.
//                                continue;
//                            }
//
//                            var interDeepCopy = new MethodDefinition(
//                                this._deepCopyMethodName, interAttributes, interCopy.ReturnType);
////                            interDeepCopy.Overrides.Add(interCopy);
//
//                            interDeepCopy.Body.Instructions.AddI(OpCodes.Ldarg_0);
//                            interDeepCopy.Body.Instructions.AddI(OpCodes.Call, interDeepCopy);
//                            interDeepCopy.Body.Instructions.AddI(OpCodes.Ret);
//                            type.Methods.Add(interDeepCopy);
//
//                        }
//                        catch (Exception ex)
//                        {
//                            System.Diagnostics.Debugger.Break();
//                            throw new Exception(String.Format("Error adding {0} to interface type {1}.\n{2}",
//                                this._deepCopyMethodName, type, ex), ex);
//                        }
//                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debugger.Break();
                    throw new Exception(String.Format("Error adding {0} to type {1}.\n{2}",
                        this._deepCopyMethodName, type, ex), ex);
                }
            }
        }

        private void HandleReturnType(bool baseIsAbstractClass, TypeDefinition baseDefinition,
            MethodDefinition deepCopyMethod)
        {
            // Use the return type defined on the base method we're overriding (if applicable).
            if (!baseIsAbstractClass)
                return;

//            if (baseDefinition.TryGetMethod(this._deepCopyMethodName, out interCopy))
//            {
//                if (interCopy.IsVirtual)
//                    deepCopyMethod.Overrides.Add(interCopy);
//            }

            // Update the return type to match the one that we're supposed to override.
            MethodDefinition baseTypeDeepCopyMethod;
            if (baseDefinition.TryGetMethod(this._deepCopyMethodName, out baseTypeDeepCopyMethod))
                deepCopyMethod.ReturnType = baseTypeDeepCopyMethod.ReturnType;
        }

        private void HandlePotentialAbstractOverride(bool baseIsAbstractClass, TypeDefinition baseDefinition,
            ref MethodAttributes methodAttributes)
        {
            if (!baseIsAbstractClass)
                return;

            // Get the deep copy method on the base type.
            MethodDefinition baseTypeDeepCopyMethod;
            if (!baseDefinition.TryGetMethod(this._deepCopyMethodName, out baseTypeDeepCopyMethod))
                return;

            // Is the deep copy method on the base type marked as 'override'?
            if (baseTypeDeepCopyMethod.IsVirtual)
                methodAttributes |= MethodAttributes.Virtual;
        }

        private void AddDeepCopyMethodForInterfaces(TypeDefinition type, MethodDefinition deepCopyMethod)
        {
            // Attributes for Interface's deep copy (if we have an interface being applied directly).
            const MethodAttributes interAttributes = MethodAttributes.Private |
                MethodAttributes.Final |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual;

            if (!type.HasInterfaces)
                return;

            foreach (var inter in type.Interfaces)
            {
                if (_ignorableInterfaces.Contains(inter.Name))
                    continue;

                var interfaceDefinition = inter as TypeDefinition;

                try
                {
                    // Try getting the deep copy method on the interface (it should be there by now).
                    MethodDefinition interCopy;
                    if (!interfaceDefinition.TryGetMethod(this._deepCopyMethodName, out interCopy))
                    {
                        throw new Exception(
                            String.Format("Interface {0} is missing the {1} method.",
                                inter, this._deepCopyMethodName));
                    }

                    // Let's create the method that will be used on 'type' to support the
                    // interface's deep copy method.
                    var interDeepCopy = new MethodDefinition(
                        this._deepCopyMethodName, interAttributes, interCopy.ReturnType);

                    // Mark it as overriding.
                    interDeepCopy.Overrides.Add(interCopy);

                    // Now we just call the regular deep copy method for the object.
                    interDeepCopy.Body.Instructions.AddI(OpCodes.Ldarg_0);
                    interDeepCopy.Body.Instructions.AddI(OpCodes.Call, deepCopyMethod);
                    interDeepCopy.Body.Instructions.AddI(OpCodes.Ret);

                    // Add the interface-specific deep copy method.
                    type.Methods.Add(interDeepCopy);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format("Error adding {0} to interface type {1}.",
                            this._deepCopyMethodName, type), ex);
                }
            }
        }

        private static TypeDefinition GetBaseDefinition(TypeDefinition type)
        {
            if (null == type.BaseType)
                return null;

            if (!type.BaseType.IsGenericInstance)
                return type.BaseType as TypeDefinition;

            var genericInstanceType = type.BaseType as GenericInstanceType;
            if (null == genericInstanceType)
                throw new Exception("basetype should be generic");

            return genericInstanceType.ElementType as TypeDefinition;
        }

        private void FillMethodBodies(List<TypeDefinition> allTypes)
        {
            foreach (var type in allTypes.Where(type => !type.IsPrimitiveObject()))
            {
                try
                {
                    // Get the main deep copy method for the object.
                    MethodDefinition deepCopyMethod;
                    if (!type.TryGetMethod(this._deepCopyMethodName, out deepCopyMethod))
                    {
                        this.LogInfo(String.Format("Type {0} is missing the {1} method.", type, this._deepCopyMethodName));
                        continue;
                    }

                    var deepCopyReference = this.ModuleDefinition.Import(deepCopyMethod);
                    var deepCopyDef = deepCopyReference.Resolve();

                    var typeEmptyConstructorReference = this.GetOrAddDefaultConstructor(type);

                    if (null == deepCopyDef)
                        throw new Exception("deep copy definition should not be null");

                    var processor = deepCopyDef.Body.GetILProcessor();

                    // Add and get a reference to the variable that'll hold our final result.
                    var retVar = processor.Body.Variables.AddV(type);

                    CurrentData currentData = new CurrentData
                        {
                            ExceptionHandlers = processor.Body.ExceptionHandlers,
                            Instructions = processor.Body.Instructions,
                            Variables = processor.Body.Variables
                        };

                    currentData.NewObjects.Push(retVar);

                    // Create a new instance of 'type' and store in the variable.
                    currentData.Instructions.AddI(OpCodes.Newobj, typeEmptyConstructorReference);
                    currentData.Instructions.AddI(OpCodes.Stloc, currentData.NewObjects.Peek());

                    // Copy stuff!
                    this.CopyFields(currentData, type);
                    this.CopyProperties(currentData, type);

                    // Get the variable with the new instance of 'type' and return it.
                    currentData.Instructions.AddI(OpCodes.Ldloc, currentData.NewObjects.Pop());
                    currentData.Instructions.AddI(OpCodes.Ret);

                    // Need this to make PeVerify happy.
                    processor.Body.InitLocals = true;

                    processor.Body.OptimizeMacros();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debugger.Break();
                    throw new Exception(String.Format("Error in type {0}.\n{1}", type, ex), ex);
                }
            }
        }

        /// <summary>
        /// Check ALL lines of code (il lines) for calls to the extension method deep copy, and replace
        /// with calls to the object's new deep copy method.
        /// </summary>
        private void ChangeExtensionMethodCallsToObjectCalls()
        {
            foreach (var type in this.ModuleDefinition.Types)
            {
                if (!type.HasMethods)
                    continue;

                foreach (var method in type.Methods.Where(method => method.HasBody))
                {
                    var instructions = method.Body.Instructions;
                    for (int index = 0; index < instructions.Count; ++index)
                    {
                        var instruction = instructions[index];

                        // Is this a call op?
                        if (instruction.OpCode != OpCodes.Call)
                            continue;

                        // Is this a call to deep copy?
                        if (!instruction.Operand.ToString().Contains("DeepCopy"))
                            continue;

                        // Is this a method ref?
                        var methodReference = instruction.Operand as MethodReference;
                        if (null == methodReference)
                            throw new Exception("MethodReference cannot be null.");

                        // Is this a generic call?
                        var genericInstanceMethod = methodReference as GenericInstanceMethod;
                        if (null == genericInstanceMethod)
                            continue;

                        var genericArguments = genericInstanceMethod.GenericArguments;
                        if (0 >= genericArguments.Count)
                            throw new Exception("there should be a generic argument for the deep copy call");

                        // Get the type definition whose deep copy method we'll be using instead.
                        var typedDefinition = genericArguments[0].Resolve();
                        if (null == typedDefinition)
                            continue;

                        // If the object doesn't have a deep copy method, just continue.
                        MethodDefinition deepCopy;
                        if (!typedDefinition.TryGetMethod(this._deepCopyMethodName, out deepCopy))
                            continue;

                        var objectsDeepCopy = Instruction.Create(OpCodes.Callvirt, deepCopy);

                        // Remove the old call.
                        instructions.RemoveAt(index);

                        // Insert the new call.
                        instructions.Insert(index, objectsDeepCopy);
                    }
                }
            }
        }

        private void Finalize()
        {
//            assemblyDefinition.Write(assemblyName, writerParameters);
            this.LogInfo("Replaced DeepCopy method bodies with copy logic.");
        }

        private void UpdateChosenDeepCloneMethod()
        {
            // Get the type that has the deep copy method.
            var typeWithDeepCopyMethod = this.ModuleDefinition.Types
                .Where(x => null != x)
                .Where(x => x.HasMethods)
                .FirstOrDefault(x => x.Methods.Any(m =>
                    m.HasCustomAttributes && m.CustomAttributes.Any(a =>
                        a.AttributeType.Name.Equals(ModuleWeaver._deepCopyAttributeName))));

            if (null == typeWithDeepCopyMethod)
                throw new Exception("need to choose a deep copy method");

            // Get the deep copy method name.
            foreach (var m in typeWithDeepCopyMethod.Methods)
            {
                if (m.HasCustomAttributes &&
                    m.CustomAttributes.Any(a =>
                        a.AttributeType.Name.Equals(ModuleWeaver._deepCopyAttributeName)))
                {
                    this._deepCopyMethodName = m.Name;
                    return;
                }
            }

            throw new Exception("should never get here");
        }

        private MethodReference GetOrAddDefaultConstructor(TypeDefinition type)
        {
            var typeEmptyConstructor = type.GetEmptyConstructor(this.ModuleDefinition);
            if (null != typeEmptyConstructor)
                return typeEmptyConstructor;

            MethodDefinition defaultConstructor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Private |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                this.ModuleDefinition.TypeSystem.Void);

            var instructions = defaultConstructor.Body.Instructions;
            var emptyConstructor = type.BaseType.Resolve().GetEmptyConstructor(this.ModuleDefinition);

            instructions.AddI(OpCodes.Ldarg_0);
            instructions.AddI(OpCodes.Call, emptyConstructor);
            instructions.AddI(OpCodes.Ret);

            defaultConstructor.Body.InitLocals = true;

            type.Methods.Add(defaultConstructor);

            return defaultConstructor;
        }

        private void CopyFields(CurrentData currentData, TypeDefinition type)
        {
            foreach (var field in type.AccessibleFields())
                this.HandleField(currentData, field);
        }

        private void CopyProperties(CurrentData currentData, TypeDefinition type)
        {
            foreach (var property in type.AccessiblePropertiesOmittingAccessibleFields())
                this.HandleProperty(currentData, property);
        }

        private void HandleField(CurrentData currentData, FieldDefinition field)
        {
            Action<Collection<Instruction>> load = collection => collection.AddI(OpCodes.Ldfld, field);
            Action<Collection<Instruction>> store = collection => collection.AddI(OpCodes.Stfld, field);

            TypeReference typeReference = field.FieldType;
            TypeDefinition typeDefinition = (typeReference as TypeDefinition);

            if (typeReference.IsPrimitiveObject())
            {
                this.CopyPrimitive(currentData, load, store);
            }
            else if (typeReference.IsArray)
            {
                this.HandleArray(currentData, typeReference, load, store);
            }
            else if (typeReference.IsGenericInstance)
            {
                if (typeReference.Name.Equals("List`1"))
                {
                    this.HandleList(currentData, typeReference, load, store);
                }
                else if (typeReference.Name.Equals("Dictionary`2"))
                {
                    this.HandleDictionary(currentData, typeReference, load, store);
                }
            }
            else if (typeReference.IsGenericParameter)
            {
                // TODO how to handle?
            }
            else
            {
                if (null == typeDefinition)
                    throw new Exception(String.Format("No TypeDefinition for {0}.", field.FullName));

                this.SafeCallDeepCopy(currentData, typeDefinition, load, store);
            }
        }

        private void HandleArray(CurrentData currentData, TypeReference arrayRef,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store)
        {
            ArrayType arrayType = (ArrayType) arrayRef;
            TypeReference elementType = arrayType.ElementType;

            if (elementType.IsPrimitiveObject())
            {
                var newArray = currentData.Variables.AddV(arrayRef);
                var currentIndex = currentData.Variables.AddV(this.ModuleDefinition.TypeSystem.Int32); // TODO replace
                var length = currentData.Variables.AddV(this.ModuleDefinition.TypeSystem.Int32); // TODO replace

                var loadMainObj = Instruction.Create(OpCodes.Ldloc, currentData.NewObjects.Peek());
                currentData.Instructions.AddI(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Stloc, newArray);
                currentData.Instructions.AddI(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ceq);
                currentData.Instructions.AddI(OpCodes.Brtrue_S, loadMainObj);

                var IL_0059 = Instruction.Create(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ldlen);
                currentData.Instructions.AddI(OpCodes.Conv_I4); // TODO replace
                currentData.Instructions.AddI(OpCodes.Stloc, length);
                currentData.Instructions.AddI(OpCodes.Ldloc, length);
                currentData.Instructions.AddI(OpCodes.Newarr, elementType);
                currentData.Instructions.AddI(OpCodes.Stloc, newArray);
                currentData.Instructions.AddI(OpCodes.Ldc_I4_0); // TODO replace
                currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Br_S, IL_0059);

                // loop start
                var IL_0025 = Instruction.Create(OpCodes.Ldloc, newArray);
                currentData.Instructions.Add(IL_0025);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);

                // Change for each primitive.
                Type type = Type.GetType(elementType.FullName);
                if (typeof (System.Int16) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_I2);
                    currentData.Instructions.AddI(OpCodes.Stelem_I2);
                }
                else if (typeof (System.Int32) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_I4);
                    currentData.Instructions.AddI(OpCodes.Stelem_I4);
                }
                else if (typeof (System.Int64) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_I8);
                    currentData.Instructions.AddI(OpCodes.Stelem_I8);
                }
                else if (typeof (float) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_R4);
                    currentData.Instructions.AddI(OpCodes.Stelem_R4);
                }
                else if (typeof (double) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_R8);
                    currentData.Instructions.AddI(OpCodes.Stelem_R8);
                }
                else if (typeof (System.UInt16) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_U2);
                    currentData.Instructions.AddI(OpCodes.Stelem_I4);
                }
                else if (typeof (System.UInt32) == type)
                {
                    currentData.Instructions.AddI(OpCodes.Ldelem_U4);
                    currentData.Instructions.AddI(OpCodes.Stelem_I4);
                }

                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldc_I4_1); // TODO replace
                currentData.Instructions.AddI(OpCodes.Add);
                currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);

                // Increment the index.
                currentData.Instructions.Add(IL_0059);
                currentData.Instructions.AddI(OpCodes.Ldloc, length);
                currentData.Instructions.AddI(OpCodes.Clt);
                currentData.Instructions.AddI(OpCodes.Brtrue_S, IL_0025);
                // end loop

                currentData.Instructions.Add(loadMainObj);
                currentData.Instructions.AddI(OpCodes.Ldloc, newArray);
                store(currentData.Instructions);
            }
            else if (elementType.IsGenericParameter)
            {
                // TODO how to handle?
            }
            else
            {
                var typeDefinition = arrayRef.Resolve();

                MethodDefinition deepCopy;
                if (!typeDefinition.TryGetMethod(this._deepCopyMethodName, out deepCopy))
                    throw new Exception(String.Format("Sub-type {0} does not implement DeepCopy.",
                        typeDefinition.FullName));

                var newArray = currentData.Variables.AddV(arrayRef);
                var currentIndex = currentData.Variables.AddV(this.ModuleDefinition.TypeSystem.Int32); // TODO replace
                var length = currentData.Variables.AddV(this.ModuleDefinition.TypeSystem.Int32); // TODO replace

                var loadMainObj = Instruction.Create(OpCodes.Ldloc, currentData.NewObjects.Peek());
                currentData.Instructions.AddI(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Stloc, newArray);
                currentData.Instructions.AddI(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ceq);
                currentData.Instructions.AddI(OpCodes.Brtrue_S, loadMainObj);

                var IL_0059 = Instruction.Create(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ldlen);
                currentData.Instructions.AddI(OpCodes.Conv_I4); // TODO replace
                currentData.Instructions.AddI(OpCodes.Stloc, length);
                currentData.Instructions.AddI(OpCodes.Ldloc, length);
                currentData.Instructions.AddI(OpCodes.Newarr, elementType);
                currentData.Instructions.AddI(OpCodes.Stloc, newArray);
                currentData.Instructions.AddI(OpCodes.Ldc_I4_0); // TODO replace
                currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Br_S, IL_0059);

                // loop start
                var IL_0025 = Instruction.Create(OpCodes.Ldnull);
                var IL_0042 = Instruction.Create(OpCodes.Ldloc, newArray);
                currentData.Instructions.Add(IL_0025);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldelem_Ref);
                currentData.Instructions.AddI(OpCodes.Ceq);
                currentData.Instructions.AddI(OpCodes.Brfalse_S, IL_0042);

                var IL_0054 = Instruction.Create(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldloc, newArray);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Stelem_Ref);
                currentData.Instructions.AddI(OpCodes.Br_S, IL_0054);

                currentData.Instructions.Add(IL_0042);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldarg_0);
                load(currentData.Instructions);
                currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
                currentData.Instructions.AddI(OpCodes.Ldelem_Ref);
                currentData.Instructions.AddI(OpCodes.Callvirt, deepCopy);
                currentData.Instructions.AddI(OpCodes.Stelem_Ref);

                // Increment the index.
                currentData.Instructions.Add(IL_0054);
                currentData.Instructions.AddI(OpCodes.Ldc_I4_1); // TODO replace
                currentData.Instructions.AddI(OpCodes.Add);
                currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);

                currentData.Instructions.Add(IL_0059);
                currentData.Instructions.AddI(OpCodes.Ldloc, length);
                currentData.Instructions.AddI(OpCodes.Clt);
                currentData.Instructions.AddI(OpCodes.Brtrue_S, IL_0025);
                // end loop

                currentData.Instructions.Add(loadMainObj);
                currentData.Instructions.AddI(OpCodes.Ldloc, newArray);
                store(currentData.Instructions);
            }
        }

        private void HandleList(CurrentData currentData, TypeReference listRef,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store)
        {
            string fullName = listRef.FullName;


            var git = (listRef as GenericInstanceType);
            if (null == git || !git.HasGenericArguments || 1 < git.GenericArguments.Count)
                throw new Exception(String.Format("Unhandled case for type {0}.", fullName));

            MethodReference addMethod = this.GetTypeMethod(listRef, "Add");

            // Do not merge this variable with enumeratorMethod (it get's modified).
            var getCount = this.GetTypeMethod(listRef, "get_Count");
            var getItem = this.GetTypeMethod(listRef, "get_Item");

            // TODO don't restrict to first?
            TypeReference listObjectsType = git.GenericArguments.First();
            if (listObjectsType.IsPrimitiveObject())
            {
                this.HandleListOfPrimitives(currentData, listRef, load, store, getCount, getItem, addMethod);
            }
            else
            {
                this.HandleListOfObjects(currentData, listRef, load, store, listObjectsType, getCount, getItem,
                    addMethod);
            }
        }

        private void HandleListOfPrimitives(CurrentData currentData, TypeReference listRef,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store,
            MethodReference getCount, MethodReference getItem, MethodReference addMethod)
        {
            var intType = this.ModuleDefinition.Import(this.ModuleDefinition.TypeSystem.Int32).Resolve();
            var listOfObjConstructor = this.GetGenericTypeConstructorMethod(listRef, new[]
                {
                    new ParameterDefinition(intType)
                });

            var newList = currentData.Variables.AddV(listRef);
            var count = currentData.Variables.AddV(intType);
            var currentIndex = currentData.Variables.AddV(intType);

            var loadNullAndRet = Instruction.Create(OpCodes.Ldloc, currentData.NewObjects.Peek());
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Stloc, newList);
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Ceq);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, loadNullAndRet);

            var forLoopCondition = Instruction.Create(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Callvirt, getCount);
            currentData.Instructions.AddI(OpCodes.Stloc, count);
            currentData.Instructions.AddI(OpCodes.Ldloc, count);
            currentData.Instructions.AddI(OpCodes.Newobj, listOfObjConstructor);
            currentData.Instructions.AddI(OpCodes.Stloc, newList);
            currentData.Instructions.AddI(OpCodes.Ldc_I4_0);
            currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Br_S, forLoopCondition);

            // loop start
            var loopStart = Instruction.Create(OpCodes.Ldloc, newList);
            currentData.Instructions.Add(loopStart);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Callvirt, getItem);
            currentData.Instructions.AddI(OpCodes.Callvirt, addMethod);

            // loop increment
            currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Ldc_I4_1);
            currentData.Instructions.AddI(OpCodes.Add);
            currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);

            // check if condition
            currentData.Instructions.Add(forLoopCondition);
            currentData.Instructions.AddI(OpCodes.Ldloc, count);
            currentData.Instructions.AddI(OpCodes.Clt);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, loopStart);
            // end loop

            currentData.Instructions.Add(loadNullAndRet);
            currentData.Instructions.AddI(OpCodes.Ldloc, newList);
            store(currentData.Instructions);
        }

        private void HandleListOfObjects(CurrentData currentData, TypeReference listRef,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store,
            TypeReference listObjectsType, MethodReference getCount, MethodReference getItem,
            MethodReference addMethod)
        {
            var typeDefinition = listObjectsType as TypeDefinition;
            if (null == typeDefinition)
                throw new Exception(
                    String.Format("List object type {0} is not a TypeDefinition.", listObjectsType.FullName));

            MethodDefinition deepCopy;
            if (!typeDefinition.TryGetMethod(this._deepCopyMethodName, out deepCopy))
                throw new Exception(
                    String.Format("Sub-type {0} does not implement DeepCopy.", typeDefinition.FullName));

            var intType = this.ModuleDefinition.Import(this.ModuleDefinition.TypeSystem.Int32).Resolve();
            var listOfObjConstructor = this.GetGenericTypeConstructorMethod(listRef, new[]
                {
                    new ParameterDefinition(intType)
                });

            var newList = currentData.Variables.AddV(listRef);
            var listObject = currentData.Variables.AddV(listObjectsType);
            var count = currentData.Variables.AddV(intType);
            var currentIndex = currentData.Variables.AddV(intType);

            var loadNullAndRet = Instruction.Create(OpCodes.Ldloc, currentData.NewObjects.Peek());
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Stloc, newList);
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Ceq);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, loadNullAndRet);

            var forLoopCondition = Instruction.Create(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Callvirt, getCount);
            currentData.Instructions.AddI(OpCodes.Stloc, count);
            currentData.Instructions.AddI(OpCodes.Ldloc, count);
            currentData.Instructions.AddI(OpCodes.Newobj, listOfObjConstructor);
            currentData.Instructions.AddI(OpCodes.Stloc, newList);
            currentData.Instructions.AddI(OpCodes.Ldc_I4_0);
            currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Br_S, forLoopCondition);

            // loop start
            var storeNull = Instruction.Create(OpCodes.Ldnull);
            var loopStart = Instruction.Create(OpCodes.Ldarg_0);
            currentData.Instructions.Add(loopStart);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Callvirt, getItem);
            currentData.Instructions.AddI(OpCodes.Stloc, listObject);
            currentData.Instructions.AddI(OpCodes.Ldloc, newList);
            currentData.Instructions.AddI(OpCodes.Ldloc, listObject);
            currentData.Instructions.AddI(OpCodes.Brfalse_S, storeNull);

            var add = Instruction.Create(OpCodes.Callvirt, addMethod);
            currentData.Instructions.AddI(OpCodes.Ldloc, listObject);
            currentData.Instructions.AddI(OpCodes.Callvirt, deepCopy);
            currentData.Instructions.AddI(OpCodes.Br_S, add);

            currentData.Instructions.Add(storeNull);

            currentData.Instructions.Add(add);

            // loop increment
            currentData.Instructions.AddI(OpCodes.Ldloc, currentIndex);
            currentData.Instructions.AddI(OpCodes.Ldc_I4_1);
            currentData.Instructions.AddI(OpCodes.Add);
            currentData.Instructions.AddI(OpCodes.Stloc, currentIndex);

            // check if condition
            currentData.Instructions.Add(forLoopCondition);
            currentData.Instructions.AddI(OpCodes.Ldloc, count);
            currentData.Instructions.AddI(OpCodes.Clt);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, loopStart);
            // end loop

            currentData.Instructions.Add(loadNullAndRet);
            currentData.Instructions.AddI(OpCodes.Ldloc, newList);
            store(currentData.Instructions);
        }

        private void HandleDictionary(CurrentData currentData, TypeReference dictRef,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store)
        {
            string fullName = dictRef.FullName;

            var git = (dictRef as GenericInstanceType);
            if (null == git || !git.HasGenericArguments || 2 != git.GenericArguments.Count)
                throw new Exception(String.Format("Unhandled case for type {0}.", fullName));

            MethodReference enumeratorMethod = this.GetTypeMethod(dictRef, "GetEnumerator");
            MethodReference comparerMethod = this.GetTypeMethod(dictRef, "get_Comparer");
            MethodReference addMethod = this.GetTypeMethod(dictRef, "Add");

            // Do not merge this variable with enumeratorMethod (it get's modified).
            var methodReference = this.GetTypeMethod(dictRef, "GetEnumerator");
            var getCount = this.GetTypeMethod(dictRef, "get_Count");
            var genericEnumerator = methodReference.ReturnType as GenericInstanceType;
            if (null != genericEnumerator)
            {
                genericEnumerator.GenericArguments.Clear();

                var baseRef = dictRef as GenericInstanceType;
                foreach (var arg in baseRef.GenericArguments)
                    genericEnumerator.GenericArguments.Add(arg);
            }

            MethodReference getCurrent = this.GetTypeMethod(genericEnumerator, "get_Current");
            MethodReference moveNext = this.GetTypeMethod(genericEnumerator, "MoveNext");

            MethodReference dispose;
            if (!this.ModuleDefinition.TryGetMethodReference(typeof (IDisposable), "Dispose", out dispose))
                throw new Exception(String.Format("Unable to get IDisposable.Dispose() for type {0}.", fullName));

            var intType = this.ModuleDefinition.Import(this.ModuleDefinition.TypeSystem.Int32).Resolve();
            var dictOfObjConstructor = this.GetGenericTypeConstructorMethod(dictRef, new []
                {
                    new ParameterDefinition(intType),
                    new ParameterDefinition(comparerMethod.ReturnType)
                });

            var typeReference = getCurrent.ReturnType.GetElementType();
            var genericDict = dictRef as GenericInstanceType;
            var genericKVP = typeReference.MakeGenericType(genericDict.GenericArguments.ToArray());

            MethodReference getKey = this.GetTypeMethod(genericKVP, "get_Key");
            MethodReference getValue = this.GetTypeMethod(genericKVP, "get_Value");

            MethodDefinition keyDeepCopy = null;
            var keyDef = genericDict.GenericArguments[0] as TypeDefinition;
            if (null != keyDef)
                keyDef.TryGetMethod(this._deepCopyMethodName, out keyDeepCopy);

            MethodDefinition valueDeepCopy = null;
            var valueDef = genericDict.GenericArguments[1] as TypeDefinition;
            if (null != valueDef)
                valueDef.TryGetMethod(this._deepCopyMethodName, out valueDeepCopy);

            var newDict = currentData.Variables.AddV(dictRef);
            var enumerator = currentData.Variables.AddV(genericEnumerator);
            var kvp = currentData.Variables.AddV(genericKVP);
            VariableDefinition kvpValue = null;
            if (null != valueDef)
                kvpValue = currentData.Variables.AddV(valueDef);

            var IL_006f = Instruction.Create(OpCodes.Ldloc, currentData.NewObjects.Peek());
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Stloc, newDict);
            currentData.Instructions.AddI(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Ceq);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, IL_006f);

            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Callvirt, getCount);

            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Callvirt, comparerMethod);
            currentData.Instructions.AddI(OpCodes.Newobj, dictOfObjConstructor);
            currentData.Instructions.AddI(OpCodes.Stloc, newDict);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Callvirt, enumeratorMethod);
            currentData.Instructions.AddI(OpCodes.Stloc_S, enumerator);

            // try
            var IL_004f = Instruction.Create(OpCodes.Ldloca_S, enumerator);
            var tryStart = Instruction.Create(OpCodes.Br_S, IL_004f);
            currentData.Instructions.Add(tryStart);

            // loop start
            var loopStart = Instruction.Create(OpCodes.Ldloca_S, enumerator);
            currentData.Instructions.Add(loopStart);
            currentData.Instructions.AddI(OpCodes.Call, getCurrent);
            currentData.Instructions.AddI(OpCodes.Stloc, kvp);

            if (!genericDict.GenericArguments[1].IsPrimitiveObject())
            {
                // store the kvp value in a local variable
                currentData.Instructions.AddI(OpCodes.Ldloca_S, kvp);
                currentData.Instructions.AddI(OpCodes.Call, getValue);
                currentData.Instructions.AddI(OpCodes.Stloc, kvpValue);
            }

            currentData.Instructions.AddI(OpCodes.Ldloc, newDict);
            currentData.Instructions.AddI(OpCodes.Ldloca_S, kvp);
            currentData.Instructions.AddI(OpCodes.Call, getKey);
            if (!genericDict.GenericArguments[0].IsPrimitiveObject())
                currentData.Instructions.AddI(OpCodes.Callvirt, keyDeepCopy);

            if (!genericDict.GenericArguments[1].IsPrimitiveObject())
            {
                var loadNull = Instruction.Create(OpCodes.Ldnull);
                currentData.Instructions.AddI(OpCodes.Ldloc, kvpValue);
                currentData.Instructions.AddI(OpCodes.Brfalse_S, loadNull);

                var add = Instruction.Create(OpCodes.Callvirt, addMethod);
                currentData.Instructions.AddI(OpCodes.Ldloc, kvpValue);
                currentData.Instructions.AddI(OpCodes.Callvirt, valueDeepCopy);
                currentData.Instructions.AddI(OpCodes.Br_S, add);

                currentData.Instructions.Add(loadNull);

                currentData.Instructions.Add(add);
            }
            else
            {
                currentData.Instructions.AddI(OpCodes.Ldloca_S, kvp);
                currentData.Instructions.AddI(OpCodes.Call, getValue);
                currentData.Instructions.AddI(OpCodes.Callvirt, addMethod);
            }

            currentData.Instructions.Add(IL_004f);
            currentData.Instructions.AddI(OpCodes.Call, moveNext);
            currentData.Instructions.AddI(OpCodes.Brtrue_S, loopStart);
            // end loop

            currentData.Instructions.AddI(OpCodes.Leave_S, IL_006f);
            // end try
            // finally

            var finallyStart = Instruction.Create(OpCodes.Ldloca_S, enumerator);

            var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = tryStart,
                    TryEnd = finallyStart,
                    HandlerStart = finallyStart,
                    HandlerEnd = IL_006f
                };
            currentData.ExceptionHandlers.Add(finallyHandler);

            currentData.Instructions.Add(finallyStart);
            currentData.Instructions.AddI(OpCodes.Constrained, genericEnumerator);
            currentData.Instructions.AddI(OpCodes.Callvirt, dispose);
            currentData.Instructions.AddI(OpCodes.Endfinally);
            // end handler

            currentData.Instructions.Add(IL_006f);
            currentData.Instructions.AddI(OpCodes.Ldloc, newDict);
            store(currentData.Instructions);
        }

        private MethodReference GetTypeMethod(TypeReference typeReference, string methodName)
        {
            MethodDefinition methodDefinition;
            if (!typeReference.Resolve().TryGetMethod(methodName, out methodDefinition))
                throw new Exception();

            MethodReference typelessReference = this.ModuleDefinition.Import(methodDefinition);

            var baseInstance = typeReference as GenericInstanceType;
            if (null != baseInstance)
                return typelessReference.MakeGeneric(baseInstance.GenericArguments.ToArray());

            if (!typeReference.HasGenericParameters)
                throw new Exception(String.Format("Type {0} is not generic?", typeReference.FullName));

            typelessReference.GenericParameters.Clear();
            foreach (var param in typeReference.GenericParameters)
                typelessReference.GenericParameters.Add(param);

            return typelessReference;
        }

        private MethodReference GetGenericTypeConstructorMethod(TypeReference typeReference)
        {
            var typeDefinition = typeReference.Resolve();

            // Get the type reference for the empty constructor.
            var typelessReference = typeDefinition.GetEmptyConstructor(this.ModuleDefinition);

            // Create a generic instance of it.
            var baseInstance = typeReference as GenericInstanceType;
            if (null == baseInstance)
                throw new Exception(String.Format("Base type {0} is not generic.", typeReference.FullName));

            // Add the generic parameterTypes to the empty constructor.
            var baseMethodRef = typelessReference.MakeGeneric(baseInstance.GenericArguments.ToArray());

            return baseMethodRef;
        }

        private MethodReference GetGenericTypeConstructorMethod(TypeReference typeReference,
            ParameterDefinition[] parameters)
        {
            var typeDefinition = typeReference.Resolve();

            // Get the type reference for the empty constructor.
            MethodReference typelessReference = null;
            foreach (var methodReference in typeDefinition.GetConstructors()
                .Where(x => x.Parameters.Count == parameters.Count()))
            {
                if (!MatchesConstructor(
                    parameters.Select(p => p.ParameterType).ToArray(),
                    methodReference.Parameters.Select(p => p.ParameterType).ToArray()))
                    continue;

                typelessReference = this.ModuleDefinition.Import(methodReference);
                break;
            }

            // Create a generic instance of it.
            var baseInstance = typeReference as GenericInstanceType;
            if (null == baseInstance)
                throw new Exception(String.Format("Base type {0} is not generic.", typeReference.FullName));

            // Add the generic parameterTypes to the empty constructor.
            var baseMethodRef = typelessReference.MakeGeneric(baseInstance.GenericArguments.ToArray());

            return baseMethodRef;
        }

        private static bool MatchesConstructor(TypeReference[] parameterTypes, TypeReference[] origTypes)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (!origTypes[i].IsPrimitive)
                {
                    if (origTypes[i].IsGenericInstance)
                    {
                        if (!parameterTypes[i].IsGenericInstance)
                            return false;

                        var origGen = origTypes[i] as GenericInstanceType;
                        if (null == origGen)
                            throw new Exception("not handled");

                        var thisGen = parameterTypes[i] as GenericInstanceType;
                        if (null == thisGen)
                            throw new Exception("not handled");

                        if (origGen.GenericArguments.Count != thisGen.GenericArguments.Count)
                            return false;

                        return MatchesConstructor(
                            thisGen.GenericArguments.ToArray(),
                            origGen.GenericArguments.ToArray());
                    }
                    else if (origTypes[i].IsGenericParameter)
                    {
                        if (!parameterTypes[i].IsGenericParameter)
                            return false;

                        if (origTypes[i].FullName != parameterTypes[i].FullName)
                            return false;
                    }
                }
                else
                {
                    if (origTypes[i] != parameterTypes[i])
                        return false;
                }
            }

            return true;
        }

        private void HandleProperty(CurrentData currentData, Tuple<MethodDefinition, MethodDefinition> property)
        {
            Action<Collection<Instruction>> load = collection => collection.AddI(OpCodes.Call, property.Item1);
            Action<Collection<Instruction>> store = collection => collection.AddI(OpCodes.Callvirt, property.Item2);

            var typeReference = property.Item1.MethodReturnType.ReturnType;
            var typeDefinition = (typeReference as TypeDefinition);

            if (typeReference.IsPrimitiveObject())
            {
                this.CopyPrimitive(currentData, load, store);
            }
            else if (typeReference.IsArray)
            {
                this.HandleArray(currentData, typeReference, load, store);
            }
            else if (typeReference.IsGenericInstance)
            {
                if (typeReference.Name.Equals("List`1"))
                {
                    this.HandleList(currentData, typeReference, load, store);
                }
            }
            else if (typeReference.IsGenericParameter)
            {
                // TODO how to handle?
            }
            else
            {
                if (null == typeDefinition)
                    throw new Exception(String.Format("No TypeDefinition for {0}.", property.Item1));

                this.SafeCallDeepCopy(currentData, typeDefinition, load, store);
            }
        }

        private void CopyPrimitive(CurrentData currentData, Action<Collection<Instruction>> load,
            Action<Collection<Instruction>> store)
        {
            currentData.Instructions.AddI(OpCodes.Ldloc, currentData.NewObjects.Peek());
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            store(currentData.Instructions);
        }

        private void SafeCallDeepCopy(CurrentData currentData, TypeDefinition typeDefinition,
            Action<Collection<Instruction>> load, Action<Collection<Instruction>> store)
        {
            MethodDefinition deepCopy;
            if (!typeDefinition.TryGetMethod(this._deepCopyMethodName, out deepCopy))
                throw new Exception(
                    String.Format("Sub-type {0} does not implement DeepCopy.", typeDefinition.FullName));

            var var0 = currentData.Variables.AddV(typeDefinition);

            // Load the object, and check to see if it's null.
            currentData.Instructions.AddI(OpCodes.Nop);
            currentData.Instructions.AddI(OpCodes.Ldarg_0);
            load(currentData.Instructions);
            currentData.Instructions.AddI(OpCodes.Stloc, var0);
            currentData.Instructions.AddI(OpCodes.Ldloc, currentData.NewObjects.Peek());
            currentData.Instructions.AddI(OpCodes.Ldloc, var0);

            var loadNull = Instruction.Create(OpCodes.Ldnull);
            currentData.Instructions.AddI(OpCodes.Brfalse_S, loadNull);

            currentData.Instructions.AddI(OpCodes.Ldloc, var0);
            currentData.Instructions.AddI(OpCodes.Callvirt, deepCopy);
            var noOp = Instruction.Create(OpCodes.Nop);
            currentData.Instructions.AddI(OpCodes.Br_S, noOp);

            currentData.Instructions.Add(loadNull);

            currentData.Instructions.Add(noOp);
            store(currentData.Instructions);
        }
    }
}