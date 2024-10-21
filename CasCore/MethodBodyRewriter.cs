using DouglasDwyer.CasCore;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CasCore;

/// <summary>
/// Handles efficiently rewriting the bodies of <see cref="MethodDefinition"/>s to include runtime checks for code access security.
/// </summary>
internal class MethodBodyRewriter
{
    /// <summary>
    /// The current instruction being rewritten, if any.
    /// </summary>
    public Instruction? Instruction { get; private set; }

    /// <summary>
    /// The current method being rewritten.
    /// </summary>
    public MethodDefinition Method { get; private set; }

    /// <summary>
    /// A list of the instructions to replace the method body with.
    /// </summary>
    private List<Instruction> _newInstructions;

    /// <summary>
    /// A mapping from old instruction offset to new instruction,
    /// for replacing branch targets.
    /// </summary>
    private Instruction?[] _offsetMap;

    /// <summary>
    /// The position of the current instruction to be replaced.
    /// </summary>
    private int _advancePosition;

    /// <summary>
    /// The position at which to start copying instructions when advancing to the next instruction.
    /// Usually, this is the same as <see cref="_advancePosition"/>. When the current instruction
    /// has prefixes (such as .constrain), though, this points to the first prefix rather than
    /// the main instruction.
    /// </summary>
    private int _copyPosition;

    /// <summary>
    /// The position at which the first new instruction was inserted.
    /// </summary>
    private int _newPosition;

    /// <summary>
    /// Creates a new, uninitialized rewriter.
    /// </summary>
    /// <param name="references">The references that the writer should use.</param>
    public MethodBodyRewriter(ImportedReferences references)
    {
        Method = new MethodDefinition("", new MethodAttributes(), references.VoidType);
        _newInstructions = new List<Instruction>();
        _offsetMap = Array.Empty<Instruction?>();
    }

    /// <summary>
    /// Initiates rewriting the provided method.
    /// </summary>
    /// <param name="method">The method to modify.</param>
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

    /// <summary>
    /// Advances to the next instruction in the method.
    /// </summary>
    /// <param name="addOriginal">Whether the original current instruction should be copied to the method's new body.</param>
    public void Advance(bool addOriginal)
    {
        ProcessInstructionsToAdvance(addOriginal);
        SetNextInstruction();
        _newPosition = _newInstructions.Count;
    }

    /// <summary>
    /// Adds an instruction before the current instruction.
    /// </summary>
    /// <param name="instruction">The new instruction to add.</param>
    public void Insert(Instruction instruction)
    {
        instruction.Offset = int.MaxValue;
        _newInstructions.Add(instruction);
    }

    /// <summary>
    /// Completes rewriting by copying the new instructions into the method body and updating branch targets.
    /// </summary>
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
            handler.HandlerEnd = GetNewBranchTarget(handler.HandlerEnd);
            handler.TryStart = GetNewBranchTarget(handler.TryStart);
            handler.TryEnd = GetNewBranchTarget(handler.TryEnd);
        }
    }

    /// <summary>
    /// Determines where any instructions that pointed to the given branch target
    /// should point after rewriting.
    /// </summary>
    /// <param name="instruction">The original branch target.</param>
    /// <returns>The new branch target.</returns>
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

    /// <summary>
    /// Advances the cursors and copies any instructions necessary from the original body.
    /// </summary>
    /// <param name="addOriginal">Whether to include instructions from the original body.</param>
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

    /// <summary>
    /// Moves to the next instruction.
    /// </summary>
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

    /// <summary>
    /// Expands any macro opcodes that may not be valid for long methods.
    /// </summary>
    /// <param name="instruction">The instruction to expand.</param>
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

    /// <summary>
    /// Updates the opcode and operand for the provided macro instruction.
    /// </summary>
    /// <param name="instruction">The instruction being updated.</param>
    /// <param name="opcode">The new opcode to use.</param>
    /// <param name="operand">The new operand to use.</param>
    private static void ExpandMacro(Instruction instruction, OpCode opcode, object operand)
    {
        instruction.OpCode = opcode;
        instruction.Operand = operand;
    }
}