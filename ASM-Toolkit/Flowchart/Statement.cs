namespace ASM_Toolkit.Flowchart;

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
	}

	public class InstructionRegister : Instruction
	{
		public string Name { get; }

		public InstructionRegister(string registerName)
		{
			Name = registerName;
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
}