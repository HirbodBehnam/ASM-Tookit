namespace ASM_Toolkit.Flowchart;

public class Asm
{
	/// <summary>
	/// The inputs of asm chart
	/// </summary>
	public Dictionary<string, Register> Inputs { get; }

	/// <summary>
	/// The outputs of the asm chart
	/// </summary>
	public Dictionary<string, Register> Outputs { get; }

	/// <summary>
	/// General registers of chart
	/// </summary>
	public Dictionary<string, Register> Registers { get; }

	/// <summary>
	/// List of states of flowchart
	/// </summary>
	public Dictionary<string, AsmBlock> States { get; }

	/// <summary>
	/// Initial values of output/general registers<br/>
	/// Non existent value means zero
	/// </summary>
	public Dictionary<string, Register> InitialValues { get; }

	/// <summary>
	/// The first state to go after <see cref="Reset"/>
	/// </summary>
	private AsmBlock? _firstState;

	/// <summary>
	/// Holds the current state of asm<br/>
	/// A null value means that this asm chart is never resetted
	/// </summary>
	public AsmBlock? CurrentState { get; private set; }

	public string? CurrentStateName
	{
		get { return CurrentState == null ? null : States.FirstOrDefault(x => x.Value == CurrentState).Key; }
	}

	/// <summary>
	/// Clocks passed from reset
	/// </summary>
	public int ClockCounter { get; private set; }

	/// <summary>
	/// Returns a merged dictionary of <see cref="Inputs"/> and <see cref="Outputs"/> and <see cref="Registers"/> 
	/// </summary>
	public Dictionary<string, Register> MergedVariables
	{
		get
		{
			Dictionary<string, Register> registers = new(Inputs.Count + Outputs.Count + Registers.Count);
			foreach ((string name, Register reg) in Inputs)
				registers.Add(name, reg);
			foreach ((string name, Register reg) in Outputs)
				registers.Add(name, reg);
			foreach ((string name, Register reg) in Registers)
				registers.Add(name, reg);
			return registers;
		}
	}

	public Asm()
	{
		Inputs = new Dictionary<string, Register>();
		Outputs = new Dictionary<string, Register>();
		Registers = new Dictionary<string, Register>();
		States = new Dictionary<string, AsmBlock>();
		InitialValues = new Dictionary<string, Register>();
	}

	/// <summary>
	/// Reset will reset the asm chart to it's first state<br/>
	/// It will also reset the clock counter<br/>
	/// Input registers are not changed
	/// <exception cref="InvalidOperationException">If the <see cref="GetFirstState"/> is null</exception>
	/// </summary>
	public void Reset()
	{
		// Check FirstState
		if (_firstState == null)
			throw new InvalidOperationException(nameof(_firstState) + " must not be empty");
		// Reset registers
		Register? reg;
		foreach ((string registerName, Register register) in Registers)
		{
			if (InitialValues.TryGetValue(registerName, out reg))
				register.Set(reg);
			else
				register.Reset();
		}

		foreach ((string registerName, Register register) in Outputs)
		{
			if (InitialValues.TryGetValue(registerName, out reg))
				register.Set(reg);
			else
				register.Reset();
		}

		// Set the next state
		CurrentState = _firstState;
		ClockCounter = 0;
	}

	/// <summary>
	/// Tick will advance the ASM chart
	/// </summary>
	/// <exception cref="InvalidOperationException">If <see cref="CurrentState"/> is null or next state could not be found</exception>
	public void Tick()
	{
		// Check if chart is resetted
		if (CurrentState == null)
			throw new InvalidOperationException("reset the asm chart at first");
		// Do the block
		string nextState = CurrentState.ExecuteBlock(MergedVariables);
		if (!States.ContainsKey(nextState))
			throw new InvalidOperationException("cannot find the next state");
		// Update current state and clock counter
		CurrentState = States[nextState];
		ClockCounter++;
	}

	/// <summary>
	/// Sets the first state after reset of verilog
	/// </summary>
	/// <param name="stateName">The name of state</param>
	public void SetFirstState(string stateName)
	{
		_firstState = States[stateName];
	}

	public AsmBlock? GetFirstState => _firstState;

	/// <summary>
	/// Checks if a register is used in the whole ASM chart
	/// </summary>
	/// <param name="registerName">The register to check</param>
	/// <returns>True if the register is being used</returns>
	public bool IsRegisterBeingUsed(string registerName)
	{
		foreach (AsmBlock block in States.Values)
		{
			foreach ((string destinationRegisterName, Statement statement) in block.Statements)
			{
				// Check if they use the register
				if (registerName == destinationRegisterName || statement.UsedRegister(registerName))
					return true;
			}
		}

		return false;
	}
}