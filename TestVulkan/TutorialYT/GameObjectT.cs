using System;
using System.Numerics;
using System.Diagnostics;

namespace TestVulkan
{
	public struct TransformComponent 
	{
		public Vector3 Translation;
		public Vector3 Scale = new(1.0f, 1.0f, 1.0f);
		public Vector3 Rotation;
		public Matrix4x4 Mat4() 
		{

			/*
			Matrix4x4 tramsform = Matrix4x4.CreateScale(Scale);
			
			Matrix4x4 tramsform1 = Matrix4x4.CreateRotationZ(Rotation.Z);
			Matrix4x4 tramsform2 = Matrix4x4.CreateRotationX(Rotation.X);
			Matrix4x4 tramsform3 = Matrix4x4.CreateRotationY(Rotation.Y);

			tramsform *= tramsform1;
			tramsform *= tramsform2;
			tramsform *= tramsform3;

			tramsform *= Matrix4x4.CreateTranslation(Translation);
			*/

			float c3 = MathF.Cos(Rotation.Z);
			float s3 = MathF.Sin(Rotation.Z);
			float c2 = MathF.Cos(Rotation.X);
			float s2 = MathF.Sin(Rotation.X);
			float c1 = MathF.Cos(Rotation.Y);
			float s1 = MathF.Sin(Rotation.Y);

			Matrix4x4 mat4 = new() 
			{
				M11 = Scale.X * (c1 * c3 + s1 * s2 * s3),
				M12 = Scale.X * (c2 * s3),
				M13 = Scale.X * (c1 * s2 * s3 - c3 * s1),
				M14 = 0.0f,

				M21 = Scale.Y * (c3 * s1 * s2 - c1 * s3),
				M22 = Scale.Y * (c2 * c3),
				M23 = Scale.Y * (c1 * c3 * s2 + s1 * s3),
				M24 = 0.0f,

				M31 = Scale.Z * (c2 * s1),
				M32 = Scale.Z * (-s2),
				M33 = Scale.Z * (c1 * c2),
				M34 = 0.0f,

				M41 = Translation.X,
				M42 = Translation.Y,
				M43 = Translation.Z,
				M44 = 1.0f
			};

			return mat4;
		}
	}

	public class GameObjectT
	{
		static public int IdT = 0;
		public int Id;

		public ModelT Model;
		public Vector3 Color;
		public TransformComponent Transform;
		public GameObjectT()
		{
			Id = IdT++;
		}

		public void CreateGameObject() { }

		public int GetId() 
		{
			return Id;
		}
	}
}
