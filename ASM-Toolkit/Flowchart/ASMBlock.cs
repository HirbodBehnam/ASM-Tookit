namespace ASM_Toolkit.Flowchart;

public class AsmBlock
{
	/// <summary>
	/// List of statements which will be ran on main block<br/>
	/// The first element in tuple is the destination register name<br/>
	/// The second element is the statement which will be ran
	/// </summary>
	public List<(string, Statement)> Statements { get; }

	/// <summary>
	/// What should happen after the <see cref="Statements"/> has been ran
	/// </summary>
	public Aftermath? AftermathOfBlock { get; set; }

	public abstract class Aftermath
	{
		// Empty class
	}

	public class AftermathJump : Aftermath
	{
		public string StateName { get; }

		public AftermathJump(string stateName)
		{
			StateName = stateName;
		}
	}

	public class AftermathCondition : Aftermath
	{
		public Statement Condition { get; }
		public IEnumerable<(string, Statement)> ConditionTrueStatements { get; }
		public IEnumerable<(string, Statement)> ConditionFalseStatements { get; }
		public string NextStateTrue { get; }
		public string NextStateFalse { get; }

		public AftermathCondition(Statement condition,
			IEnumerable<(string, Statement)>? conditionFalseStatements,
			IEnumerable<(string, Statement)>? conditionTrueStatements,
			string nextStateFalse, string nextStateTrue)
		{
			Condition = condition;
			ConditionFalseStatements = conditionFalseStatements ?? Array.Empty<(string, Statement)>();
			ConditionTrueStatements = conditionTrueStatements ?? Array.Empty<(string, Statement)>();
			NextStateFalse = nextStateFalse;
			NextStateTrue = nextStateTrue;
		}
	}

	public AsmBlock()
	{
		Statements = new List<(string, Statement)>();
	}

	/// <summary>
	/// This method will execute the ASM block and update all registers<br/>
	/// At last, it will return the name of next state (asm block)
	/// </summary>
	/// <param name="registers">The registers in this ASM chart</param>
	/// <returns>The next block name</returns>
	/// <exception cref="InvalidOperationException">If <see cref="AftermathOfBlock"/> is null</exception>
	public string ExecuteBlock(Dictionary<string, Register> registers)
	{
		// Check aftermath
		if (AftermathOfBlock == null)
			throw new InvalidOperationException(nameof(AftermathOfBlock) + " is null");
		// Execute the blocks
		RunStatementsAndAssign(Statements, registers);

		// Now check what happens after the main block
		switch (AftermathOfBlock)
		{
			case AftermathCondition condition:
			{
				// Run the test
				bool result = condition.Condition.Evaluate(registers);
				if (result)
				{
					RunStatementsAndAssign(condition.ConditionTrueStatements, registers);
					return condition.NextStateTrue;
				}

				// result is false
				RunStatementsAndAssign(condition.ConditionFalseStatements, registers);
				return condition.NextStateFalse;
			}
			// Direct jump
			case AftermathJump jump:
				return jump.StateName;
		}

		// Invalid state. Not possible
		throw new InvalidOperationException(nameof(AftermathOfBlock) + " is invalid");
	}

	/// <summary>
	/// This function will loop over all statements and run them and update registers
	/// </summary>
	/// <param name="statements">Statements to run</param>
	/// <param name="registers">The registers</param>
	private static void RunStatementsAndAssign(IEnumerable<(string, Statement)> statements,
		Dictionary<string, Register> registers)
	{
		foreach ((string registerName, Statement statement) in statements)
		{
			// Check if the destination register exists
			if (!registers.ContainsKey(registerName))
				throw new RegisterNotFoundException(registerName);
			Register resultRegister = registers[registerName];
			// Now run the statement
			Register statementResult = statement.Evaluate(registers, resultRegister.Length);
			resultRegister.Set(statementResult);
		}
	}
}