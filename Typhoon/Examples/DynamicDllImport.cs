using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Threading;

// from    https://code.google.com/archive/p/dynamicdllimport/downloads
namespace Typhoon
{
    internal class DynamicDllImportMetaObject : DynamicMetaObject
    {
        public DynamicDllImportMetaObject(Expression expression, object value)
            : base(expression, BindingRestrictions.Empty, value)
        {
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            Type returnType = GetMethodReturnType(binder);
            Type[] types = new Type[args.Length];
            Expression[] arguments = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Type type = args[i].LimitType;
                Expression expression = args[i].Expression;
                dynamic typedParameterExpression = expression;
                if (typedParameterExpression.IsByRef)
                {
                    types[i] = type.MakeByRefType();
                }
                else
                {
                    types[i] = type;
                }
                arguments[i] = expression;
            }
            MethodInfo method = (base.Value as DynamicDllImport).GetInvokeMethod(binder.Name, returnType, types);
            Expression callingExpression;
            if (method.ReturnType == typeof(void))
            {
                callingExpression = Expression.Block(Expression.Call(method, arguments), Expression.Default(typeof(object)));
            }
            else
            {
                callingExpression = Expression.Convert(Expression.Call(method, arguments), typeof(object));
            }
            BindingRestrictions bindingRestrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof(DynamicDllImport));
            return new DynamicMetaObject(callingExpression, bindingRestrictions);
        }

        private Type GetMethodReturnType(InvokeMemberBinder binder)
        {
            IList<Type> types = binder.GetType().GetField("m_typeArguments", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(binder) as IList<Type>;
            if ((types != null) && (types.Count > 0))
            {
                return types[0];
            }
            return null;
        }
    }

    public class DynamicDllImport : DynamicObject
    {
        internal string dllName;
        public string DllName
        {
            get
            {
                return dllName;
            }
        }

        public System.Runtime.InteropServices.CharSet CharSet = System.Runtime.InteropServices.CharSet.Auto;
        public System.Runtime.InteropServices.CallingConvention CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;

        public DynamicDllImport(string dllName)
        {
            this.dllName = dllName;
        }

        public DynamicDllImport(string dllName, System.Runtime.InteropServices.CharSet charSet = System.Runtime.InteropServices.CharSet.Auto, System.Runtime.InteropServices.CallingConvention callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)
        {
            this.dllName = dllName;
            this.CharSet = charSet;
            this.CallingConvention = callingConvention;
        }

        AssemblyBuilder assemblyBuilder;
        ModuleBuilder moduleBuilder;

        private string assemblyName;
        internal string AssemblyName
        {
            get
            {
                if (this.assemblyName == null)
                {
                    this.assemblyName = GetAssemblyName();
                }
                return this.assemblyName;
            }
            set
            {
                this.assemblyName = value;
            }
        }

        private string GetAssemblyName()
        {
            return (new System.IO.FileInfo(this.dllName)).Name;
        }

        private int methodIndex = 0;

        private string GetDefineTypeName(string methodName)
        {
            return string.Format("{0}_{1}", methodName, Interlocked.Increment(ref this.methodIndex));
        }

        public override DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter)
        {
            return new DynamicDllImportMetaObject(parameter, this);
        }

        public MethodInfo GetInvokeMethod(string methodName, Type returnType, Type[] types)
        {
            string entryName = methodName;
            if (assemblyBuilder == null)
            {
                AssemblyName assemblyName = new AssemblyName(AssemblyName);
                assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName);
            }

            var defineType = moduleBuilder.DefineType(GetDefineTypeName(methodName));
            var methodBuilder = defineType.DefinePInvokeMethod(methodName, dllName, entryName,
                       MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                       CallingConventions.Standard,
                       returnType, types,
                       CallingConvention, CharSet);
            if ((returnType != null) && (returnType != typeof(void)))
            {
                methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig | methodBuilder.GetMethodImplementationFlags());
            }
            var type = defineType.CreateType();

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            return method;
        }
    }
}
