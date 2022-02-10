using System;
using System.Diagnostics;
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

		public float MoveSpeed = 3.0f;
		public float LookSpeed = 1.5f;


		private Vector3 Rotate = Vector3.Zero;
		private Vector3 MoveDir = Vector3.Zero;
		public KeyboardMovementController()
		{
		}

		public void MovePlaneXZ(ref SDL.SDL_Event sdlEvent, float dt, ref GameObjectT gameObject)
		{
			if (sdlEvent.key.repeat == 1)
				return;

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

			if (Vector3.Dot(rotate, rotate) > float.Epsilon)
				gameObject.Transform.Rotation += LookSpeed * dt * Vector3.Normalize(rotate);

			gameObject.Transform.Rotation.X = Math.Clamp(gameObject.Transform.Rotation.X, -1.5f, 1.5f);
			gameObject.Transform.Rotation.Y = gameObject.Transform.Rotation.Y % (MathF.PI * 2);

			float yaw = gameObject.Transform.Rotation.Y;
			Vector3 fowardDir = new(MathF.Sin(yaw), 0.0f, MathF.Cos(yaw));
			Vector3 rightDir = new(fowardDir.Z, 0.0f, -fowardDir.X);
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
				gameObject.Transform.Translation += MoveSpeed * dt * Vector3.Normalize(moveDir);

		}
		public void MovePlaneXZ2(ref SDL.SDL_Event sdlEvent, ref GameObjectT gameObject)
		{
			//if (sdlEvent.key.repeat == 1)
			//{
			//	Rotate = Vector3.Zero;
			//	MoveDir = Vector3.Zero;
			//	return;
			//}

			if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				//Trace.WriteLine(sdlEvent.key.repeat);
				//if (sdlEvent.key.repeat == 1)
				//{
				//	Rotate = Vector3.Zero;
				//	MoveDir = Vector3.Zero;
				//	return;
				//}

				Rotate = Vector3.Zero;
				if (sdlEvent.key.keysym.sym == Keys.LookRight)
					Rotate.Y += 1.0f;
				if (sdlEvent.key.keysym.sym == Keys.LookLeft)
					Rotate.Y -= 1.0f;

				if (sdlEvent.key.keysym.sym == Keys.LookUp)
					Rotate.X += 1.0f;
				if (sdlEvent.key.keysym.sym == Keys.LookDown)
					Rotate.X -= 1.0f;

				float yaw = gameObject.Transform.Rotation.Y;
				Vector3 fowardDir = new(MathF.Sin(yaw), 0.0f, MathF.Cos(yaw));
				Vector3 rightDir = new(fowardDir.Z, 0.0f, -fowardDir.X);
				Vector3 upDir = new(0.0f, -1.0f, 0.0f);

				MoveDir = Vector3.Zero;
				if (sdlEvent.key.keysym.sym == Keys.MoveForward)
					MoveDir += fowardDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveBackward)
					MoveDir -= fowardDir;

				if (sdlEvent.key.keysym.sym == Keys.MoveRight)
					MoveDir += rightDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveLeft)
					MoveDir -= rightDir;

				if (sdlEvent.key.keysym.sym == Keys.MoveUp)
					MoveDir += upDir;
				if (sdlEvent.key.keysym.sym == Keys.MoveDown)
					MoveDir -= upDir;
			}

			if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYUP)
			{
				Rotate = Vector3.Zero;
				MoveDir = Vector3.Zero;
			}
		}

		public void MovePlaneXZ3(float dt, ref GameObjectT gameObject)
		{
			if (Vector3.Dot(Rotate, Rotate) > float.Epsilon)
				gameObject.Transform.Rotation += LookSpeed * dt * Vector3.Normalize(Rotate);

			gameObject.Transform.Rotation.X = Math.Clamp(gameObject.Transform.Rotation.X, -1.5f, 1.5f);
			gameObject.Transform.Rotation.Y = gameObject.Transform.Rotation.Y % (MathF.PI * 2);

			if (Vector3.Dot(MoveDir, MoveDir) > float.Epsilon)
				gameObject.Transform.Translation += MoveSpeed * dt * Vector3.Normalize(MoveDir);

		}
	}
}