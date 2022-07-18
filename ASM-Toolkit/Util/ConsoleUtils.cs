using System.Numerics;

namespace ASM_Toolkit.Util;

public static class ConsoleUtils
{
	public const string InvalidOptionMessage = "Invalid option";

	/// <summary>
	/// This function will print a prompt and then read a single key from keyboard
	/// </summary>
	/// <param name="promptText">The text to print</param>
	/// <returns>The key which user pressed</returns>
	public static char InputKey(string promptText = "")
	{
		Console.Write(promptText);
		ConsoleKeyInfo key = Console.ReadKey();
		Console.WriteLine(); // Move to next line
		return key.KeyChar;
	}

	/// <summary>
	/// This function will read input from stdin until it reaches a positive number and returns it
	/// </summary>
	/// <param name="promptText">The text to print before asking for number</param>
	/// <returns>The number entered</returns>
	public static int GetPositiveInteger(string promptText = "")
	{
		while (true)
		{
			Console.Write(promptText);
			if (int.TryParse(Console.ReadLine(), out int number))
			{
				if (number > 0)
					return number;
				Console.WriteLine("Please enter a positive number.");
				continue;
			}

			Console.WriteLine("Cannot parse number.");
		}
	}

	/// <summary>
	/// This function will get a positive big integer from command line
	/// </summary>
	/// <param name="promptText">The text to show to user</param>
	/// <returns>The big integer which user entered</returns>
	public static BigInteger GetPositiveBigNumber(string promptText = "")
	{
		while (true)
		{
			Console.Write(promptText);
			if (BigInteger.TryParse(Console.ReadLine(), out BigInteger number))
			{
				if (number.Sign > 0)
					return number;
				Console.WriteLine("Please enter a positive number.");
				continue;
			}

			Console.WriteLine("Cannot parse number.");
		}
	}
}