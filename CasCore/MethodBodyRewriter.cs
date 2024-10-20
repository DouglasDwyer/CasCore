using DouglasDwyer.CasCore;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CasCore;

internal class MethodBodyRewriter
{
    public Instruction? Instruction { get; private set; }
    public MethodDefinition Method { get; private set; }

    private List<Instruction> _newInstructions;
    private Instruction?[] _offsetMap;

    private int _advancePosition;
    private int _copyPosition;
    private int _newPosition;

    public MethodBodyRewriter(ImportedReferences references)
    {
        Method = new MethodDefinition("", new MethodAttributes(), references.VoidType);
        _newInstructions = new List<Instruction>();
        _offsetMap = Array.Empty<Instruction?>();
    }

    public void Start(MethodDefinition method)
    {
        Method = method;
        _newInstructions.Clear();
        _newInstructions.EnsureCapacity(2 * Method.Body.Instructions.Count);
        
        if (_offsetMap.Length < Method.Body.CodeSize)
        {
            _offsetMap = new Instruction?[Method.Body.CodeSize];
        }

        _advancePosition = 0;
        _copyPosition = 0;
        _newPosition = 0;

        Advance(false);
    }

    public void Advance(bool addOriginal)
    {
        ProcessInstructionsToAdvance(addOriginal);
        SetNextInstruction();
        _newPosition = _newInstructions.Count;
    }

    public void Insert(Instruction instruction)
    {
        instruction.Offset = int.MaxValue;
        _newInstructions.Add(instruction);
    }

    public void Finish()
    {
        Method.Body.Instructions.Clear();
        
        foreach (var instruction in _newInstructions)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineBrTarget)
            {
                instruction.Operand = GetNewBranchTarget((Instruction)instruction.Operand!);
            }

            Method.Body.Instructions.Add(instruction);
        }

        foreach (var handler in Method.Body.ExceptionHandlers)
        {
            handler.FilterStart = GetNewBranchTarget(handler.FilterStart);
            handler.HandlerStart = GetNewBranchTarget(handler.HandlerStart);
            handler.TryStart = GetNewBranchTarget(handler.TryStart);
        }
    }

    private Instruction? GetNewBranchTarget(Instruction? instruction)
    {
        if (instruction is null)
        {
            return null;
        }
        else if (instruction.Offset == int.MaxValue)
        {
            return instruction;
        }
        else
        {
            return _offsetMap[instruction.Offset];
        }
    }

    private void ProcessInstructionsToAdvance(bool addOriginal)
    {
        var branchTargetInstruction = _newPosition == _newInstructions.Count ? Method.Body.Instructions[_copyPosition] : _newInstructions[_newPosition];

        while (_copyPosition < _advancePosition)
        {
            var oldInstruction = Method.Body.Instructions[_copyPosition];
            SimplifyMacro(oldInstruction);
            _offsetMap[oldInstruction.Offset] = branchTargetInstruction;
            
            if (addOriginal)
            {
                _newInstructions.Add(oldInstruction);
            }
            
            _copyPosition += 1;
        }
    }

    private void SetNextInstruction()
    {
        while (_advancePosition < Method.Body.Instructions.Count
            && Method.Body.Instructions[_advancePosition].OpCode.OpCodeType == OpCodeType.Prefix)
        {
            _advancePosition++;
        }

        if (_advancePosition < Method.Body.Instructions.Count)
        {
            Instruction = Method.Body.Instructions[_advancePosition];
        }
        else
        {
            Instruction = null;
        }

        _advancePosition++;
    }

    private void SimplifyMacro(Instruction instruction)
    {
        switch (instruction.OpCode.Code)
        {
            case Code.Ldloc_0:
                ExpandMacro(instruction, OpCodes.Ldloc, Method.Body.Variables[0]);
                break;
            case Code.Ldloc_1:
                ExpandMacro(instruction, OpCodes.Ldloc, Method.Body.Variables[1]);
                break;
            case Code.Ldloc_2:
                ExpandMacro(instruction, OpCodes.Ldloc, Method.Body.Variables[2]);
                break;
            case Code.Ldloc_3:
                ExpandMacro(instruction, OpCodes.Ldloc, Method.Body.Variables[3]);
                break;
            case Code.Stloc_0:
                ExpandMacro(instruction, OpCodes.Stloc, Method.Body.Variables[0]);
                break;
            case Code.Stloc_1:
                ExpandMacro(instruction, OpCodes.Stloc, Method.Body.Variables[1]);
                break;
            case Code.Stloc_2:
                ExpandMacro(instruction, OpCodes.Stloc, Method.Body.Variables[2]);
                break;
            case Code.Stloc_3:
                ExpandMacro(instruction, OpCodes.Stloc, Method.Body.Variables[3]);
                break;
            case Code.Ldarg_S:
                instruction.OpCode = OpCodes.Ldarg;
                break;
            case Code.Ldarga_S:
                instruction.OpCode = OpCodes.Ldarga;
                break;
            case Code.Starg_S:
                instruction.OpCode = OpCodes.Starg;
                break;
            case Code.Ldloc_S:
                instruction.OpCode = OpCodes.Ldloc;
                break;
            case Code.Ldloca_S:
                instruction.OpCode = OpCodes.Ldloca;
                break;
            case Code.Stloc_S:
                instruction.OpCode = OpCodes.Stloc;
                break;
            case Code.Br_S:
                instruction.OpCode = OpCodes.Br;
                break;
            case Code.Brfalse_S:
                instruction.OpCode = OpCodes.Brfalse;
                break;
            case Code.Brtrue_S:
                instruction.OpCode = OpCodes.Brtrue;
                break;
            case Code.Beq_S:
                instruction.OpCode = OpCodes.Beq;
                break;
            case Code.Bge_S:
                instruction.OpCode = OpCodes.Bge;
                break;
            case Code.Bgt_S:
                instruction.OpCode = OpCodes.Bgt;
                break;
            case Code.Ble_S:
                instruction.OpCode = OpCodes.Ble;
                break;
            case Code.Blt_S:
                instruction.OpCode = OpCodes.Blt;
                break;
            case Code.Bne_Un_S:
                instruction.OpCode = OpCodes.Bne_Un;
                break;
            case Code.Bge_Un_S:
                instruction.OpCode = OpCodes.Bge_Un;
                break;
            case Code.Bgt_Un_S:
                instruction.OpCode = OpCodes.Bgt_Un;
                break;
            case Code.Ble_Un_S:
                instruction.OpCode = OpCodes.Ble_Un;
                break;
            case Code.Blt_Un_S:
                instruction.OpCode = OpCodes.Blt_Un;
                break;
            case Code.Leave_S:
                instruction.OpCode = OpCodes.Leave;
                break;
        }
    }

    private static void ExpandMacro(Instruction instruction, OpCode opcode, object operand)
    {
        instruction.OpCode = opcode;
        instruction.Operand = operand;
    }
}