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
		public float Angle = 0f;

		public Matrix4x4 Mat4() 
		{
			/*
			Matrix4x4 transform = Matrix4x4.CreateScale(Scale);
			
			Matrix4x4 transform1 = Matrix4x4.CreateRotationZ(Rotation.Z);
			Matrix4x4 transform2 = Matrix4x4.CreateRotationX(Rotation.X);
			Matrix4x4 transform3 = Matrix4x4.CreateRotationY(Rotation.Y);

			transform *= transform1;
			transform *= transform2;
			transform *= transform3;

			transform *= Matrix4x4.CreateTranslation(Translation);
			*/
			/*
			Matrix4x4 transform = Matrix4x4.CreateScale(Scale);

			Matrix4x4 transform1 = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angele));

			transform *= transform1;

			transform *= Matrix4x4.CreateTranslation(Translation);
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

		public Matrix4x4 NormalMatrix() 
		{
			float c3 = MathF.Cos(Rotation.Z);
			float s3 = MathF.Sin(Rotation.Z);
			float c2 = MathF.Cos(Rotation.X);
			float s2 = MathF.Sin(Rotation.X);
			float c1 = MathF.Cos(Rotation.Y);
			float s1 = MathF.Sin(Rotation.Y);

			Vector3 invScale = Vector3.One / Scale;

			Matrix4x4 mat4 = new()
			{
				M11 = invScale.X * (c1 * c3 + s1 * s2 * s3),
				M12 = invScale.X * (c2 * s3),
				M13 = invScale.X * (c1 * s2 * s3 - c3 * s1),
				M14 = 0.0f,

				M21 = invScale.Y * (c3 * s1 * s2 - c1 * s3),
				M22 = invScale.Y * (c2 * c3),
				M23 = invScale.Y * (c1 * c3 * s2 + s1 * s3),
				M24 = 0.0f,

				M31 = invScale.Z * (c2 * s1),
				M32 = invScale.Z * (-s2),
				M33 = invScale.Z * (c1 * c2),
				M34 = 0.0f,

				M41 = 0.0f,
				M42 = 0.0f,
				M43 = 0.0f,
				M44 = 1.0f
			};

			return mat4;
		}
	}

	public struct PointLightComponent 
	{
		public float lightIntensity = 1.0f;
	}

	public class GameObjectT
	{
		static public int IdT = 0;
		static public Dictionary<int, GameObjectT> Map = new();

		public int Id;

		public ModelT Model;
		public Vector3 Color;
		public TransformComponent Transform;

		public PointLightComponent? PointLight = null;

		public GameObjectT()
		{
			Id = IdT++;
		}
		public GameObjectT(Vector3? color, float intensity = 10.0f, float radius = 0.1f)
		{
			Id = IdT++;
			Color = (Vector3)color;
			Transform.Scale.X = radius;
			PointLight = new PointLightComponent() { lightIntensity = intensity };
		}
		public void CreateGameObject() { }

		public int GetId() 
		{
			return Id;
		}
	}
}
