﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pEngine.Platform;
using pEngine.Platform.Input;
using pEngine.Platform.Forms;

using pEngine.Utils.Threading;
using pEngine.Utils.Timing.Base;

using pEngine.Input;

namespace pEngine.Framework
{
	public partial class GameHost : pObject
	{
		/// <summary>
		/// Makes a new instance of <see cref="GameHost"/> class.
		/// </summary>
		public GameHost()
		{
			// - Initialize environment if not initialized
			pEngine.Platform.Environment.Initialize();

			// - Make game loops
			InputGameLoop = new GameLoop(HandleInput, "InputThread");
			PhysicsGameLoop = new ThreadedGameLoop(HandlePhysics, "PhysicsThread");

			// - Modules
			Input = new InputEngine(this, InputGameLoop);
		}

		/// <summary>
		/// Gets the running game.
		/// </summary>
		public Game RunningGame { get; private set; }

		/// <summary>
		/// Running platform.
		/// </summary>
		public IPlatform Platform => pEngine.Platform.Environment.Platform;

		/// <summary>
		/// Game window.
		/// </summary>
		public IWindow Window => Platform.ApplicationWindow;

		#region Game management

		/// <summary>
		/// Run the specified game.
		/// </summary>
		/// <param name="game">Game to run.</param>
		public virtual void Run(Game game)
		{
			RunningGame = game;

			// - Make a new window
			Window.Make();
			Window.Show();

			// - Initialize input apis
			Platform.Input.Initialize();
			Input.Initialize();

			// - Start gameloops
			PhysicsGameLoop.Run();
			InputGameLoop.Run();

			// - Load the game
			RunningGame.Load();
		}

		#endregion

		#region Input thread

		/// <summary>
		/// Game loop for input handling.
		/// </summary>
		public GameLoop InputGameLoop { get; }

		/// <summary>
		/// 
		/// </summary>
		public InputEngine Input { get; }

		/// <summary>
		/// Handle an input frame.
		/// </summary>
		/// <param name="clock">Timed clock.</param>
		protected virtual void HandleInput(IFrameBasedClock clock)
		{
			Window.PollMesages(true);

            // - Update hardware input
            Platform.Input.Update(clock);

			// - 
			Input.Update(clock);
		}

		#endregion

		#region Physics thread

		/// <summary>
		/// Game loop for physics handling.
		/// </summary>
		public GameLoop PhysicsGameLoop { get; }

		InputService In;

		/// <summary>
		/// Handle a physics calculation frame.
		/// </summary>
		/// <param name="clock">Timed clock.</param>
		protected virtual void HandlePhysics(IFrameBasedClock clock)
		{
			if (In == null)
			{
				In = Input.GetSettings(PhysicsGameLoop) as InputService;
			}

			if (In.Joypads.Count > 0 && In.Joypads[0].Buttons.Count() > 0 && In.Joypads[0].Buttons[0] == KeyState.Pressed)
				return;
		}


		#endregion

	}
}
