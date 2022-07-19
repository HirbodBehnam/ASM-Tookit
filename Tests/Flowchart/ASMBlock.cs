using System;
using System.Collections.Generic;
using ASM_Toolkit;
using NUnit.Framework;

namespace Tests.Flowchart;

public class AsmBlockTest
{
	[Test]
	public void SimpleDirectJumpTest()
	{
		// Initialize registers
		Register reg1 = new(32), reg2 = new(32), reg3 = new(32);
		Dictionary<string, Register> registers = new()
			{{nameof(reg1), reg1}, {nameof(reg2), reg2}, {nameof(reg3), reg3}};
		const uint reg1Initial = 1234, reg2Initial = 4321;
		reg1.Set(reg1Initial);
		reg2.Set(reg2Initial);
		// Initial the block
		AsmBlock block = new();
		block.Statements.Add((nameof(reg3), new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.Mult)
		}))); // reg3 = reg1 * reg2
		block.Statements.Add((nameof(reg2), new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionConstant(10),
			new Statement.InstructionOperator(Statement.Operator.Mod)
		}))); // reg2 = reg2 % 10
		block.Statements.Add((nameof(reg1), new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionConstant(10000),
			new Statement.InstructionOperator(Statement.Operator.CompareLessThan)
		}))); // reg1 = reg1 < 10000
		block.Statements.Add((nameof(reg3), new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg3)),
			new Statement.InstructionConstant(100),
			new Statement.InstructionOperator(Statement.Operator.Div)
		}))); // reg3 = reg3 / 100
		// Set the jump
		const string nextStateName = "next state";
		block.AftermathOfBlock = new AsmBlock.AftermathJump(nextStateName);
		// Execute the block
		string nextStateNameGot = block.ExecuteBlock(registers);
		// Check next state
		Assert.AreEqual(nextStateName, nextStateNameGot);
		// Check values
		Assert.AreEqual((uint) reg1, Convert.ToUInt32(reg1Initial < 10000));
		Assert.AreEqual((uint) reg2, reg2Initial % 10);
		Assert.AreEqual((uint) reg3, (reg1Initial * reg2Initial) / 100);
	}

	[Test]
	public void ConditionalJumpTest()
	{
		// Initialize registers
		Register reg1 = new(32), reg2 = new(32);
		Dictionary<string, Register> registers = new()
			{{nameof(reg1), reg1}, {nameof(reg2), reg2}};
		const uint reg1Initial = 1234, reg2Initial = 4321;
		reg1.Set(reg1Initial);
		reg2.Set(reg2Initial);
		// Create the block with empty statements
		AsmBlock block = new();
		// Main condition: reg1 + reg2 = actual value of them
		Statement condition = new(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.Add),
			new Statement.InstructionConstant(reg1Initial + reg2Initial),
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		});
		const string conditionTrueName = "true", conditionFalseName = "false";
		block.AftermathOfBlock =
			new AsmBlock.AftermathCondition(condition, null, null, conditionFalseName, conditionTrueName);
		// Check if the condition is true
		Assert.AreEqual(conditionTrueName, block.ExecuteBlock(registers));
		// Condition with register edit
		condition = new Statement(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(nameof(reg1)),
			new Statement.InstructionRegister(nameof(reg2)),
			new Statement.InstructionOperator(Statement.Operator.Add),
			new Statement.InstructionConstant(reg1Initial + reg2Initial + 1), // makes it false
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		});
		List<(string, Statement)> conditionTrueStatements = new()
		{
			(nameof(reg1), new Statement(
				new Statement.Instruction[]
				{
					new Statement.InstructionConstant(1)
				}))
		};
		List<(string, Statement)> conditionFalseStatements = new()
		{
			(nameof(reg2), new Statement(
				new Statement.Instruction[]
				{
					new Statement.InstructionConstant(1)
				}))
		};

		block.AftermathOfBlock =
			new AsmBlock.AftermathCondition(condition, conditionFalseStatements, conditionTrueStatements,
				conditionFalseName, conditionTrueName);
		// Check next statement and registers
		Assert.AreEqual(conditionFalseName, block.ExecuteBlock(registers));
		Assert.AreEqual(1, (ulong) reg2);
		Assert.AreNotEqual(1, (ulong) reg1);
	}
}