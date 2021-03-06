﻿using System;

namespace pEngine.Utils.Math
{
	/// <summary>
	/// An object that can be moved.
	/// </summary>
	public interface IPhysicalObject : ISpaced
	{
		
		/// <summary>
		/// Gets or sets the position.
		/// </summary>
		new Vector2i Position { get; set; }

	}
}
