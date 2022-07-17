using ASM_Toolkit.Util;

namespace ASM_Toolkit;

public static class Menus
{
	public static void MainMenu()
	{
		while (true)
		{
			Console.WriteLine("Select an option:");
			Console.WriteLine("1. Load an ASM chart");
			Console.WriteLine("2. Create new ASM chart");
			Console.WriteLine("3. Exit");
			switch (ConsoleUtils.InputKey())
			{
				case '1':
					break;
				case '2':
					new AsmMenus().MainMenu();
					break;
				case '3':
					return;
				default:
					Console.WriteLine(ConsoleUtils.InvalidOptionMessage);
					break;
			}
		}
	}
}