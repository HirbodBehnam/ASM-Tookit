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
		Assert.AreEqual(constant, (uint) statement.Evaluate(new Dictionary<string, Register>(), 100));
		Assert.AreEqual(constant & 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 1));
		Assert.AreEqual(constant & 0b11111, (uint) statement.Evaluate(new Dictionary<string, Register>(), 5));
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
		Assert.Catch<RegisterNotFoundException>(() => { statement.Evaluate(new Dictionary<string, Register>(), 100); });
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
		Assert.AreEqual(result, (uint) statement.Evaluate(registers, 32));
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
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 32));

		instructions = new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1234),
			new Statement.InstructionOperator(Statement.Operator.UnaryOr),
			new Statement.InstructionConstant(1234),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		statement = new Statement(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(new Dictionary<string, Register>(), 32));
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
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(registers, 32));

		instructions = new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionOperator(Statement.Operator.Add),
		};
		statement = new Statement(instructions);
		Assert.AreEqual(1234 + 1, (uint) statement.Evaluate(registers, 32));
	}

	[Test]
	public void ConditionTest()
	{
		// A test which is aimed to test conditional statements with unknown register size but 
		// one bit output
		Register reg1 = new(4), reg2 = new(16);
		reg1.Set(3);
		reg2.Set(1234);
		Dictionary<string, Register> registers = new() {{nameof(reg1), reg1}, {nameof(reg2), reg2}};
		// Statement 1: reg2 == reg1 + 1231
		IEnumerable<Statement.Instruction> instructions = new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1231),
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionOperator(Statement.Operator.Add),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		};
		Statement statement = new(instructions);
		Register result = statement.Evaluate(registers);
		// Check the length to be 32 (there is a constant)
		Assert.AreEqual(32, result.Length);
		Assert.AreEqual(true, (bool) result);
		// Next test: Must size must be increased
		reg2 = new Register(1024);
		reg2.Set(1234);
		registers = new Dictionary<string, Register> {{nameof(reg1), reg1}, {nameof(reg2), reg2}};
		instructions = new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1231),
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionOperator(Statement.Operator.Add),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		};
		statement = new Statement(instructions);
		result = statement.Evaluate(registers);
		// Check the length to be 1024 (largest register)
		Assert.AreEqual(1024, result.Length);
		Assert.AreEqual(true, (bool) result);
	}

	[Test]
	public void CheckEqual()
	{
		// Simple tests
		{
			Assert.AreEqual(new Statement.InstructionConstant(123), new Statement.InstructionConstant(123));
			Assert.AreNotEqual(new Statement.InstructionConstant(12), new Statement.InstructionConstant(123));
			Statement.Instruction instruction1 = new Statement.InstructionConstant(123);
			Assert.IsTrue(instruction1.Equals(new Statement.InstructionConstant(123)));
			instruction1 = new Statement.InstructionOperator(Statement.Operator.Add);
			Statement.Instruction instruction2 = new Statement.InstructionConstant(123);
			Assert.IsFalse(instruction1.Equals(instruction2));
			instruction2 = new Statement.InstructionRegister("hello");
			Assert.IsFalse(instruction1.Equals(instruction2));
		}
		// Statement test
		{
			Statement statement1 = new(new Statement.Instruction[]
			{
				new Statement.InstructionConstant(1231),
				new Statement.InstructionRegister("a"),
				new Statement.InstructionOperator(Statement.Operator.Add),
				new Statement.InstructionRegister("b"),
				new Statement.InstructionOperator(Statement.Operator.CompareEqual),
			});
			Statement statement2 = new(new Statement.Instruction[]
			{
				new Statement.InstructionConstant(1231),
				new Statement.InstructionRegister("a"),
				new Statement.InstructionOperator(Statement.Operator.Add),
				new Statement.InstructionRegister("b"),
				new Statement.InstructionOperator(Statement.Operator.CompareEqual),
			});
			Assert.AreEqual(statement1, statement2);
			Assert.IsTrue(statement1.Equals(statement2));
		}
		{
			Statement statement1 = new(new Statement.Instruction[]
			{
				new Statement.InstructionConstant(1231),
				new Statement.InstructionRegister("a"),
				new Statement.InstructionOperator(Statement.Operator.Add),
				new Statement.InstructionRegister("b"),
				new Statement.InstructionOperator(Statement.Operator.Add),
			});
			Statement statement2 = new(new Statement.Instruction[]
			{
				new Statement.InstructionConstant(1231),
				new Statement.InstructionRegister("a"),
				new Statement.InstructionOperator(Statement.Operator.Add),
				new Statement.InstructionRegister("b"),
				new Statement.InstructionOperator(Statement.Operator.CompareEqual),
			});
			Assert.AreNotEqual(statement1, statement2);
			Assert.IsFalse(statement1.Equals(statement2));
		}
	}

	[Test]
	public void ToStringTest()
	{
		var result = new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1231),
			new Statement.InstructionRegister("a"),
			new Statement.InstructionOperator(Statement.Operator.Add),
			new Statement.InstructionRegister("b"),
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		}).ToString();
		Assert.AreEqual("((1231 + a) == b)", result);
		result = new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionConstant(1231),
			new Statement.InstructionOperator(Statement.Operator.UnaryAnd),
			new Statement.InstructionRegister("a"),
			new Statement.InstructionOperator(Statement.Operator.Add),
		}).ToString();
		Assert.AreEqual("((And(1231)) + a)", result);
	}
}