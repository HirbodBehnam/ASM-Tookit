namespace ASM_Toolkit;

public class RegisterNotFoundException : Exception
{
	public RegisterNotFoundException(string registerName) : base("register not found: " + registerName)
	{
	}
}