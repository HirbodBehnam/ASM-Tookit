namespace ASM_Toolkit;

public class Statement
{
	private readonly IEnumerable<Instruction> _instructions;

	public abstract class Instruction
	{
		// Empty class
	}

	public class InstructionOperator : Instruction
	{
		public Operator Op { get; }

		public InstructionOperator(Operator op)
		{
			this.Op = op;
		}

		public override bool Equals(object? obj)
		{
			// Check for null and compare run-time types.
			if (obj == null || GetType() != obj.GetType())
				return false;
			return Op == ((InstructionOperator) obj).Op;
		}

		public override int GetHashCode()
		{
			return (int) Op;
		}
	}

	public enum Operator
	{
		UnaryOr,
		UnaryAnd,
		UnaryXor,
		UnaryNot,
		UnaryNegate,
		Add,
		Sub,
		Mult,
		Div,
		Mod,
		Or,
		And,
		Xor,
		CompareLessThan,
		CompareLessThanEqual,
		CompareGreaterThan,
		CompareGreaterThanEqual,
		CompareEqual,
		CompareNotEqual,
	}

	public class InstructionConstant : Instruction
	{
		public uint Constant { get; }

		public InstructionConstant(uint constant)
		{
			Constant = constant;
		}

		public override bool Equals(object? obj)
		{
			// Check for null and compare run-time types.
			if (obj == null || GetType() != obj.GetType())
				return false;
			return Constant == ((InstructionConstant) obj).Constant;
		}

		public override int GetHashCode()
		{
			return (int) Constant;
		}

		public override string ToString()
		{
			return Constant.ToString();
		}
	}

	public class InstructionRegister : Instruction
	{
		public string Name { get; }

		public InstructionRegister(string registerName)
		{
			Name = registerName;
		}

		public override bool Equals(object? obj)
		{
			// Check for null and compare run-time types.
			if (obj == null || GetType() != obj.GetType())
				return false;
			return Name == ((InstructionRegister) obj).Name;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public Statement(IEnumerable<Instruction> instructions)
	{
		_instructions = instructions;
	}

	/// <summary>
	/// Evaluate will execute the expression on a list of registers and returns the result 
	/// </summary>
	/// <param name="registers">The list of registers which might be used</param>
	/// <param name="finalRegisterSize">The result register size<br/>
	/// If this is less than or equal zero the biggest register size will be chosen</param>
	/// <returns>The evaluated result</returns>
	/// <exception cref="RegisterNotFoundException">If the resister needed in statements could not be found in <see cref="registers"/></exception>
	/// <exception cref="ArgumentOutOfRangeException">If the operation in registers is invalid</exception>
	public Register Evaluate(Dictionary<string, Register> registers, int finalRegisterSize = 0)
	{
		// Evaluate final register size if need
		if (finalRegisterSize <= 0)
		{
			foreach (Instruction instruction in _instructions)
			{
				var currentSize = 0;
				switch (instruction)
				{
					case InstructionConstant:
						currentSize = 32;
						break;
					case InstructionRegister reg:
					{
						// Check if the register exists and then push it into registers
						if (!registers.TryGetValue(reg.Name, out Register? register))
							throw new RegisterNotFoundException(reg.Name);
						currentSize = register.Length;
						break;
					}
				}

				finalRegisterSize = Math.Max(finalRegisterSize, currentSize);
			}
		}

		// Just like https://www.geeksforgeeks.org/stack-set-4-evaluation-postfix-expression/
		Stack<Register> regs = new();
		foreach (Instruction instruction in _instructions)
		{
			switch (instruction)
			{
				case InstructionConstant c:
				{
					// Convert to reg and push
					Register reg = new(finalRegisterSize);
					reg.Set(c.Constant);
					regs.Push(reg);
					break;
				}
				case InstructionRegister reg:
				{
					// Check if the register exists and then push it into registers
					// Needs to be checked if the finalRegisterSize is non zero
					if (!registers.TryGetValue(reg.Name, out Register? sourceRegister))
						throw new RegisterNotFoundException(reg.Name);
					// No need to clone
					Register register = new(finalRegisterSize);
					register.Set(sourceRegister);
					regs.Push(register);
					break;
				}
				case InstructionOperator o:
				{
					// Pop the first operand because we might get unary operator in stack
					Register operand = regs.Pop();
					Register result = o.Op switch
					{
						// Unary
						Operator.UnaryOr => (Register) operand.Or(),
						Operator.UnaryAnd => (Register) operand.And(),
						Operator.UnaryXor => (Register) operand.Xor(),
						Operator.UnaryNot => ~operand,
						Operator.UnaryNegate => -operand,
						// Arithmetic
						Operator.Add => regs.Pop() + operand,
						Operator.Sub => regs.Pop() - operand,
						Operator.Mult => regs.Pop() * operand,
						Operator.Div => regs.Pop() / operand,
						Operator.Mod => regs.Pop() % operand,
						// Bit
						Operator.Or => regs.Pop() | operand,
						Operator.And => regs.Pop() & operand,
						Operator.Xor => regs.Pop() ^ operand,
						// Compare
						Operator.CompareLessThan => (Register) (regs.Pop() < operand),
						Operator.CompareLessThanEqual => (Register) (regs.Pop() <= operand),
						Operator.CompareGreaterThan => (Register) (regs.Pop() >= operand),
						Operator.CompareGreaterThanEqual => (Register) (regs.Pop() >= operand),
						Operator.CompareEqual => (Register) (regs.Pop() == operand),
						Operator.CompareNotEqual => (Register) (regs.Pop() != operand),
						// Should never happen
						_ => throw new ArgumentOutOfRangeException(nameof(o.Op), "invalid operator")
					};
					// Check bit registers
					if (result.Length == 1)
					{
						// ... and expand them to match the result size!
						// Note: Other operations will result in register size equal to finalRegisterSize
						bool boolResult = result[0];
						result = new Register(finalRegisterSize)
						{
							[0] = boolResult
						};
					}

					regs.Push(result);
					break;
				}
			}
		}

		return regs.Pop();
	}

	/// <summary>
	/// This method checks if a register is used in the expression
	/// </summary>
	/// <param name="registerName">The register name to check</param>
	/// <returns>True if used otherwise false</returns>
	public bool UsedRegister(string registerName)
	{
		foreach (Instruction instruction in _instructions)
			if (instruction is InstructionRegister register && register.Name == registerName)
				return true;

		return false;
	}

	/// <summary>
	/// This method will convert the instructions in this statement to a list of <see cref="AsmSaverLoader.SavedInstruction"/>
	/// and enables the easy save of them
	/// </summary>
	/// <returns>A list of <see cref="AsmSaverLoader.SavedInstruction"/></returns>
	public List<AsmSaverLoader.SavedInstruction> ToSavedInstructions()
	{
		List<AsmSaverLoader.SavedInstruction> result = new();
		foreach (Instruction instruction in _instructions)
		{
			switch (instruction)
			{
				case InstructionOperator op:
					result.Add(new AsmSaverLoader.SavedInstruction
					{
						Op = op.Op,
					});
					break;
				case InstructionRegister reg:
					result.Add(new AsmSaverLoader.SavedInstruction
					{
						RegisterName = reg.Name,
					});
					break;
				case InstructionConstant c:
					result.Add(new AsmSaverLoader.SavedInstruction
					{
						Constant = c.Constant,
					});
					break;
			}
		}

		return result;
	}

	public override bool Equals(object? obj)
	{
		// Check for null and compare run-time types.
		if (obj == null || GetType() != obj.GetType())
			return false;
		// Get statements
		List<Instruction> statements1 = _instructions.ToList(), statements2 = ((Statement) obj)._instructions.ToList();
		if (statements1.Count != statements2.Count)
			return false;
		// Check each one
		return !statements1.Where((t, i) => !t.Equals(statements2[i])).Any();
	}

	public override int GetHashCode()
	{
		return _instructions.GetHashCode();
	}

	public override string ToString()
	{
		// https://www.geeksforgeeks.org/postfix-to-infix/
		Stack<string> stack = new();
		// Loop over all stuff
		foreach (Instruction instruction in _instructions)
		{
			switch (instruction)
			{
				case InstructionOperator op:
					string operand = stack.Pop();
					string toPush = op.Op switch
					{
						// Unary
						Operator.UnaryOr => $"Or({operand})",
						Operator.UnaryAnd => $"And({operand})",
						Operator.UnaryXor => $"Xor({operand})",
						Operator.UnaryNot => $"~{operand}",
						Operator.UnaryNegate => $"-{operand}",
						// Arithmetic
						Operator.Add => $"{stack.Pop()} + {operand}",
						Operator.Sub => $"{stack.Pop()} - {operand}",
						Operator.Mult => $"{stack.Pop()} * {operand}",
						Operator.Div => $"{stack.Pop()} / {operand}",
						Operator.Mod => $"{stack.Pop()} % {operand}",
						// Bit
						Operator.Or => $"{stack.Pop()} | {operand}",
						Operator.And => $"{stack.Pop()} & {operand}",
						Operator.Xor => $"{stack.Pop()} ^ {operand}",
						// Compare
						Operator.CompareLessThan => $"{stack.Pop()} < {operand}",
						Operator.CompareLessThanEqual => $"{stack.Pop()} <= {operand}",
						Operator.CompareGreaterThan => $"{stack.Pop()} > {operand}",
						Operator.CompareGreaterThanEqual => $"{stack.Pop()} >= {operand}",
						Operator.CompareEqual => $"{stack.Pop()} == {operand}",
						Operator.CompareNotEqual => $"{stack.Pop()} != {operand}",
						// Should never happen
						_ => throw new ArgumentOutOfRangeException(nameof(op.Op), "invalid operator")
					};
					stack.Push($"({toPush})");
					break;
				default:
					stack.Push(instruction.ToString()!);
					break;
			}
		}

		// Done
		return stack.Pop();
	}

	/// <summary>
	/// Converts the statement to RHS of a verilog assignment
	/// </summary>
	/// <returns>The verilog RHS</returns>
	internal string ToVerilog()
	{
		// Mostly like ToString
		// https://www.geeksforgeeks.org/postfix-to-infix/
		Stack<string> stack = new();
		// Loop over all stuff
		foreach (Instruction instruction in _instructions)
		{
			switch (instruction)
			{
				case InstructionOperator op:
					string operand = stack.Pop();
					string toPush = op.Op switch
					{
						// Unary
						Operator.UnaryOr => $"|{operand}",
						Operator.UnaryAnd => $"&{operand}",
						Operator.UnaryXor => $"^{operand}",
						Operator.UnaryNot => $"~{operand}",
						Operator.UnaryNegate => $"-{operand}",
						// Arithmetic
						Operator.Add => $"{stack.Pop()} + {operand}",
						Operator.Sub => $"{stack.Pop()} - {operand}",
						Operator.Mult => $"{stack.Pop()} * {operand}",
						Operator.Div => $"{stack.Pop()} / {operand}",
						Operator.Mod => $"{stack.Pop()} % {operand}",
						// Bit
						Operator.Or => $"{stack.Pop()} | {operand}",
						Operator.And => $"{stack.Pop()} & {operand}",
						Operator.Xor => $"{stack.Pop()} ^ {operand}",
						// Compare
						Operator.CompareLessThan => $"{stack.Pop()} < {operand}",
						Operator.CompareLessThanEqual => $"{stack.Pop()} <= {operand}",
						Operator.CompareGreaterThan => $"{stack.Pop()} > {operand}",
						Operator.CompareGreaterThanEqual => $"{stack.Pop()} >= {operand}",
						Operator.CompareEqual => $"{stack.Pop()} == {operand}",
						Operator.CompareNotEqual => $"{stack.Pop()} != {operand}",
						// Should never happen
						_ => throw new ArgumentOutOfRangeException(nameof(op.Op), "invalid operator")
					};
					stack.Push($"({toPush})");
					break;
				default:
					stack.Push(instruction.ToString()!);
					break;
			}
		}

		// Done
		return stack.Pop();
	}
}