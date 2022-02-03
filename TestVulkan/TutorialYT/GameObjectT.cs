using System;
using System.Numerics;

namespace TestVulkan
{
	public struct Transform2dComponent 
	{
		public Vector2 Translation;
		public Vector2 Scale = new(1.0f, 1.0f);
		public float Rotation;
		public Matrix4x4 Mat4() 
		{
			float s = MathF.Sin(Rotation);
			float c = MathF.Cos(Rotation);

			Matrix4x4 rotMat = new();
			rotMat.M11 = c;
			rotMat.M12 = s;
			rotMat.M21 = -s;
			rotMat.M22 = c;

			Matrix4x4 rotMat2 = Matrix4x4.CreateFromAxisAngle(new Vector3(Translation, 1.0f), Rotation);
			
			Matrix4x4 rotMatx = Matrix4x4.CreateRotationX(Rotation, Vector3.Zero);
			Matrix4x4 rotMaty = Matrix4x4.CreateRotationY(Rotation, Vector3.Zero);
			//
			//this one!
			Matrix4x4 rotMatz = Matrix4x4.CreateRotationZ(Rotation, Vector3.Zero);

			Matrix4x4 scaleMat = Matrix4x4.CreateScale(new Vector3(Scale, 1.0f));

			return scaleMat * rotMatz;
		}
	}

	public class GameObjectT
	{
		static public int IdT = 0;
		public int Id;

		public ModelT Model;
		public Vector3 Color;
		public Transform2dComponent Transform2D;
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
