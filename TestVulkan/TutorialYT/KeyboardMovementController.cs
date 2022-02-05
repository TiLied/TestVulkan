using System;
using System.Numerics;
using SDL2;

namespace TestVulkan
{
	public class KeyboardMovementController
	{
		public struct KeyMappings
		{
			public SDL.SDL_Keycode MoveLeft = SDL.SDL_Keycode.SDLK_a;
			public SDL.SDL_Keycode MoveRight = SDL.SDL_Keycode.SDLK_d;
			public SDL.SDL_Keycode MoveForward = SDL.SDL_Keycode.SDLK_w;
			public SDL.SDL_Keycode MoveBackward = SDL.SDL_Keycode.SDLK_s;
			public SDL.SDL_Keycode MoveUp = SDL.SDL_Keycode.SDLK_e;
			public SDL.SDL_Keycode MoveDown = SDL.SDL_Keycode.SDLK_q;
			public SDL.SDL_Keycode LookLeft = SDL.SDL_Keycode.SDLK_LEFT;
			public SDL.SDL_Keycode LookRight = SDL.SDL_Keycode.SDLK_RIGHT;
			public SDL.SDL_Keycode LookUp = SDL.SDL_Keycode.SDLK_UP;
			public SDL.SDL_Keycode LookDown = SDL.SDL_Keycode.SDLK_DOWN;
		}

		public KeyMappings Keys = new();

		public float MoveSpeed = 30.0f;
		public float LookSpeed = 10.5f;

		public KeyboardMovementController()
		{
		}

		public void MovePlaneXZ(ref SDL.SDL_Event sdlEvent, double dt, ref GameObjectT gameObject) 
		{
			//Everything is kinda wrong, not sure what is wrong, but changed calc dt like here 
			//https://stackoverflow.com/questions/41742142/limiting-fps-in-c
			//because if doing like in tutorial, it didn't work with mailbox.
			//maybe it's my problem, I mess up something :(
			//
			//Probably because of Matrices in c#!!!!
			//Everything speedups! change something with events! TODO!

			Vector3 rotate = Vector3.Zero;
			if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				//something is wrong!
				if (sdlEvent.key.keysym.sym == Keys.LookRight) 
					rotate.Y -= 1.0f;
				if (sdlEvent.key.keysym.sym == Keys.LookLeft)
					rotate.Y += 1.0f;

				if (sdlEvent.key.keysym.sym == Keys.LookUp)
					rotate.X -= 1.0f;
				if (sdlEvent.key.keysym.sym == Keys.LookDown)
					rotate.X += 1.0f;
			}

			if(Vector3.Dot(rotate, rotate) > float.Epsilon)
				gameObject.Transform.Rotation += LookSpeed * (float)dt * Vector3.Normalize(rotate);

			gameObject.Transform.Rotation.X = Math.Clamp(gameObject.Transform.Rotation.X, -1.5f, 1.5f);
			gameObject.Transform.Rotation.Y = gameObject.Transform.Rotation.Y % (MathF.PI * 2);

			float yaw = gameObject.Transform.Rotation.Y;
			Vector3 fowardDir = new(MathF.Sin(yaw), 0.0f, MathF.Cos(yaw));
			Vector3 rightDir = new(fowardDir.Z, 0.0f,-fowardDir.X);
			Vector3 upDir = new(0.0f, -1.0f, 0.0f);

			Vector3 moveDir = Vector3.Zero;
			if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				if (sdlEvent.key.keysym.sym == Keys.MoveForward)
					moveDir += fowardDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveBackward)
					moveDir -= fowardDir;

				if (sdlEvent.key.keysym.sym == Keys.MoveRight)
					moveDir -= rightDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveLeft)
					moveDir += rightDir;

				if (sdlEvent.key.keysym.sym == Keys.MoveUp)
					moveDir += upDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveDown)
					moveDir -= upDir;
			}

			if (Vector3.Dot(moveDir, moveDir) > float.Epsilon)
				gameObject.Transform.Translation += MoveSpeed * (float)dt * Vector3.Normalize(moveDir);

		}

	}
}