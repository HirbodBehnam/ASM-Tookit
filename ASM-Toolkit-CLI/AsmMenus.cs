using System.Numerics;
using System.Text.RegularExpressions;
using ASM_Toolkit;
using ASM_Toolkit_CLI.Util;

namespace ASM_Toolkit_CLI;

public class AsmMenus
{
	private static readonly Regex RegisterNameRegex = new("^[a-zA-Z_][a-zA-Z0-9_]+$");
	private readonly Asm _asmChart;
	private string? _saveLocation;

	public AsmMenus(Asm asm, string? saveLocation)
	{
		_asmChart = asm;
		_saveLocation = saveLocation;
	}

	public AsmMenus() : this(new Asm(), null)
	{
	}

	/// <summary>
	/// Opens the main menu for editing this ASM chart
	/// </summary>
	public void MainMenu()
	{
		while (true)
		{
			Console.WriteLine("1. Save ASM");
			Console.WriteLine("2. Add/Modify/Remove input ports");
			Console.WriteLine("3. Add/Modify/Remove output registers");
			Console.WriteLine("4. Add/Modify/Remove general registers");
			Console.WriteLine("5. Add/Modify/Remove initial values");
			Console.WriteLine("6. Add/Modify/Remove state");
			Console.WriteLine("7. Set first state");
			Console.WriteLine("8. Enter simulation mode");
			Console.WriteLine("9. Generate verilog code");
			Console.WriteLine("0. Exit");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1': // Save
					if (_saveLocation == null)
					{
						Console.Write("Enter a filename to save the asm chart in it: ");
						_saveLocation = Console.ReadLine()!;
					}

					// Try to save
					var fileContent = "";
					try
					{
						fileContent = AsmSaverLoader.SaveToJson(_asmChart);
						File.WriteAllText(_saveLocation, fileContent);
						Console.WriteLine("Saved!");
					}
					catch (Exception ex)
					{
						Console.WriteLine("Cannot save the asm chart: " + ex.Message);
						Console.WriteLine("Here is the content of file:");
						Console.WriteLine(fileContent);
					}

					break;
				case '2': // Input ports
					PortsModificationMenu(_asmChart.Inputs, "input");
					break;
				case '3': // Output ports
					PortsModificationMenu(_asmChart.Outputs, "output");
					break;
				case '4': // Input ports
					PortsModificationMenu(_asmChart.Registers, "general register");
					break;
				case '5': // Initial values
					InitialValueModificationMenu();
					break;
				case '6': // State
					ModifyStateMenu();
					break;
				case '7': // First state
					Console.Write("Enter first state name: ");
					string firstStateName = Console.ReadLine()!;
					if (!_asmChart.States.ContainsKey(firstStateName))
					{
						Console.WriteLine("Invalid state name!");
						return;
					}

					_asmChart.SetFirstState(firstStateName);
					break;
				case '8':
					SimulateMenu();
					break;
				case '0': // Exit
					if (ConsoleUtils.InputKey("All unsaved changes will be discarded. Press y to exit: ") == 'y')
						return;
					Console.WriteLine("Going back...");
					break;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}


	private void PortsModificationMenu(IDictionary<string, Register> list, string portName)
	{
		while (true)
		{
			Console.WriteLine($"Modifying {portName} ports");
			Console.WriteLine("1. List all ports");
			Console.WriteLine("2. Add port");
			Console.WriteLine("3. Remove port");
			Console.WriteLine("4. Change register size");
			Console.WriteLine("0. Back");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1': // List all
					if (list.Count == 0)
						Console.WriteLine("No ports!");
					else
						foreach ((string registerName, Register register) in list)
							Console.WriteLine($"{registerName}: {register.Length} bits");
					break;
				case '2': // Add to port
				{
					string registerName = GetRegisterName();
					int registerSize = ConsoleUtils.GetPositiveInteger("Enter register size in bits: ");
					list[registerName] = new Register(registerSize);
					break;
				}
				case '3': // Remove
					RemoveRegister(list);
					break;
				case '4': // Change size
					ChangeRegisterSize(list);
					break;
				case '0': // Back
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}

	private void InitialValueModificationMenu()
	{
		Console.WriteLine("Note: All non defined initial values are zero.");
		while (true)
		{
			Console.WriteLine("1. List all initial values");
			Console.WriteLine("2. Remove initial value");
			Console.WriteLine("3. Set initial value");
			Console.WriteLine("0. Back");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1': // List all
					if (_asmChart.InitialValues.Count == 0)
						Console.WriteLine("All initial values are zero");
					else
						foreach ((string registerName, Register initialValue) in _asmChart.InitialValues)
							Console.WriteLine($"{registerName}: {initialValue}");
					break;
				case '2': // Remove
					Console.Write("Enter the register name to remove it's initial value: ");
					Console.WriteLine(_asmChart.InitialValues.Remove(Console.ReadLine()!)
						? "Initial value removed."
						: "Register does not exists!");
					break;
				case '3': // Set
				{
					Console.Write("Enter the register name to set it's default value: ");
					string registerName = Console.ReadLine()!;
					int? registerSize = GetRegisterSizeOfOutputGeneral(registerName);
					if (registerSize == null)
					{
						Console.WriteLine("Register not found in outputs or general registers.");
						continue;
					}

					// Get the number
					BigInteger initialValue = ConsoleUtils.GetPositiveBigNumber("Enter the initial value: ");
					Register initialRegister = new(registerSize.Value);
					initialRegister.Set(initialRegister);
					// Check truncation
					if (initialValue != initialRegister.Number)
						Console.WriteLine($"Number truncated to {initialRegister}");
					// Assign
					_asmChart.InitialValues[registerName] = initialRegister;
					break;
				}
				case '0': // Back
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}

	private void ModifyStateMenu()
	{
		while (true)
		{
			Console.WriteLine("1. List state names");
			Console.WriteLine("2. View state");
			Console.WriteLine("3. Add state");
			Console.WriteLine("4. Remove state");
			Console.WriteLine("5. Edit state");
			Console.WriteLine("0. Back");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1': // List state names
					if (_asmChart.States.Count == 0)
						Console.WriteLine("No states created yet!");
					else
						foreach (string stateName in _asmChart.States.Keys)
							Console.WriteLine(stateName);
					break;
				case '2': // View state
				{
					Console.Write("Enter state name: ");
					string stateName = Console.ReadLine()!;
					if (!_asmChart.States.TryGetValue(stateName, out AsmBlock? state))
					{
						Console.WriteLine("Cannot find the specified state.");
						break;
					}

					Console.WriteLine(state.ToString());
					break;
				}
				case '3': // Add
					AddState();
					break;
				case '4': // Remove
					RemoveState();
					break;
				case '5': // Edit
				{
					Console.Write("Enter state name: ");
					string stateName = Console.ReadLine()!;
					if (!_asmChart.States.TryGetValue(stateName, out AsmBlock? state))
					{
						Console.WriteLine("State does not exists.");
						break;
					}

					EditState(state);
					break;
				}
				case '0': // Back
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}

	private void EditState(AsmBlock state)
	{
		// Cache registers
		HashSet<string> editableRegisters = new(_asmChart.Outputs.Keys.Concat(_asmChart.Registers.Keys));
		HashSet<string> allRegisters = new(_asmChart.MergedVariables.Keys);
		while (true)
		{
			Console.WriteLine("1. Edit aftermath");
			Console.WriteLine("2. Add main block statements");
			Console.WriteLine("3. Remove main block statements");
			Console.WriteLine("0. Back");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1':
					state.AftermathOfBlock = GetAftermath(new HashSet<string>(_asmChart.States.Keys),
						editableRegisters, allRegisters);
					break;
				case '2':
					state.Statements.AddRange(ReadStatements(editableRegisters, allRegisters));
					break;
				case '3':
					for (var i = 0; i < state.Statements.Count; i++)
						Console.WriteLine($"{i + 1}. {state.Statements[i].Item1} = {state.Statements[i].Item2}");
					int chosenIndex = ConsoleUtils.GetPositiveInteger("Enter a number by it's index to remove: ");
					if (chosenIndex > state.Statements.Count || state.Statements.Count <= 0)
					{
						Console.WriteLine("Out of range!");
						break;
					}

					state.Statements.RemoveAt(chosenIndex - 1);
					Console.WriteLine("Removed");
					break;
				case '0':
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}

	private void SimulateMenu()
	{
		if (_asmChart.GetFirstState == null)
		{
			Console.WriteLine("First state is not set! Cannot simulate");
			return;
		}

		foreach ((string stateName, AsmBlock state) in _asmChart.States)
			if (state.AftermathOfBlock == null)
			{
				Console.WriteLine($"Empty aftermath in state {stateName}");
				return;
			}

		_asmChart.Reset(); // Reset before going into simulation
		while (true)
		{
			Console.WriteLine("Current clock number: " + _asmChart.ClockCounter);
			Console.WriteLine("Current state: " + _asmChart.CurrentStateName);
			Console.WriteLine("What do you want to do?");
			Console.WriteLine("1. List all registers with values");
			Console.WriteLine("2. Change input values");
			Console.WriteLine("3. Tick");
			Console.WriteLine("4. Multi Tick");
			Console.WriteLine("0. Back");
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1':
					Console.WriteLine("Inputs:");
					PrintRegisterValues(_asmChart.Inputs);
					Console.WriteLine("Outputs:");
					PrintRegisterValues(_asmChart.Outputs);
					Console.WriteLine("General Registers:");
					PrintRegisterValues(_asmChart.Registers);
					break;
				case '2':
					ChangeRegisterValue(_asmChart.Inputs);
					break;
				case '3':
					_asmChart.Tick();
					break;
				case '4':
					int tickCount = ConsoleUtils.GetPositiveInteger("How many ticks? ");
					for (var i = 0; i < tickCount; i++)
						_asmChart.Tick();
					break;
				case '0':
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}

	/// <summary>
	/// This method will try to get a register name from user<br/>
	/// The returned register name is a valid in terms of characters<br/>
	/// It also is a non existent register name
	/// </summary>
	/// <returns>A valid register name</returns>
	private string GetRegisterName()
	{
		while (true)
		{
			Console.Write("Enter register name: ");
			string registerName = Console.ReadLine()!;
			// Check the regex
			if (!RegisterNameRegex.IsMatch(registerName))
			{
				Console.WriteLine(
					"Invalid register name. First letter must be alphabet and then alphabet and numbers are allowed.");
				continue;
			}

			// Check if it exists
			if (_asmChart.MergedVariables.ContainsKey(registerName))
			{
				Console.WriteLine("A register with this name already exists!");
				continue;
			}

			// Done
			return registerName;
		}
	}

	/// <summary>
	/// This function will ask the user the name of register which they want to remove. Then tries to remove it.<br/>
	/// It can fail if the register is being used in the chart
	/// </summary>
	/// <param name="registers"></param>
	private void RemoveRegister(IDictionary<string, Register> registers)
	{
		Console.Write("Enter register name to remove: ");
		string registerName = Console.ReadLine()!;
		// Check if it's being used in the asm chart
		if (_asmChart.IsRegisterBeingUsed(registerName))
		{
			Console.WriteLine("Cannot remove this register because it's being used.");
			return;
		}

		// Remove and check if it exists
		bool removed = registers.Remove(registerName);
		Console.WriteLine(removed ? "Register removed." : "Register not found!");
	}

	/// <summary>
	/// Gets the register size in bits of a register which must be in output register or general register
	/// </summary>
	/// <param name="registerName">The name to search for</param>
	/// <returns>The register size or null if it does not exists</returns>
	private int? GetRegisterSizeOfOutputGeneral(string registerName)
	{
		if (_asmChart.Outputs.TryGetValue(registerName, out Register? reg))
			return reg.Length;
		if (_asmChart.Registers.TryGetValue(registerName, out reg))
			return reg.Length;
		return null;
	}

	/// <summary>
	/// Gets the input from user to create a state for asm chart
	/// </summary>
	private void AddState()
	{
		AsmBlock state = new();
		// Cache registers
		HashSet<string> editableRegisters = new(_asmChart.Outputs.Keys.Concat(_asmChart.Registers.Keys));
		HashSet<string> allRegisters = new(_asmChart.MergedVariables.Keys);
		// Get the new state name and check it
		Console.Write("Enter state name: ");
		string stateName = Console.ReadLine()!;
		if (_asmChart.States.ContainsKey(stateName))
		{
			Console.WriteLine("This state already exists!");
			return;
		}

		// Get the statements
		Console.WriteLine("I will ask you about the statements which will be ran in this state.");
		state.Statements.AddRange(ReadStatements(editableRegisters, allRegisters));

		// Get the aftermath
		if (ConsoleUtils.YesNoQuestion("Do you want to create a aftermath now?"))
			state.AftermathOfBlock = GetAftermath(
				new HashSet<string>(_asmChart.States.Keys.Append(stateName)),
				editableRegisters, allRegisters
			);

		// Done
		_asmChart.States[stateName] = state;
	}

	/// <summary>
	/// Removes a statement of choice of user
	/// </summary>
	private void RemoveState()
	{
		Console.Write("Enter state name to remove: ");
		string stateName = Console.ReadLine()!;
		// Check if state exists
		if (!_asmChart.States.ContainsKey(stateName))
		{
			Console.WriteLine("State does not exists!");
			return;
		}

		// Check if the state is being used
		foreach ((string toCheckName, AsmBlock toCheckState) in _asmChart.States)
		{
			if (toCheckName == stateName) // So user can remove cyclic stuff!
				continue;
			// Check the end
			bool inUse = toCheckState.AftermathOfBlock switch
			{
				AsmBlock.AftermathCondition aftermathCondition => aftermathCondition.NextStateFalse == stateName ||
				                                                  aftermathCondition.NextStateTrue == stateName,
				AsmBlock.AftermathJump aftermathJump => aftermathJump.StateName == stateName,
				_ => false
			};
			if (inUse)
			{
				Console.WriteLine($"This state is in use at state {toCheckName}. Cannot remove it.");
				return;
			}
		}

		// Remove and check if it exists
		_asmChart.States.Remove(stateName);
		Console.WriteLine("State removed");
	}

	/// <summary>
	/// Gets the aftermath for a state from user
	/// </summary>
	/// <param name="validStates">The list of states which this block can jump to</param>
	/// <param name="mutableRegisters">List of registers which can be changed</param>
	/// <param name="allRegisters">List of all registers</param>
	/// <returns>The <see cref="AsmBlock.Aftermath"/> entered by user</returns>
	private static AsmBlock.Aftermath GetAftermath(IReadOnlySet<string> validStates,
		IReadOnlySet<string> mutableRegisters,
		IReadOnlySet<string> allRegisters)
	{
		if (ConsoleUtils.YesNoQuestion("Is the aftermath an unconditional jump?"))
		{
			string nextStateName;
			while (true)
			{
				Console.Write("What is the next state name? ");
				nextStateName = Console.ReadLine()!;
				if (validStates.Contains(nextStateName))
					break;
				Console.WriteLine("Invalid state name.");
			}

			return new AsmBlock.AftermathJump(nextStateName);
		}

		// Conditional jump
		Console.WriteLine("Choose the condition...");
		Statement condition = ReadStatement(allRegisters);
		Console.WriteLine("Enter the statements which will be ran if the condition is true:");
		var trueConditionStatements = ReadStatements(mutableRegisters, allRegisters);
		Console.WriteLine("Enter the statements which will be ran if the condition is false:");
		var falseConditionStatements = ReadStatements(mutableRegisters, allRegisters);
		// Next state names
		string nextStateFalse, nextStateTrue;
		while (true)
		{
			Console.Write("What is the next state name if condition is true? ");
			nextStateTrue = Console.ReadLine()!;
			if (validStates.Contains(nextStateTrue))
				break;
			Console.WriteLine("Invalid state name.");
		}

		while (true)
		{
			Console.Write("What is the next state name if condition is false? ");
			nextStateFalse = Console.ReadLine()!;
			if (validStates.Contains(nextStateFalse))
				break;
			Console.WriteLine("Invalid state name.");
		}

		// Done
		return new AsmBlock.AftermathCondition(
			condition,
			falseConditionStatements,
			trueConditionStatements,
			nextStateFalse,
			nextStateTrue
		);
	}

	/// <summary>
	/// Read a list of statements from terminal<br/>
	/// Each entry in list contains a destination register name and the statement
	/// </summary>
	/// <param name="mutableRegisters">List of mutable registers</param>
	/// <param name="allRegisters">List of all registers</param>
	/// <returns></returns>
	private static IEnumerable<(string, Statement)> ReadStatements(IReadOnlySet<string> mutableRegisters,
		IReadOnlySet<string> allRegisters)
	{
		List<(string, Statement)> result = new();
		while (true)
		{
			if (!ConsoleUtils.YesNoQuestion("Add a statement?"))
				break;
			// Get the register
			string destinationRegisterName;
			while (true)
			{
				Console.Write("What register is the destination of this statement? ");
				destinationRegisterName = Console.ReadLine()!;
				// Check it
				if (mutableRegisters.Contains(destinationRegisterName))
					break;
				Console.WriteLine("Register not found in Outputs or General Registers");
			}

			// Get the statement
			Statement statement = ReadStatement(allRegisters);
			result.Add((destinationRegisterName, statement));
		}

		return result;
	}

	/// <summary>
	/// Reads a statement from terminal
	/// </summary>
	/// <param name="validRegisterNames">List of all registers</param>
	/// <returns>The statement which user entered</returns>
	private static Statement ReadStatement(IReadOnlySet<string> validRegisterNames)
	{
		// Get operator
		Console.WriteLine("Please select the operator:");
		var operators = Enum.GetValues<Statement.Operator>();
		for (var i = 0; i < operators.Length; i++)
			Console.WriteLine($"{i + 1}. {operators[i]}");
		Console.WriteLine($"{operators.Length + 1}. Assignment (copy)");
		Statement.Operator? opt = null;
		var simpleAssignment = false;
		while (true)
		{
			int choice = ConsoleUtils.GetPositiveInteger("Operator? (select by number) ");
			if (choice == operators.Length + 1)
			{
				simpleAssignment = true;
				break;
			}

			if (choice <= 0 || choice > operators.Length)
			{
				Console.WriteLine("Out of range");
				continue;
			}

			opt = operators[choice - 1];
			break;
		}

		// Get the first operand
		Statement.Instruction firstOperand = GetOperand(validRegisterNames, "first operand");
		if (opt == null || simpleAssignment)
			return new Statement(new[] {firstOperand});
		if (opt.Value.IsUnary())
		{
			return new Statement(new[]
			{
				firstOperand,
				new Statement.InstructionOperator(opt.Value),
			});
		}

		// Get the second one
		Statement.Instruction secondOperand = GetOperand(validRegisterNames, "second operand");
		return new Statement(new[]
		{
			firstOperand,
			secondOperand,
			new Statement.InstructionOperator(opt.Value),
		});
	}

	/// <summary>
	/// Gets an operand from user
	/// </summary>
	/// <param name="validRegisterNames">Valid names of registers</param>
	/// <param name="operandName">The operand name to show to user</param>
	/// <returns>The instruction</returns>
	private static Statement.Instruction GetOperand(IReadOnlySet<string> validRegisterNames, string operandName)
	{
		if (ConsoleUtils.YesNoQuestion($"Is {operandName} a literal constant?"))
			return new Statement.InstructionConstant(
				ConsoleUtils.GetNonNegativeInteger("Enter a non negative number: "));
		// Register
		while (true)
		{
			Console.Write("Enter register name: ");
			string registerName = Console.ReadLine()!;
			if (validRegisterNames.Contains(registerName))
				return new Statement.InstructionRegister(registerName);
			Console.WriteLine("Invalid register name!");
		}
	}

	/// <summary>
	/// This function will get a register name from user and a number and resizes the register to that size
	/// </summary>
	/// <param name="registers">The list of registers</param>
	private static void ChangeRegisterSize(IDictionary<string, Register> registers)
	{
		Console.Write("Enter register name to resize: ");
		string registerName = Console.ReadLine()!;

		if (!registers.ContainsKey(registerName))
		{
			Console.WriteLine("Register not found!");
			return;
		}

		int newSize = ConsoleUtils.GetPositiveInteger("Enter the register size in bits: ");
		registers[registerName] = new Register(newSize);
	}

	private static void PrintRegisterValues(IDictionary<string, Register> registers)
	{
		foreach ((string registerName, Register register) in registers.OrderBy(p => p.Key))
			Console.WriteLine($"{registerName} ({register.Length} bit): {register}");
	}

	private static void ChangeRegisterValue(IDictionary<string, Register> registers)
	{
		Console.Write("Enter the register name which you want to change: ");
		string registerName = Console.ReadLine()!;
		// Check register
		if (!registers.TryGetValue(registerName, out Register? register))
		{
			Console.WriteLine("Register does not exists.");
			return;
		}

		// Get the new value
		register.Set(ConsoleUtils.GetNonNegativeBigNumber("Enter the new value for this register: "));
	}
}