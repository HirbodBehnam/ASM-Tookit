namespace ASM_Toolkit;

public static class OperatorExtensions
{
	public static bool IsUnary(this Statement.Operator opt)
	{
		return opt is Statement.Operator.UnaryAnd or Statement.Operator.UnaryNegate or Statement.Operator.UnaryNot
			or Statement.Operator.UnaryOr or Statement.Operator.UnaryXor;
	}
}