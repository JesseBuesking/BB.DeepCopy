using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace BB.DeepCopy
{
    public static class PropertyAndFieldExtensions
    {
        public static IEnumerable<FieldDefinition> AccessibleFields(this TypeDefinition typeDefinition)
        {
            if (null == typeDefinition)
                throw new ArgumentNullException("typeDefinition");

            // Use all fields on the type itself (since we can actually access all backing fields).
            var result = new List<FieldDefinition>();
            result.AddRange(typeDefinition.PublicFields());
            result.AddRange(typeDefinition.PrivateFields());

            var baseTypeReferences = new Queue<TypeReference>();
            if (!new[] {"Object"}.Contains(typeDefinition.BaseType.Name))
            {
                baseTypeReferences.Enqueue(typeDefinition.BaseType);
                while (0 < baseTypeReferences.Count)
                {
                    TypeReference baseType = baseTypeReferences.Dequeue();

                    var baseDefinition = baseType.Resolve();

                    // Use any available non-backing fields.
                    result.AddRange(baseDefinition.PublicFields());
                    result.AddRange(baseDefinition.PrivateFields()
                        .Where(pf => !pf.Name.Contains("__BackingField")));

                    var baseReference = baseDefinition.BaseType;
                    if (!new[] {"Object"}.Contains(baseReference.Name))
                        baseTypeReferences.Enqueue(baseReference);
                }
            }

            return result;
        }

        public static IEnumerable<Tuple<MethodDefinition, MethodDefinition>>
            AccessiblePropertiesOmittingAccessibleFields(this TypeDefinition typeDefinition)
        {
            if (null == typeDefinition)
                throw new ArgumentNullException("typeDefinition");

            // Skip all properties on the type itself (we use all fields instead).
            var result = new List<Tuple<MethodDefinition, MethodDefinition>>();

            var baseTypeReferences = new Queue<TypeReference>();
            if (!new[] {"Object"}.Contains(typeDefinition.BaseType.Name))
            {
                baseTypeReferences.Enqueue(typeDefinition.BaseType);
                while (0 < baseTypeReferences.Count)
                {
                    TypeReference baseType = baseTypeReferences.Dequeue();

                    var baseDefinition = baseType.Resolve();
                    var inaccessibleFields =
                        baseDefinition.PrivateFields().Where(pf => pf.Name.Contains("__BackingField"))
                        .ToList();

                    // Add all properties that are backed by inaccessible fields.
                    result.AddRange(baseDefinition.PublicProperties(inaccessibleFields));
                    result.AddRange(baseDefinition.PrivateProperties(inaccessibleFields));

                    var baseReference = baseDefinition.BaseType;
                    if (!new[] {"Object"}.Contains(baseReference.Name))
                        baseTypeReferences.Enqueue(baseReference);
                }
            }

            return result;
        }

        public static FieldDefinition PropertiesBackingField(this MethodDefinition propertySetter)
        {
            Instruction previousInstruction = null;
            foreach (var instruction in propertySetter.Body.Instructions)
            {
                if (OpCodes.Stfld != instruction.OpCode)
                {
                    previousInstruction = instruction;
                    continue;
                }

                if (null == previousInstruction)
                    throw new Exception("how'd we get here?");

                // Only use the stfld that uses 'value'
                // e.g. this.backingProperty == 'value';
                if ((OpCodes.Ldarg_1 != previousInstruction.OpCode) &&
                    (OpCodes.Ldarg != previousInstruction.OpCode || 0 != (int) previousInstruction.Operand))
                {
                    previousInstruction = instruction;
                    continue;
                }

                var operand = instruction.Operand;
                var fieldReference = operand as FieldReference;
                if (null == fieldReference)
                    throw new Exception("not handling null field references");

                var fieldDefinition = fieldReference.Resolve();
                if (null == fieldDefinition)
                    throw new Exception("not handling null field defintions");

                return fieldDefinition;
            }

            throw new Exception("not handling default case");
        }

        public static IEnumerable<FieldDefinition> PublicFields(this TypeDefinition type)
        {
            return type.Fields.Where(x => x.IsPublic)
                .Where(x => !x.IsStatic);
        }

        public static IEnumerable<Tuple<MethodDefinition, MethodDefinition>> PublicProperties(
            this TypeDefinition type, List<FieldDefinition> backingFields)
        {
            return type.Methods
                .Where(x => x.IsPublic && x.IsProperty() && x.IsGetter)
                .Where(x => !x.IsStatic)
                .Select(x =>
                    {
                        var result = new Tuple<MethodDefinition, MethodDefinition>(x, type.Methods
                            .SingleOrDefault(m =>
                                m.IsSetter &&
                                    m.IsProperty() &&
                                    m.Name != null &&
                                    m.Name.Equals("set_" + x.Name.Substring(4, x.Name.Length - 4))));

                        var backingField = result.Item2.PropertiesBackingField();
                        if (!backingFields.Any(uf => uf.FullName.Equals(backingField.FullName)))
                            result = new Tuple<MethodDefinition, MethodDefinition>(result.Item1, null);

                        return result;
                    })
                .Where(x => null != x.Item2);
        }

        public static IEnumerable<Tuple<MethodDefinition, MethodDefinition>> PublicProperties(
            this TypeDefinition type)
        {
            return type.Methods
                .Where(x => x.IsPublic && x.IsProperty() && x.IsGetter)
                .Where(x => !x.IsStatic)
                .Select(x =>
                    {
                        var result = new Tuple<MethodDefinition, MethodDefinition>(x, type.Methods
                            .SingleOrDefault(m =>
                                m.IsSetter &&
                                    m.IsProperty() &&
                                    m.Name != null &&
                                    m.Name.Equals("set_" + x.Name.Substring(4, x.Name.Length - 4))));

                        return result;
                    })
                .Where(x => null != x.Item2);
        }

        public static IEnumerable<FieldDefinition> PrivateFields(this TypeDefinition type)
        {
            return type.Fields.Where(x => !x.IsPublic) // && !x.Name.Contains("__BackingField"))
                .Where(x => !x.IsStatic);
        }

        public static IEnumerable<Tuple<MethodDefinition, MethodDefinition>> PrivateProperties(
            this TypeDefinition type, List<FieldDefinition> backingFields)
        {
            return type.Methods
                .Where(x => !x.IsPublic && x.IsProperty() && x.IsGetter)
                .Where(x => !x.IsStatic)
                .Select(x =>
                    {
                        var result = new Tuple<MethodDefinition, MethodDefinition>(x, type.Methods
                            .SingleOrDefault(m =>
                                m.IsSetter &&
                                    m.IsProperty() &&
                                    m.Name != null &&
                                    m.Name.Equals("set_" + x.Name.Substring(4, x.Name.Length - 4))));

                        var backingField = result.Item2.PropertiesBackingField();
                        if (!backingFields.Any(uf => uf.FullName.Equals(backingField.FullName)))
                            result = new Tuple<MethodDefinition, MethodDefinition>(result.Item1, null);

                        return result;
                    })
                .Where(x => null != x.Item2);
        }

        public static IEnumerable<Tuple<MethodDefinition, MethodDefinition>> PrivateProperties(
            this TypeDefinition type)
        {
            return type.Methods
                .Where(x => !x.IsPublic && x.IsProperty() && x.IsGetter)
                .Where(x => !x.IsStatic)
                .Select(x =>
                    {
                        var result = new Tuple<MethodDefinition, MethodDefinition>(x, type.Methods
                            .SingleOrDefault(m =>
                                m.IsSetter &&
                                    m.IsProperty() &&
                                    m.Name != null &&
                                    m.Name.Equals("set_" + x.Name.Substring(4, x.Name.Length - 4))));

                        result.Item2.PropertiesBackingField();

                        return result;
                    })
                .Where(x => null != x.Item2);
        }

        public static IEnumerable<FieldDefinition> PublicBackingFields(this TypeDefinition type)
        {
            return type.Fields
                .Where(x =>
                    {
                        if (x.IsPublic || !x.Name.Contains("__BackingField"))
                            return false;

                        var singleOrDefault = type.Methods
                            .SingleOrDefault(m => m.IsProperty() && m.IsGetter);
                        return null != singleOrDefault && singleOrDefault.IsPublic;
                    });
        }

        public static IEnumerable<FieldDefinition> PrivateBackingFields(this TypeDefinition type)
        {
            return type.Fields
                .Where(x =>
                    {
                        if (x.IsPublic || !x.Name.Contains("__BackingField"))
                            return false;

                        var singleOrDefault = type.Methods
                            .SingleOrDefault(m => m.IsProperty() && m.IsGetter);
                        return null != singleOrDefault && !singleOrDefault.IsPublic;
                    });
        }
    }
}