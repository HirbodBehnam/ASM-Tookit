using ASM_Toolkit;
using ASM_Toolkit_CLI.Util;

namespace ASM_Toolkit_CLI;

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
			switch (ConsoleUtils.InputKey("Choose: "))
			{
				case '1':
				{
					Console.Write("Enter the file path: ");
					string path = Console.ReadLine()!;
					
					Asm asm;
					try
					{
						string loadedFile = File.ReadAllText(path);
						asm = AsmSaverLoader.LoadFromJson(loadedFile);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Cannot load the saved ASM: " + ex.Message);
						continue;
					}

					new AsmMenus(asm, path).MainMenu();
					break;
				}
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