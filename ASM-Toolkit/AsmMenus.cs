using System.Numerics;
using System.Text.RegularExpressions;
using ASM_Toolkit.Flowchart;
using ASM_Toolkit.Util;

namespace ASM_Toolkit;

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
			Console.WriteLine("7. Enter simulation mode");
			Console.WriteLine("8. Generate verilog code");
			Console.WriteLine("0. Exit");
			switch (ConsoleUtils.InputKey())
			{
				case '1': // Save
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
			switch (ConsoleUtils.InputKey())
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
					int registerSize = ConsoleUtils.GetPositiveInteger("Enter register size: ");
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
			switch (ConsoleUtils.InputKey())
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

		int newSize = ConsoleUtils.GetPositiveInteger("Enter the register size: ");
		registers[registerName] = new Register(newSize);
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
}