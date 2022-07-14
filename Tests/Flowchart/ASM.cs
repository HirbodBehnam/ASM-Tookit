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
}