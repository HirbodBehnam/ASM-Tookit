using System;
using System.Collections.Generic;
using System.Linq;
using ASM_Toolkit.Flowchart;
using NUnit.Framework;

namespace Tests.Flowchart;

public class AsmTest
{
	[Test]
	public void AsmChartTest()
	{
		Asm asm = new();
		Assert.IsNull(asm.CurrentState);
		// An asm chart which decrements a number until it's zero
		const string counterRegisterName = "counter", inputWireName = "input";
		const string loopStateName = "loop", doneStateName = "done";
		const uint inputWireNumber = 100;
		// The statement which decreases the counter
		Statement decreaserStatement = new(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(counterRegisterName),
			new Statement.InstructionConstant(1),
			new Statement.InstructionOperator(Statement.Operator.Add),
		});
		// The statement which checks the equality
		Statement equalChecker = new(new Statement.Instruction[]
		{
			new Statement.InstructionRegister(counterRegisterName),
			new Statement.InstructionRegister(inputWireName),
			new Statement.InstructionOperator(Statement.Operator.CompareEqual),
		});
		// The ASM block of main counter
		AsmBlock decreaserState = new();
		decreaserState.Statements.Add((counterRegisterName, decreaserStatement));
		decreaserState.AftermathOfBlock =
			new AsmBlock.AftermathCondition(equalChecker, null, null, loopStateName, doneStateName);
		// The asm block of done
		AsmBlock doneBlock = new()
		{
			AftermathOfBlock = new AsmBlock.AftermathJump(doneStateName)
		};
		// The asm chart
		Register inputRegister = new(32);
		inputRegister.Set(inputWireNumber);
		asm.Inputs[inputWireName] = inputRegister;
		asm.Outputs[counterRegisterName] = new Register(32);
		asm.Registers["dummy"] = new Register(1);
		asm.States[loopStateName] = decreaserState;
		asm.States[doneStateName] = doneBlock;
		asm.SetFirstState(loopStateName);
		Assert.AreEqual(decreaserState, asm.GetFirstState);
		// Reset the ASM chart
		asm.Reset();
		for (var i = 0; i < inputWireNumber - 1; i++)
			asm.Tick();
		Assert.AreEqual(decreaserState, asm.CurrentState);
		asm.Tick();
		Assert.AreEqual(inputWireNumber, asm.ClockCounter);
		Assert.AreEqual(doneBlock, asm.CurrentState);
		// With initial variables
		const uint initialCounterValue = 60;
		Register initialCounterRegister = new(32);
		initialCounterRegister.Set(initialCounterValue);
		asm.InitialValues[counterRegisterName] = initialCounterRegister;
		asm.Reset();
		Assert.AreEqual(initialCounterValue, (uint) asm.Outputs[counterRegisterName]);
		// Run for least amount of ticks
		for (var i = 0; i < inputWireNumber - initialCounterValue - 1; i++)
			asm.Tick();
		Assert.AreEqual(decreaserState, asm.CurrentState);
		asm.Tick();
		Assert.AreEqual(inputWireNumber - initialCounterValue, asm.ClockCounter);
		Assert.AreEqual(doneBlock, asm.CurrentState);
	}

	[Test]
	public void SaveLoadTest()
	{
		for (var i = 0; i < 100; i++)
		{
			Asm mainAsm = GenerateRandomAsmChart();
			Asm loadedAsm = AsmSaverLoader.LoadFromJson(AsmSaverLoader.SaveToJson(mainAsm));
			// Check if they are the same
			// Check registers
			Assert.AreEqual(mainAsm.Inputs.Count, loadedAsm.Inputs.Count);
			Assert.AreEqual(mainAsm.Outputs.Count, loadedAsm.Outputs.Count);
			Assert.AreEqual(mainAsm.Registers.Count, loadedAsm.Registers.Count);
			Assert.IsTrue(mainAsm.Inputs.Keys.All(loadedAsm.Inputs.ContainsKey));
			Assert.IsTrue(mainAsm.Outputs.Keys.All(loadedAsm.Outputs.ContainsKey));
			Assert.IsTrue(mainAsm.Registers.Keys.All(loadedAsm.Registers.ContainsKey));
			Assert.IsTrue(mainAsm.Inputs.All(x => loadedAsm.Inputs[x.Key].Length == x.Value.Length));
			Assert.IsTrue(mainAsm.Outputs.All(x => loadedAsm.Outputs[x.Key].Length == x.Value.Length));
			Assert.IsTrue(mainAsm.Registers.All(x => loadedAsm.Registers[x.Key].Length == x.Value.Length));
			// Check default values
			Assert.AreEqual(mainAsm.InitialValues.Count, loadedAsm.InitialValues.Count);
			Assert.IsTrue(mainAsm.InitialValues.Keys.All(loadedAsm.InitialValues.ContainsKey));
			Assert.IsTrue(mainAsm.InitialValues.All(x =>
				loadedAsm.InitialValues[x.Key].Length == x.Value.Length && loadedAsm.InitialValues[x.Key] == x.Value));
			// Check states
			Assert.AreEqual(mainAsm.States.Count, loadedAsm.States.Count);
			Assert.IsTrue(mainAsm.States.Keys.All(loadedAsm.States.ContainsKey));
			foreach ((string stateName, AsmBlock block) in mainAsm.States)
			{
				Assert.True(block.Statements.SequenceEqual(loadedAsm.States[stateName].Statements));
				Assert.AreEqual(block.AftermathOfBlock, loadedAsm.States[stateName].AftermathOfBlock);
			}
		}
	}

	private static Asm GenerateRandomAsmChart()
	{
		Random rng = new();
		Asm asm = new();
		// Create random registers
		for (var i = 0; i < rng.Next(3); i++)
			asm.Inputs[RandomHex(rng)] = new Register(rng.Next(32) + 1);
		for (var i = 0; i < rng.Next(3); i++)
			asm.Outputs[RandomHex(rng)] = new Register(rng.Next(32) + 1);
		for (var i = 0; i < rng.Next(3); i++)
			asm.Registers[RandomHex(rng)] = new Register(rng.Next(32) + 1);
		var editableRegisterNames = asm.Outputs.Keys.Concat(asm.Registers.Keys).ToList();
		// Create random initial variables
		if (editableRegisterNames.Count > 0)
		{
			for (var i = 0; i < rng.Next(2); i++)
			{
				string registerName = editableRegisterNames[rng.Next(editableRegisterNames.Count)];
				int registerSize = asm.MergedVariables[registerName].Length;
				asm.InitialValues[registerName] = new Register(registerSize);
				asm.InitialValues[registerName].Set((uint) rng.Next());
			}
		}

		// Create states
		for (var i = 0; i < rng.Next(5); i++)
		{
			string stateName = RandomHex(rng);
			AsmBlock state = new();
			// Random register names. They dont matter in save testing
			state.Statements.AddRange(RandomStatements(rng));
			// Aftermath also does not matter. We are just testing
			state.AftermathOfBlock = (rng.Next() % 3) switch
			{
				0 => new AsmBlock.AftermathJump(RandomHex(rng)),
				1 => new AsmBlock.AftermathCondition(
					RandomStatement(rng), 
					RandomStatements(rng), RandomStatements(rng),
					RandomHex(rng), RandomHex(rng)),
				_ => null
			};
			// Set the state
			asm.States[stateName] = state;
		}

		return asm;
	}

	/// <summary>
	/// This function will create a random hex string with length of 64 characters
	/// </summary>
	/// <param name="rng">The random generator</param>
	/// <returns>A random string</returns>
	private static string RandomHex(Random rng)
	{
		Span<byte> temp = stackalloc byte[32];
		rng.NextBytes(temp);
		return Convert.ToHexString(temp);
	}

	private static List<(string, Statement)> RandomStatements(Random rng)
	{
		List<(string, Statement)> result = new(rng.Next(5));
		for (var i = 0; i < result.Capacity; i++)
			result.Add((RandomHex(rng), RandomStatement(rng)));
		return result;
	}

	private static Statement RandomStatement(Random rng)
	{
		var operators = Enum.GetValues<Statement.Operator>();
		List<Statement.Instruction> result = new(rng.Next(10));
		for (var i = 0; i < result.Capacity; i++)
		{
			switch (rng.Next() % 3)
			{
				case 0:
					result.Add(new Statement.InstructionConstant((uint) rng.Next()));
					break;
				case 1:
					result.Add(new Statement.InstructionOperator(operators[rng.Next(operators.Length)]));
					break;
				case 2:
					result.Add(new Statement.InstructionRegister(RandomHex(rng)));
					break;
			}
		}

		return new Statement(result);
	}
}