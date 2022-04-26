using System;
using System.Numerics;

namespace TestVulkan
{
	public class CameraT
	{
		private Matrix4x4 ProjectionMatrix = Matrix4x4.Identity;
		private Matrix4x4 ViewMatrix = Matrix4x4.Identity;
		private Matrix4x4 InverseViewMatrix = Matrix4x4.Identity;

		public Matrix4x4 GetProjection => ProjectionMatrix;
		public Matrix4x4 GetView => ViewMatrix;
		public Matrix4x4 GetInverseView => InverseViewMatrix;
		public Vector3 GetPosition => new(InverseViewMatrix.M41, InverseViewMatrix.M42, InverseViewMatrix.M43);

		public CameraT()
		{
		}

		public void SetOrthographicProjection(float left, float right, float top, float bottom, float near, float far) 
		{
			//Matrix4x4.CreateOrthographicOffCenter
			
			ProjectionMatrix.M11 = 2.0f / (right - left);

			ProjectionMatrix.M22 = 2.0f / (bottom - top);

			ProjectionMatrix.M33 = 1.0f / (far - near);

			ProjectionMatrix.M41 = -(right + left) / (right - left);
			ProjectionMatrix.M42 = -(bottom + top) / (bottom - top);
			ProjectionMatrix.M43 = -near / (far - near);
			
		}

		public void SetPerspectiveProjection(float fovy, float aspect, float near, float far) 
		{
			//Matrix4x4.CreatePerspectiveFieldOfView

			float tanHalfFovy = MathF.Tan(fovy / 2.0f);

			ProjectionMatrix = new Matrix4x4();
			ProjectionMatrix.M11 = 1.0f / (aspect * tanHalfFovy);
			ProjectionMatrix.M22 = 1.0f / (tanHalfFovy);
			ProjectionMatrix.M33 = far / (far - near);
			ProjectionMatrix.M34 = 1.0f;
			ProjectionMatrix.M43 = -(far * near) / (far - near);


			//var ProjectionMatrix1 = Matrix4x4.CreatePerspectiveFieldOfView(fovy,aspect,near,far);
			//ProjectionMatrix = ProjectionMatrix1;
		}

		public void SetViewDirection(Vector3 position, Vector3 direction, Vector3? up) 
		{
			if (up == null)
				up = new Vector3(0.0f,-1.0f,0.0f);

			Vector3 w = Vector3.Normalize(direction);
			Vector3 u = Vector3.Normalize(Vector3.Cross(w, (Vector3)up));
			Vector3 v = Vector3.Cross(w,u);

			ViewMatrix = Matrix4x4.Identity;
			ViewMatrix.M11 = u.X;
			ViewMatrix.M21 = u.Y;
			ViewMatrix.M31 = u.Z;
			ViewMatrix.M12 = v.X;
			ViewMatrix.M22 = v.Y;
			ViewMatrix.M32 = v.Z;
			ViewMatrix.M13 = w.X;
			ViewMatrix.M23 = w.Y;
			ViewMatrix.M33 = w.Z;
			ViewMatrix.M41 = -Vector3.Dot(u,position);
			ViewMatrix.M42 = -Vector3.Dot(v, position);
			ViewMatrix.M43 = -Vector3.Dot(w, position);

			InverseViewMatrix = Matrix4x4.Identity;
			InverseViewMatrix.M11 = u.X;
			InverseViewMatrix.M12 = u.Y;
			InverseViewMatrix.M13 = u.Z;
			InverseViewMatrix.M21 = v.X;
			InverseViewMatrix.M22 = v.Y;
			InverseViewMatrix.M23 = v.Z;
			InverseViewMatrix.M31 = w.X;
			InverseViewMatrix.M32 = w.Y;
			InverseViewMatrix.M33 = w.Z;
			InverseViewMatrix.M41 = position.X;
			InverseViewMatrix.M42 = position.Y;
			InverseViewMatrix.M43 = position.Z;
		}

		public void SetViewTarget(Vector3 position, Vector3 target, Vector3? up)
		{
			//Matrix4x4.CreateLookAt
			if (up == null)
				up = new Vector3(0.0f, -1.0f, 0.0f);

			SetViewDirection(position, target - position, up);

		}

		public void SetViewYXZ(Vector3 position, Vector3 rotation)
		{
			float c3 = MathF.Cos(rotation.Z);
			float s3 = MathF.Sin(rotation.Z);
			float c2 = MathF.Cos(rotation.X);
			float s2 = MathF.Sin(rotation.X);
			float c1 = MathF.Cos(rotation.Y);
			float s1 = MathF.Sin(rotation.Y);

			Vector3 u = new(c1 * c3 + s1 * s2 * s3, c2 * s3, c1 * s2 * s3 - c3 * s1);
			Vector3 v = new(c3 * s1 * s2 - c1 * s3, c2 * c3, c1 * c3 * s2 + s1 * s3);
			Vector3 w = new(c2 * s1, -s2, c1 * c2);

			ViewMatrix = Matrix4x4.Identity;

			ViewMatrix.M11 = u.X;
			ViewMatrix.M21 = u.Y;
			ViewMatrix.M31 = u.Z;
			ViewMatrix.M12 = v.X;
			ViewMatrix.M22 = v.Y;
			ViewMatrix.M32 = v.Z;
			ViewMatrix.M13 = w.X;
			ViewMatrix.M23 = w.Y;
			ViewMatrix.M33 = w.Z;
			ViewMatrix.M41 = -Vector3.Dot(u, position);
			ViewMatrix.M42 = -Vector3.Dot(v, position);
			ViewMatrix.M43 = -Vector3.Dot(w, position);

			InverseViewMatrix = Matrix4x4.Identity;
			InverseViewMatrix.M11 = u.X;
			InverseViewMatrix.M12 = u.Y;
			InverseViewMatrix.M13 = u.Z;
			InverseViewMatrix.M21 = v.X;
			InverseViewMatrix.M22 = v.Y;
			InverseViewMatrix.M23 = v.Z;
			InverseViewMatrix.M31 = w.X;
			InverseViewMatrix.M32 = w.Y;
			InverseViewMatrix.M33 = w.Z;
			InverseViewMatrix.M41 = position.X;
			InverseViewMatrix.M42 = position.Y;
			InverseViewMatrix.M43 = position.Z;
		}
	}
}
