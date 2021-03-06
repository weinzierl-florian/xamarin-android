using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Android.Runtime {
	internal static class ConstructorBuilder {
		static readonly MethodInfo newobject =
#if NETFRAMEWORK || NET_2_0
			typeof (System.Runtime.Serialization.FormatterServices).GetMethod ("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static)!;
#else
			typeof (System.Runtime.CompilerServices.RuntimeHelpers).GetMethod ("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static)!;
#endif
		static MethodInfo gettype = typeof (System.Type).GetMethod ("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static)!;
		static FieldInfo handlefld = typeof (Java.Lang.Object).GetField ("handle", BindingFlags.NonPublic | BindingFlags.Instance)!;
		static FieldInfo Throwable_handle = typeof (Java.Lang.Throwable).GetField ("handle", BindingFlags.NonPublic | BindingFlags.Instance)!;


		internal static Action <IntPtr, object? []?> CreateDelegate (ConstructorInfo cinfo) {
			var type = cinfo.DeclaringType;
			var handle = handlefld;
			if (typeof (Java.Lang.Throwable).IsAssignableFrom (type)) {
				handle = Throwable_handle;
			}

			DynamicMethod method = new DynamicMethod (DynamicMethodNameCounter.GetUniqueName (), typeof (void), new Type [] {typeof (IntPtr), typeof (object []) }, typeof (DynamicMethodNameCounter), true);
			ILGenerator il = method.GetILGenerator ();

			il.DeclareLocal (typeof (object));

			il.Emit (OpCodes.Ldtoken, type);
			il.Emit (OpCodes.Call, gettype);
			il.Emit (OpCodes.Call, newobject);
			il.Emit (OpCodes.Stloc_0);
			il.Emit (OpCodes.Ldloc_0);
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stfld, handle);

			il.Emit (OpCodes.Ldloc_0);

			var len = cinfo.GetParameters ().Length;
			for (int i = 0; i < len; i++) {
				il.Emit (OpCodes.Ldarg, 1);
				il.Emit (OpCodes.Ldc_I4, i);
				il.Emit (OpCodes.Ldelem_Ref);
			}
			il.Emit (OpCodes.Call, cinfo);

			il.Emit (OpCodes.Ret);

			return (Action<IntPtr, object?[]?>) method.CreateDelegate (typeof (Action <IntPtr, object []>));
		}
	}
}
