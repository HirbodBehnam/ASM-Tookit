using System;
using System.Collections;
using System.Numerics;
using ASM_Toolkit.Flowchart;
using NUnit.Framework;

namespace Tests.Flowchart;

public class RegisterTests
{
	[Test]
	public void ToStringTest()
	{
		Random rng = new();
		const int size = 100;
		Register reg1 = new(size), reg2 = new(size);
		BitArray ary = new(size);
		for (var i = 0; i < size; i++)
			ary[i] = rng.Next() % 2 == 0;
		reg1.Set(ary);
		reg2.Set(reg1);
		Assert.AreEqual(reg1.Number, reg2.Number);
	}

	[Test]
	public void SetTest()
	{
		Random rng = new();
		Register reg = new(32);
		for (var i = 0; i < 100; i++)
		{
			var number = (uint) rng.Next();
			reg.Set(number);
			Assert.AreEqual(number, (uint) reg);
			Assert.AreEqual(number.ToString(), reg.ToString());
		}

		// Set the last bit as well
		for (var i = 0; i < 100; i++)
		{
			var number = (uint) rng.Next();
			number |= 2147483648; // 1 << 31
			reg.Set(number);
			Assert.AreEqual(number, (uint) reg);
			Assert.AreEqual(number.ToString(), reg.ToString());
		}

		// Set number into smaller register
		reg = new Register(10);
		const uint mask = (1 << 10) - 1;
		for (var i = 0; i < 100; i++)
		{
			var number = (uint) rng.Next();
			reg.Set(number);
			number &= mask;
			Assert.AreEqual(number, (uint) reg);
			Assert.AreEqual(number.ToString(), reg.ToString());
		}
	}

	[Test]
	public void SetBigIntegerTest()
	{
		Register reg = new(1024);
		// Small test
		BigInteger big = 123456;
		reg.Set(big);
		Assert.AreEqual(big, reg.Number);
		Assert.AreEqual(big.ToString(), reg.ToString());
		// Big test
		const string numberString = "12345678901234567890";
		big = BigInteger.Parse(numberString);
		reg.Set(big);
		Assert.AreEqual(big, reg.Number);
		Assert.AreEqual(numberString, reg.ToString());
	}

	[Test]
	public void UnaryTest()
	{
		const int regSize = 32;
		Register reg = new(regSize);
		// Empty reg test
		Assert.IsFalse(reg.Or());
		Assert.IsFalse(reg.And());
		Assert.IsFalse(reg.Xor());
		// Set one bit
		reg[0] = true;
		Assert.IsTrue(reg.Or());
		Assert.IsFalse(reg.And());
		Assert.IsTrue(reg.Xor());
		// Set one bit in random place
		reg = new Register(regSize)
		{
			[10] = true
		};
		Assert.IsTrue(reg.Or());
		Assert.IsFalse(reg.And());
		Assert.IsTrue(reg.Xor());
		// Set all bits
		for (var i = 0; i < regSize; i++)
			reg[i] = true;
		Assert.IsTrue(reg.Or());
		Assert.IsTrue(reg.And());
		// Check XOR
		reg = new Register(regSize);
		var xorResult = false;
		for (var i = 0; i < regSize; i++)
		{
			reg[i] = true;
			xorResult = !xorResult;
			Assert.AreEqual(xorResult, reg.Xor());
		}
	}

	[Test]
	public void NotTest()
	{
		Random rng = new();
		Register reg = new(100);
		// Randomly set the register
		for (var i = 0; i < reg.Length; i++)
			reg[i] = rng.Next() % 2 != 0;
		// Not and check
		Register notReg = ~reg;
		for (var i = 0; i < reg.Length; i++)
			Assert.AreNotEqual(reg[i], notReg[i]);
	}

	[Test]
	public void ArithmeticTest()
	{
		Random rng = new();
		for (var i = 0; i < 1000; i++)
		{
			uint num1 = (uint) rng.Next(), num2 = (uint) rng.Next();
			Register reg1 = new(32), reg2 = new(32);
			reg1.Set(num1);
			reg2.Set(num2);
			Assert.AreEqual(num1 + num2, (uint) (reg1 + reg2));
			Assert.AreEqual(num1 - num2, (uint) (reg1 - reg2));
			Assert.AreEqual(num1 * num2, (uint) (reg1 * reg2));
			Assert.AreEqual(num1 / num2, (uint) (reg1 / reg2));
			Assert.AreEqual(num1 % num2, (uint) (reg1 % reg2));
			Assert.AreEqual(num1 > num2, reg1 > reg2);
			Assert.AreEqual(num1 < num2, reg1 < reg2);
			Assert.AreEqual(num1 >= num2, reg1 >= reg2);
			Assert.AreEqual(num1 <= num2, reg1 <= reg2);
			Assert.AreEqual(num1 == num2, reg1 == reg2);
			Assert.AreEqual(num1 != num2, reg1 != reg2);
		}

		// Underflow test
		unchecked
		{
			const uint num1 = 1, num2 = 100;
			const uint diff = num1 - num2;
			Register reg1 = new(32), reg2 = new(32);
			reg1.Set(num1);
			reg2.Set(num2);
			Assert.AreEqual(diff, (uint) (reg1 - reg2));
		}
	}

	[Test]
	public void BitTest()
	{
		Random rng = new();
		for (var i = 0; i < 1000; i++)
		{
			uint num1 = (uint) rng.Next(), num2 = (uint) rng.Next();
			Register reg1 = new(32), reg2 = new(32);
			reg1.Set(num1);
			reg2.Set(num2);
			Assert.AreEqual(num1 | num2, (uint) (reg1 | reg2));
			Assert.AreEqual(num1 & num2, (uint) (reg1 & reg2));
			Assert.AreEqual(num1 ^ num2, (uint) (reg1 ^ reg2));
			// These operations should not change inner values
			Assert.AreEqual(num1, (uint) reg1);
			Assert.AreEqual(num2, (uint) reg2);
		}
	}
}