namespace ASM_Toolkit.Flowchart;

public class RegisterNotFoundException : Exception
{
	public RegisterNotFoundException(string registerName) : base("register not found: " + registerName)
	{
	}
}