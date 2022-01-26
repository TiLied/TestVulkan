using System.Runtime.InteropServices;

namespace TestVulkan
{
    public static class Help
    {
        private static List<IntPtr> intPtrs = new();

		unsafe public static IntPtr ReturnIntPtr(this string? str)
		{
			IntPtr ptr = Marshal.StringToHGlobalAnsi(str);
			intPtrs.Add(ptr);
			return ptr;
		}

		unsafe public static IntPtr* ReturnIntPtrPointerArray(this string?[] arrayStr)
		{
			IntPtr array = Marshal.AllocHGlobal(IntPtr.Size * arrayStr.Length);

			IntPtr* pArray = (IntPtr*)array.ToPointer();

			for (int i = 0; i < arrayStr.Length; ++i)
			{
				IntPtr ptr = Marshal.StringToHGlobalAnsi(arrayStr[i]);
				intPtrs.Add(ptr);
				pArray[i] = ptr;
			}

			intPtrs.Add(array);

			return (IntPtr*)array;
		}

		unsafe public static IntPtr* ReturnIntPtrPointerArrayElements<T>(T[] arrayElements)
		{
			//not working, dont know why :(
			IntPtr array = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>() * arrayElements.Length);

			IntPtr* pArray = (IntPtr*)array.ToPointer();

			for (int i = 0; i < arrayElements.Length; ++i)
			{
				IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
				Marshal.StructureToPtr(arrayElements[i], ptr, false);
				intPtrs.Add(ptr);
				pArray[i] = ptr;
			}

			intPtrs.Add(array);

			return (IntPtr*)array;
		}
		public static void FreeMemory() 
		{
			foreach (IntPtr intPtr in intPtrs)
			{
				Marshal.FreeHGlobal(intPtr);
			}

			intPtrs.Clear();
		}

		public static byte[] ReadFile(string filename) 
		{
			byte[] bytes = File.ReadAllBytes(filename);

			return bytes;
		}
	}
}