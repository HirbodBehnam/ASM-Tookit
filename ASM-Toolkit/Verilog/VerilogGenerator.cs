using System.Text;

namespace ASM_Toolkit.Verilog;

public static class VerilogGenerator
{
	private const string CurrentStateRegisterName = "current_state", NextStateRegisterName = "next_state";

	/// <summary>
	/// Generates the verilog code from a <see cref="Asm"/> chart
	/// </summary>
	/// <param name="asm">The asm chart to create the verilog code</param>
	/// <returns>The asm chart verilog code as string</returns>
	/// <exception cref="ArgumentNullException">If <see cref="Asm.GetFirstState"/> is null or one of the aftermath blocks are null</exception>
	/// <exception cref="InvalidOperationException">If first state is invalid</exception>
	public static string ConvertToVerilogCode(Asm asm)
	{
		// Create the module info
		StringBuilder result = new();
		result.AppendLine("module MyModule (");
		result.AppendLine(GenerateInputOutputPorts(asm.Inputs, asm.Outputs));
		result.AppendLine(");");
		// Create the registers
		result.AppendLine(GenerateGeneralRegisters(asm.States.Count, asm.Registers));
		// Create the state dictionary
		var states = GenerateStateDictionary(asm.States.Keys);
		// Create the combinational block
		result.AppendLine(GenerateCombinationalBlock(asm, states));
		// Create the behavioral block
		if (asm.GetFirstState == null)
			throw new ArgumentNullException(nameof(asm.GetFirstState), "First state should not be null");
		int? first = null;
		foreach ((string stateName, AsmBlock state) in asm.States)
			if (state == asm.GetFirstState)
			{
				first = states[stateName];
				break;
			}

		if (first == null)
			throw new InvalidOperationException("The first state is invalid");
		result.AppendLine(GenerateBehavioralBlock(asm, first.Value));
		// Done
		result.AppendLine("endmodule");
		return result.ToString();
	}

	/// <summary>
	/// Generates the string for input outputs of the ASM chart<br/>
	/// This list always contains the clk and reset
	/// </summary>
	/// <param name="inputs">Input ports</param>
	/// <param name="outputs">Output ports</param>
	/// <returns>The string which must be placed in the module info</returns>
	private static string GenerateInputOutputPorts(IDictionary<string, Register> inputs,
		IDictionary<string, Register> outputs)
	{
		StringBuilder sb = new();
		sb.AppendLine("\tinput wire clk,");
		sb.AppendLine("\tinput wire reset,");
		foreach ((string name, Register register) in inputs.OrderBy(x => x.Key))
			sb.AppendLine($"\tinput wire [{register.Length - 1}:0] {name},");
		foreach ((string name, Register register) in outputs.OrderBy(x => x.Key))
			sb.AppendLine($"\toutput reg [{register.Length - 1}:0] {name},");
		// Remove the last ,
		while (sb[^1] != ',')
			sb.Length--;
		sb.Length--; // This one removes the ,
		return sb.ToString();
	}

	/// <summary>
	/// Generates the inside registers needed for module including the state register
	/// </summary>
	/// <param name="totalStates">Total number of states</param>
	/// <param name="generalRegisters">List of registers which are not input nor output</param>
	/// <returns>The general input registers text in verilog code</returns>
	private static string GenerateGeneralRegisters(int totalStates, IDictionary<string, Register> generalRegisters)
	{
		StringBuilder sb = new();
		sb.AppendLine(
			$"\treg [{(int) Math.Floor(Math.Log2(totalStates) + 1)}:0] {CurrentStateRegisterName}, {NextStateRegisterName};");
		sb.AppendLine("\t// General registers");
		foreach ((string name, Register register) in generalRegisters.OrderBy(x => x.Key))
			sb.AppendLine($"\toutput reg [{register.Length - 1}:0] {name};");
		return sb.ToString();
	}

	/// <summary>
	/// Generates the combinational block of the asm chart
	/// </summary>
	/// <param name="asm">The asm chart to generate the block for it</param>
	/// <param name="stateMapper">The dictionary of state names to a number which is used in verilog code</param>
	/// <returns>The verilog always block of combinational part</returns>
	private static string GenerateCombinationalBlock(Asm asm, IDictionary<string, int> stateMapper)
	{
		StringBuilder sb = new();
		sb.AppendLine("\t// Combinational part");
		sb.AppendLine("\talways @(*) begin");
		sb.AppendLine($"\t\tcase ({CurrentStateRegisterName})");
		foreach ((string stateName, int stateNumber) in stateMapper)
		{
			sb.AppendLine("\t\t" + stateNumber + ": begin");
			AsmBlock block = asm.States[stateName];
			sb.AppendLine(GenerateStateVerilog(block, stateMapper));
			sb.AppendLine("\t\tend"); // Switch end
		}

		sb.AppendLine("\t\tendcase"); // case end
		sb.AppendLine("\tend"); // always end
		return sb.ToString();
	}

	/// <summary>
	/// Generate the verilog code for a state
	/// </summary>
	/// <param name="block">The state to generate the block for</param>
	/// <param name="stateMapper">The dictionary of state names to a number which is used in verilog code</param>
	/// <returns>The verilog code for this block</returns>
	/// <exception cref="ArgumentNullException">If the aftermath of block is null</exception>
	private static string GenerateStateVerilog(AsmBlock block, IDictionary<string, int> stateMapper)
	{
		StringBuilder sb = new();
		// The main block
		foreach ((string destinationRegister, Statement statement) in block.Statements)
			sb.AppendLine($"\t\t\t{destinationRegister} = {statement.ToVerilog()};");
		// Aftermath
		if (block.AftermathOfBlock == null)
			throw new ArgumentNullException(nameof(block.AftermathOfBlock), "Cannot go to nowhere!");
		switch (block.AftermathOfBlock)
		{
			case AsmBlock.AftermathJump jump:
				sb.AppendLine($"\t\t\t{NextStateRegisterName} = {stateMapper[jump.StateName]};");
				break;
			case AsmBlock.AftermathCondition condition:
				sb.AppendLine($"\t\t\tif ({condition.Condition.ToVerilog()}) begin");
				// True box
				foreach ((string destinationRegister, Statement statement) in condition.ConditionTrueStatements)
					sb.AppendLine($"\t\t\t\t{destinationRegister} = {statement.ToVerilog()};");
				sb.AppendLine($"\t\t\t\t{NextStateRegisterName} = {stateMapper[condition.NextStateTrue]};");
				// --------
				sb.AppendLine("\t\t\tend else begin");
				// False box
				foreach ((string destinationRegister, Statement statement) in condition.ConditionFalseStatements)
					sb.AppendLine($"\t\t\t\t{destinationRegister} = {statement.ToVerilog()};");
				sb.AppendLine($"\t\t\t\t{NextStateRegisterName} = {stateMapper[condition.NextStateFalse]};");
				// Done
				sb.AppendLine("\t\t\tend");
				break;
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates the behavioral block of asm chart
	/// </summary>
	/// <param name="asm">The asm chart</param>
	/// <param name="firstStateNumber">The first state number</param>
	/// <returns>Verilog code of behavioral block</returns>
	private static string GenerateBehavioralBlock(Asm asm, int firstStateNumber)
	{
		StringBuilder sb = new();
		sb.AppendLine("\t// Behavioral part");
		sb.AppendLine("\talways @(posedge clk) begin");
		sb.AppendLine("\t\tif (reset) begin");
		sb.AppendLine(GenerateResetBlock(asm, firstStateNumber));
		sb.AppendLine("\t\tend else begin");
		sb.AppendLine($"\t\t\t{CurrentStateRegisterName} <= {NextStateRegisterName};");
		sb.AppendLine("\t\tend"); // if
		sb.AppendLine("\tend"); // always
		return sb.ToString();
	}

	/// <summary>
	/// Create the reset block used in behavioral block
	/// </summary>
	/// <param name="asm">The asm chart</param>
	/// <param name="firstStateNumber">The first state number</param>
	/// <returns>The verilog code</returns>
	private static string GenerateResetBlock(Asm asm, int firstStateNumber)
	{
		StringBuilder sb = new();
		var registersList = asm.Outputs.Keys.Concat(asm.Registers.Keys);
		foreach (string registerName in registersList)
		{
			if (asm.InitialValues.TryGetValue(registerName, out Register? value))
				sb.AppendLine($"\t\t\t{registerName} <= {value.Number};");
			else
				sb.AppendLine($"\t\t\t{registerName} <= 0;");
		}

		sb.AppendLine($"\t\t\t{CurrentStateRegisterName} <= {firstStateNumber};");
		return sb.ToString();
	}

	/// <summary>
	/// Generates a map from state name to state number used in verilog code
	/// </summary>
	/// <param name="stateNames">Names of states</param>
	/// <returns>The map</returns>
	private static Dictionary<string, int> GenerateStateDictionary(IEnumerable<string> stateNames)
	{
		Dictionary<string, int> result = new();
		var i = 0;
		foreach (string name in stateNames)
		{
			result[name] = i;
			i++;
		}

		return result;
	}
}