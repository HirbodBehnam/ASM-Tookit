﻿using System.Collections;
using System.Numerics;

namespace ASM_Toolkit;

public class Register
{
	private readonly BitArray _data;

	public int Length => _data.Count;

	public bool this[int index]
	{
		get => _data[index];
		set => _data[index] = value;
	}

	/// <summary>
	/// Number will get the number which these registers represent<br/>
	/// The number is always positive (this register is unsigned)
	/// </summary>
	public BigInteger Number
	{
		get
		{
			var raw = new byte[CeilingDiv(_data.Count, 8)];
			_data.CopyTo(raw, 0);
			return new BigInteger(raw, isUnsigned: true);
		}
	}

	/// <summary>
	/// Initializes the register vector with given size
	/// </summary>
	/// <param name="width">The size of given vector. Cannot be changed</param>
	public Register(int width)
	{
		_data = new BitArray(width);
	}

	private Register(BitArray array)
	{
		_data = array;
	}

	/// <summary>
	/// Set will clear the bit array and set all bits according to bits
	/// of given number.
	/// </summary>
	/// <param name="a">The number to get the bits from it</param>
	public void Set(uint a)
	{
		_data.SetAll(false); // Clear the bit array
		for (var i = 0; i < Math.Min(32, _data.Count); i++)
			this[i] = ((a >> i) & 1) != 0; // Set each bit from LSB
	}

	/// <summary>
	/// Set will set the bits in register from a <see cref="BigInteger"/>
	/// </summary>
	/// <param name="number">The number to get the bits from</param>
	public void Set(BigInteger number)
	{
		_data.SetAll(false); // Clear bit array
		byte[] data = number.ToByteArray();
		for (var i = 0; i < Math.Min(data.Length * 8, Length); i++)
			this[i] = ((data[i / 8] >> (i % 8)) & 1) != 0; // Set the bit
	}

	/// <summary>
	/// Sets the bits of this register from a bitset
	/// </summary>
	/// <param name="array">The bit set to get the bits from</param>
	public void Set(BitArray array)
	{
		_data.SetAll(false); // Clear the bit array
		for (var i = 0; i < Math.Min(this.Length, array.Count); i++)
			this[i] = array[i]; // Copy bits
	}

	/// <summary>
	/// Assigns a register into this register
	/// </summary>
	/// <param name="reg">Register to assign</param>
	public void Set(Register reg)
	{
		_data.SetAll(false); // Clear the bit array
		for (var i = 0; i < Math.Min(this.Length, reg.Length); i++)
			this[i] = reg[i]; // Copy bits
	}

	public void Reset()
	{
		_data.SetAll(false);
	}

	/// <summary>
	/// Clone will make a copy from this register
	/// </summary>
	/// <returns>A copied register</returns>
	public Register Clone()
	{
		var reg = new Register(Length);
		reg.Set(this);
		return reg;
	}

	/// <summary>
	/// Unary or
	/// </summary>
	/// <returns>True if there is at least one 1 in bitset</returns>
	public bool Or() => _data.Cast<bool>().Any(bit => bit);

	/// <summary>
	/// Unary and
	/// </summary>
	/// <returns>True if there all bits are 1 in bitset</returns>
	public bool And() => _data.Cast<bool>().All(bit => bit);

	/// <summary>
	/// Unary xor
	/// </summary>
	/// <details>
	/// What this method does inside is that it counts all 1 bits and
	/// checks if it's dividable by two or not
	/// </details>
	/// <returns>The xor of all bits</returns>
	public bool Xor() => _data.Cast<bool>().Count(bit => bit) % 2 == 1;

	public static explicit operator Register(uint b)
	{
		Register reg = new(32);
		reg.Set(b);
		return reg;
	}

	public static implicit operator uint(Register r) => (uint) r.Number;

	/// <summary>
	/// Conversion to bool is like <see cref="Number"/> != 0
	/// </summary>
	/// <param name="r">The register</param>
	/// <returns>If at least one bit is not zero</returns>
	public static implicit operator bool(Register r) => r.Or();

	/// <summary>
	/// Creates a one bit register from a bool
	/// </summary>
	/// <param name="b">The bool</param>
	/// <returns>A <see cref="Register"/> with <see cref="Length"/> of one</returns>
	public static explicit operator Register(bool b) => new(new BitArray(new[] {b}));

	/// <summary>
	/// Will create a copy of this register with same length which has all of it's bits inverted
	/// </summary>
	/// <param name="a">The register to invert</param>
	/// <returns>A new copy of register</returns>
	public static Register operator ~(Register a) => new(new BitArray(a._data).Not());

	public static Register operator -(Register a) => (~a) + ((Register) 1);

	/// <summary>
	/// Add two registers together<br/>
	/// It is important to note that the width of the final register is the same as
	/// the first operand
	/// </summary>
	/// <param name="a">First operand</param>
	/// <param name="b">Second operand</param>
	/// <returns>a + b</returns>
	public static Register operator +(Register a, Register b)
	{
		BigInteger resultNumber = a.Number + b.Number;
		Register resultRegister = new(a.Length); // Result has the same size as first operand
		resultRegister.Set(resultNumber);
		return resultRegister;
	}

	/// <summary>
	/// Subtracts two registers from together<br/>
	/// It is important to note that the width of the final register is the same as
	/// the first operand
	/// </summary>
	/// <param name="a">First operand</param>
	/// <param name="b">Second operand</param>
	/// <details>
	/// This method is defined as <code>a + (-b)</code>. (-b) will be evaluated to two's complement and then
	/// will be added to a just like real hardware
	/// </details>
	/// <returns>a - b</returns>
	public static Register operator -(Register a, Register b)
	{
		return a + (-b);
	}

	/// <summary>
	/// Multiplies two registers in each other<br/>
	/// It is important to note that the width of the final register is the same as
	/// the first operand
	/// </summary>
	/// <param name="a">First operand</param>
	/// <param name="b">Second operand</param>
	/// <returns>a * b</returns>
	public static Register operator *(Register a, Register b)
	{
		BigInteger resultNumber = a.Number * b.Number;
		Register resultRegister = new(a.Length); // Result has the same size as first operand
		resultRegister.Set(resultNumber);
		return resultRegister;
	}

	/// <summary>
	/// Divides two registers<br/>
	/// It is important to note that the width of the final register is the same as
	/// the first operand
	/// </summary>
	/// <param name="a">First operand</param>
	/// <param name="b">Second operand</param>
	/// <returns>a / b</returns>
	public static Register operator /(Register a, Register b)
	{
		BigInteger resultNumber = a.Number / b.Number;
		Register resultRegister = new(a.Length); // Result has the same size as first operand
		resultRegister.Set(resultNumber);
		return resultRegister;
	}

	/// <summary>
	/// Finds the mod of two registers<br/>
	/// It is important to note that the width of the final register is the same as
	/// the first operand
	/// </summary>
	/// <param name="a">First operand</param>
	/// <param name="b">Second operand</param>
	/// <returns>a % b</returns>
	public static Register operator %(Register a, Register b)
	{
		BigInteger resultNumber = a.Number % b.Number;
		Register resultRegister = new(a.Length); // Result has the same size as first operand
		resultRegister.Set(resultNumber);
		return resultRegister;
	}

	public static Register operator |(Register a, Register b) => new(new BitArray(a._data).Or(b._data));
	public static Register operator &(Register a, Register b) => new(new BitArray(a._data).And(b._data));
	public static Register operator ^(Register a, Register b) => new(new BitArray(a._data).Xor(b._data));

	public static bool operator >(Register a, Register b) => a.Number > b.Number;
	public static bool operator >=(Register a, Register b) => a.Number >= b.Number;
	public static bool operator <(Register a, Register b) => a.Number < b.Number;
	public static bool operator <=(Register a, Register b) => a.Number <= b.Number;
	public static bool operator ==(Register a, Register b) => a.Number == b.Number;
	public static bool operator !=(Register a, Register b) => a.Number != b.Number;

	/// <summary>
	/// To string returns the number stored in register as string<br/>
	/// The given number is unsigned
	/// </summary>
	/// <returns></returns>
	public override string ToString() => Number.ToString();

	private bool Equals(Register other)
	{
		return _data.Equals(other._data);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((Register) obj);
	}

	public override int GetHashCode()
	{
		return _data.GetHashCode();
	}

	private static int CeilingDiv(int x, int y) => (x + y - 1) / y;
}