using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace BB.DeepCopy
{
    public class CurrentData
    {
        public Stack<VariableDefinition> NewObjects;

        public Collection<ExceptionHandler> ExceptionHandlers;

        public Collection<VariableDefinition> Variables;

        public Collection<Instruction> Instructions;

        public Instruction Start;

        public Instruction End;

        public Collection<TypeDefinition> GenericParameters;

        public CurrentData()
        {
            this.NewObjects = new Stack<VariableDefinition>();
            this.GenericParameters = new Collection<TypeDefinition>();
        }
    }
}