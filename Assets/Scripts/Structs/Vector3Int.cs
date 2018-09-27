
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;


public struct Vector3Int : IEquatable<Vector3Int>
{
	public int x,y,z;

	public Vector3Int(int x, int y, int z)
	{
		this.x=x;
		this.y=y;
		this.z=z;
	}

	public static Vector3Int zero()
	{
		return new Vector3Int (0, 0, 0);
	}

	public static Vector3Int operator +(Vector3Int a, Vector3Int b)
	{
		Vector3Int temp = new Vector3Int();
		temp.x = a.x +b.x;
		temp.y = a.y +b.y;
		temp.z = a.z +b.z;
		return temp;
	}

	public bool Equals(Vector3Int other)
	{
		return other.x==this.x && other.y==this.y && other.z==this.z;
	}

	public static bool operator ==(Vector3Int a, Vector3Int b)
	{
		return a.Equals(b);
	}

	public override bool Equals(object obj)
	{
		if(obj==null || !(obj is Vector3Int))
			return false;

		return Equals((Vector3Int)obj);
	}

	public override int GetHashCode ()
	{
		int hash = 437;
		hash = 3132 * hash + this.x;
		hash = 2572 * hash + this.y;
		hash = 1871 * hash + this.z;
		return hash;
	}

	public override string ToString ()
	{
		return string.Format ("("+x+", "+y+", "+z+")");
	}

	public static bool operator !=(Vector3Int a, Vector3Int b)
	{
		return !(a==b);
	}
}
