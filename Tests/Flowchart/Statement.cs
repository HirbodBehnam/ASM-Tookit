using System.Collections.Generic;
using ASM_Toolkit.Flowchart;
using NUnit.Framework;

namespace Tests.Flowchart;

public class StatementTests
{
	[Test]
	public void ConstantTest()
	{
		const uint constant = 100;
		IEnumerable<Statement.Instruction> instructions = new[]
		{
			new Statement.InstructionConstant(constant),
		};
		Statement statement = new(instructions);
		Assert.AreEqual(constant, (uint) statement.Evaluate(new Dictionary<string, Register>(), 100).Number);
		Assert.AreEqual(constant & 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 1).Number);
		Assert.AreEqual(constant & 0b11111, (uint) statement.Evaluate(new Dictionary<string, Register>(), 5).Number);
	}

	[Test]
	public void RegisterTest()
	{
		const uint registerNumber = 100;
		const string registerName = "name";
		IEnumerable<Statement.Instruction> instructions = new[]
		{
			new Statement.InstructionRegister(registerName),
		};
		Statement statement = new(instructions);
		Dictionary<string, Register> registers = new() {{registerName, (Register) registerNumber}};
		Assert.AreEqual(registerNumber, (uint) statement.Evaluate(registers, 100).Number);
		Assert.Catch<Statement.RegisterNotFoundException>(() =>
		{
			statement.Evaluate(new Dictionary<string, Register>(), 100);
		});
	}

	[Test]
	public void ArithmeticTest()
	{
		const uint registerNumber = 100, constantNumber = 10, result = registerNumber + constantNumber;
		const string registerName = "name";
		IEnumerable<Statement.Instruction> instructions = new Statement.Instruction[]
		{
			new Statement.InstructionRegister(registerName),
			new Statement.InstructionConstant(constantNumber),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		Statement statement = new(instructions);
		Dictionary<string, Register> registers = new() {{registerName, (Register) registerNumber}};
		Assert.AreEqual(result, (uint) statement.Evaluate(registers, 32).Number);
	}

	[Test]
	public void ResizeTest1()
	{
		IEnumerable<Statement.Instruction> instructions = new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1234),
			new Statement.InstructionConstant(1234),
			new Statement.InstructionOperator(Statement.Operator.UnaryOr),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		Statement statement = new(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 32).Number);

		instructions = new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1234),
			new Statement.InstructionOperator(Statement.Operator.UnaryOr),
			new Statement.InstructionConstant(1234),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		statement = new Statement(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 32).Number);
	}

	[Test]
	public void ResizeTest2()
	{
		Register reg1 = new(1), reg2 = new(32);
		reg1[0] = true;
		reg2.Set(1234);
		Dictionary<string, Register> registers = new() {{nameof(reg1), reg1}, {nameof(reg2), reg2}};
		IEnumerable<Statement.Instruction> instructions = new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		Statement statement = new(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(registers, 32).Number);

		instructions = new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		statement = new Statement(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(registers, 32).Number);
	}
}