using System.Numerics;
using Newtonsoft.Json;

namespace ASM_Toolkit.Flowchart;

public static class AsmSaverLoader
{
	public class SavedInstruction
	{
		public Statement.Operator? Op { get; set; }
		public uint? Constant { get; set; }
		public string? RegisterName { get; set; }
	}

	private class SavedAftermathCondition
	{
		public List<SavedInstruction> Condition { get; }
		public List<(string, List<SavedInstruction>)> ConditionTrueStatements { get; }
		public List<(string, List<SavedInstruction>)> ConditionFalseStatements { get; }
		public string NextStateTrue { get; set; }
		public string NextStateFalse { get; set; }

		public SavedAftermathCondition()
		{
			Condition = new List<SavedInstruction>();
			ConditionFalseStatements = new List<(string, List<SavedInstruction>)>();
			ConditionTrueStatements = new List<(string, List<SavedInstruction>)>();
			NextStateFalse = "";
			NextStateTrue = "";
		}

		public SavedAftermathCondition(AsmBlock.AftermathCondition aftermath)
		{
			Condition = aftermath.Condition.ToSavedInstructions();
			ConditionTrueStatements = MapFromStatements(aftermath.ConditionTrueStatements);
			ConditionFalseStatements = MapFromStatements(aftermath.ConditionFalseStatements);
			NextStateFalse = aftermath.NextStateFalse;
			NextStateTrue = aftermath.NextStateTrue;
		}
	}

	private class SavedAsmBlock
	{
		public string? AftermathJump { get; set; }
		public SavedAftermathCondition? AftermathCondition { get; set; }
		public List<(string, List<SavedInstruction>)> Statements { get; set; }

		public SavedAsmBlock()
		{
			Statements = new List<(string, List<SavedInstruction>)>();
		}
	}

	private class SavedAsmChart
	{
		public Dictionary<string, int> Inputs { get; set; }
		public Dictionary<string, int> Outputs { get; set; }
		public Dictionary<string, int> Registers { get; set; }
		public Dictionary<string, BigInteger> InitialValues { get; }
		public Dictionary<string, SavedAsmBlock> States { get; }

		public string? FirstStateName { get; set; }

		public SavedAsmChart()
		{
			Inputs = new Dictionary<string, int>();
			Outputs = new Dictionary<string, int>();
			Registers = new Dictionary<string, int>();
			InitialValues = new Dictionary<string, BigInteger>();
			States = new Dictionary<string, SavedAsmBlock>();
		}
	}

	/// <summary>
	/// This function will save a ASM chart as json
	/// </summary>
	/// <param name="asm">The asm to save</param>
	/// <returns>The json text</returns>
	public static string SaveToJson(Asm asm)
	{
		SavedAsmChart savedAsmChart = new()
		{
			// Copy simple stuff
			Inputs = MapFromRegisterDictionaryToRegisterSize(asm.Inputs),
			Outputs = MapFromRegisterDictionaryToRegisterSize(asm.Outputs),
			Registers = MapFromRegisterDictionaryToRegisterSize(asm.Registers)
		};
		// Copy the initial variables
		foreach ((string registerName, Register reg) in asm.InitialValues)
			savedAsmChart.InitialValues[registerName] = reg.Number;
		// Copy first state
		foreach ((string stateName, AsmBlock block) in asm.States)
			if (block == asm.GetFirstState)
			{
				savedAsmChart.FirstStateName = stateName;
				break;
			}

		// Copy each state
		foreach ((string stateName, AsmBlock state) in asm.States)
		{
			SavedAsmBlock savedBlock = new()
			{
				Statements = MapFromStatements(state.Statements)
			};
			switch (state.AftermathOfBlock)
			{
				case AsmBlock.AftermathJump jump:
					savedBlock.AftermathJump = jump.StateName;
					break;
				case AsmBlock.AftermathCondition condition:
					savedBlock.AftermathCondition = new SavedAftermathCondition(condition);
					break;
			}

			// Add to save
			savedAsmChart.States[stateName] = savedBlock;
		}

		// Convert to json
		return JsonConvert.SerializeObject(savedAsmChart);
	}

	/// <summary>
	/// Loads an ASM chart from json string
	/// </summary>
	/// <param name="json">The ASM chart to load in json</param>
	/// <returns>The ASM chart</returns>
	/// <exception cref="JsonException">If there was a problem parsing json</exception>
	public static Asm LoadFromJson(string json)
	{
		var savedAsmChart = JsonConvert.DeserializeObject<SavedAsmChart>(json);
		if (savedAsmChart == null)
			throw new JsonException("Data must not be null");
		// Create the asm chart
		Asm asm = new();
		// Load the registers
		MapFromRegisterSizeToRegisterDictionary(savedAsmChart.Inputs, asm.Inputs);
		MapFromRegisterSizeToRegisterDictionary(savedAsmChart.Outputs, asm.Outputs);
		MapFromRegisterSizeToRegisterDictionary(savedAsmChart.Registers, asm.Registers);
		// Load initial values
		foreach ((string name, BigInteger initialValue) in savedAsmChart.InitialValues)
		{
			asm.InitialValues[name] = new Register(asm.MergedVariables[name].Length);
			asm.InitialValues[name].Set(initialValue);
		}

		// Load states
		foreach ((string stateName, SavedAsmBlock savedBlock) in savedAsmChart.States)
		{
			AsmBlock block = new();
			// Set the instructions
			foreach ((string destinationRegisterName, var instructions) in savedBlock.Statements)
				block.Statements.Add((destinationRegisterName, MapFromSavedInstructionToStatement(instructions)));
			// Set the aftermath
			if (savedBlock.AftermathJump != null)
				block.AftermathOfBlock = new AsmBlock.AftermathJump(savedBlock.AftermathJump);
			else if (savedBlock.AftermathCondition != null)
			{
				// Map statements
				List<(string, Statement)> conditionFalseStatements = new(), conditionTrueStatements = new();
				foreach ((string destinationRegisterName, var instructions) in savedBlock.AftermathCondition
					         .ConditionFalseStatements)
					conditionFalseStatements.Add((destinationRegisterName,
						MapFromSavedInstructionToStatement(instructions)));
				foreach ((string destinationRegisterName, var instructions) in savedBlock.AftermathCondition
					         .ConditionTrueStatements)
					conditionTrueStatements.Add((destinationRegisterName,
						MapFromSavedInstructionToStatement(instructions)));
				// Create the block itself
				block.AftermathOfBlock = new AsmBlock.AftermathCondition(
					MapFromSavedInstructionToStatement(savedBlock.AftermathCondition.Condition),
					conditionFalseStatements,
					conditionTrueStatements,
					savedBlock.AftermathCondition.NextStateFalse,
					savedBlock.AftermathCondition.NextStateTrue
				);
			}

			// Set the state
			asm.States[stateName] = block;

			// Check initial state
			if (stateName == savedAsmChart.FirstStateName)
				asm.SetFirstState(stateName);
		}

		// Done
		return asm;
	}

	private static List<(string, List<SavedInstruction>)> MapFromStatements(IEnumerable<(string, Statement)> statements)
	{
		List<(string, List<SavedInstruction>)> result = new();
		foreach ((string str, Statement statement) in statements)
			result.Add((str, statement.ToSavedInstructions()));
		return result;
	}

	private static Dictionary<string, int> MapFromRegisterDictionaryToRegisterSize(
		IDictionary<string, Register> registers)
	{
		Dictionary<string, int> result = new(registers.Count);
		foreach ((string registerName, Register reg) in registers)
			result[registerName] = reg.Length;
		return result;
	}

	private static void MapFromRegisterSizeToRegisterDictionary(
		IDictionary<string, int> registersSizes,
		IDictionary<string, Register> registers)
	{
		foreach ((string registerName, int size) in registersSizes)
			registers[registerName] = new Register(size);
	}

	private static Statement MapFromSavedInstructionToStatement(IEnumerable<SavedInstruction> savedInstructions)
	{
		List<Statement.Instruction> instructions = new();
		foreach (SavedInstruction savedInstruction in savedInstructions)
		{
			if (savedInstruction.Op != null)
				instructions.Add(new Statement.InstructionOperator(savedInstruction.Op.Value));
			else if (savedInstruction.Constant != null)
				instructions.Add(new Statement.InstructionConstant(savedInstruction.Constant.Value));
			else if (savedInstruction.RegisterName != null)
				instructions.Add(new Statement.InstructionRegister(savedInstruction.RegisterName));
		}

		return new Statement(instructions);
	}
}