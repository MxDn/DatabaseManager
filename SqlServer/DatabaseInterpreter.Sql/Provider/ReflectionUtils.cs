using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DatabaseInterpreter.Core
{
    public static class ReflectionUtils
    {
        /// <summary>
        ///     Binding Flags constant to be reused for all Reflection access methods.
        /// </summary>
        public const BindingFlags MemberAccess = BindingFlags.IgnoreCase | BindingFlags.Instance |
                                                 BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public const BindingFlags MemberAccessCom =
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        ///     Calls a method on an object dynamically. This version requires explicit
        ///     specification of the parameter type signatures.
        /// </summary>
        /// <param name="instance">Instance of object to call method on</param>
        /// <param name="method">The method to call as a stringToTypedValue</param>
        /// <param name="parameterTypes">
        ///     Specify each of the types for each parameter passed.
        ///     You can also pass null, but you may get errors for ambiguous methods signatures
        ///     when null parameters are passed
        /// </param>
        /// <param name="parms">any variable number of parameters.</param>
        /// <returns>object</returns>
        public static object CallMethod(
            object instance,
            string method,
            Type[] parameterTypes,
            params object[] parms)
        {
            return parameterTypes == null && parms.Length != 0
                ? instance.GetType().GetMethod(method,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.InvokeMethod).Invoke(instance, parms)
                : instance.GetType().GetMethod(method,
                        BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, parameterTypes, null)
                    .Invoke(instance, parms);
        }

        /// <summary>
        ///     Calls a method on an object dynamically.
        ///     This version doesn't require specific parameter signatures to be passed.
        ///     Instead parameter types are inferred based on types passed. Note that if
        ///     you pass a null parameter, type inferrance cannot occur and if overloads
        ///     exist the call may fail. if so use the more detailed overload of this method.
        /// </summary>
        /// <param name="instance">Instance of object to call method on</param>
        /// <param name="method">The method to call as a stringToTypedValue</param>
        /// <param name="parameterTypes">
        ///     Specify each of the types for each parameter passed.
        ///     You can also pass null, but you may get errors for ambiguous methods signatures
        ///     when null parameters are passed
        /// </param>
        /// <param name="parms">any variable number of parameters.</param>
        /// <returns>object</returns>
        public static object CallMethod(object instance, string method, params object[] parms)
        {
            var parameterTypes = (Type[])null;
            if (parms != null)
            {
                parameterTypes = new Type[parms.Length];
                for (var index = 0; index < parms.Length; ++index)
                {
                    if (parms[index] == null)
                    {
                        parameterTypes = null;
                        break;
                    }

                    parameterTypes[index] = parms[index].GetType();
                }
            }

            return ReflectionUtils.CallMethod(instance, method, parameterTypes, parms);
        }

        /// <summary>
        ///     Wrapper method to call a 'dynamic' (non-typelib) method
        ///     on a COM object
        /// </summary>
        /// <param name="params"></param>
        /// <param name="instance"></param>
        /// 1st - Method name, 2nd - 1st parameter, 3rd - 2nd parm etc.
        /// <returns></returns>
        public static object CallMethodCom(object instance, string method, params object[] parms)
        {
            return instance.GetType().InvokeMember(method,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
                null, instance, parms);
        }

        /// <summary>
        ///     Calls a method on an object with extended . syntax (object: this Method: Entity.CalculateOrderTotal)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="method"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        public static object CallMethodEx(object parent, string method, params object[] parms)
        {
            parent.GetType();
            var length = method.IndexOf(".");
            if (length < 0)
            {
                return CallMethod(parent, method, parms);
            }

            var Property = method.Substring(0, length);
            var method1 = method.Substring(length + 1);
            return CallMethodEx(GetPropertyInternal(parent, Property), method1, parms);
        }

        /// <summary>
        ///     Calls a method on a COM object with '.' syntax (Customer instance and Address.DoSomeThing method)
        /// </summary>
        /// <param name="parent">the object instance on which to call method</param>
        /// <param name="method">The method or . syntax path to the method (Address.Parse)</param>
        /// <param name="parms">Any number of parameters</param>
        /// <returns></returns>
        public static object CallMethodExCom(object parent, string method, params object[] parms)
        {
            parent.GetType();
            var length = method.IndexOf(".");
            if (length < 0)
            {
                return CallMethodCom(parent, method, parms);
            }

            var name = method.Substring(0, length);
            var method1 = method.Substring(length + 1);
            return CallMethodExCom(
                parent.GetType().InvokeMember(name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.GetProperty, null, parent, null), method1, parms);
        }

        /// <summary>
        ///     Creates a COM instance from a ProgID. Loads either
        ///     Exe or DLL servers.
        /// </summary>
        /// <param name="progId"></param>
        /// <returns></returns>
        public static object CreateComInstance(string progId)
        {
            var typeFromProgId = Type.GetTypeFromProgID(progId);
            return typeFromProgId == (Type)null ? null : Activator.CreateInstance(typeFromProgId);
        }

        /// <summary>
        ///     Creates an instance of a type based on a string. Assumes that the type's
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CreateInstanceFromString(string typeName, params object[] args)
        {
            try
            {
                var typeFromName = ReflectionUtils.GetTypeFromName(typeName);
                return typeFromName == (Type)null ? null : Activator.CreateInstance(typeFromName, args);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Creates an instance from a type by calling the parameterless constructor.
        ///     Note this will not work with COM objects - continue to use the Activator.CreateInstance
        ///     for COM objects.
        ///     <seealso>Class wwUtils</seealso>
        /// </summary>
        /// <param name="typeToCreate">
        ///     The type from which to create an instance.
        /// </param>
        /// <returns>object</returns>
        public static object CreateInstanceFromType(Type typeToCreate, params object[] args)
        {
            if (args != null)
            {
                return Activator.CreateInstance(typeToCreate, args);
            }

            var emptyTypes = Type.EmptyTypes;
            return typeToCreate.GetConstructor(emptyTypes).Invoke(null);
        }

        /// <summary>Returns a List of KeyValuePair object</summary>
        /// <param name="enumeration"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetEnumList(
            Type enumType,
            bool valueAsFieldValueNumber = false)
        {
            var values = Enum.GetValues(enumType);
            var keyValuePairList = new List<KeyValuePair<string, string>>();
            foreach (var obj in values)
            {
                var camelCase = obj.ToString();
                if (!valueAsFieldValueNumber)
                {
                    keyValuePairList.Add(new KeyValuePair<string, string>(obj.ToString(),
                        StringUtils.FromCamelCase(camelCase)));
                }
                else
                {
                    keyValuePairList.Add(new KeyValuePair<string, string>(((int)obj).ToString(),
                        StringUtils.FromCamelCase(camelCase)));
                }
            }

            return keyValuePairList;
        }

        /// <summary>
        ///     Retrieve a field dynamically from an object. This is a simple implementation that's
        ///     straight Reflection and doesn't support indexers.
        /// </summary>
        /// <param name="Object">Object to retreve Field from</param>
        /// <param name="Property">name of the field to retrieve</param>
        /// <returns></returns>
        public static object GetField(object Object, string Property)
        {
            return Object.GetType().GetField(Property,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.GetField).GetValue(Object);
        }

        /// <summary>
        ///     Retrieve a property value from an object dynamically. This is a simple version
        ///     that uses Reflection calls directly. It doesn't support indexers.
        /// </summary>
        /// <param name="instance">Object to make the call on</param>
        /// <param name="property">Property to retrieve</param>
        /// <returns>Object - cast to proper type</returns>
        public static object GetProperty(object instance, string property)
        {
            try
            {
                return instance.GetType().GetProperty(property,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).GetValue(instance, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Retrieve a dynamic 'non-typelib' property</summary>
        /// <param name="instance">Object to make the call on</param>
        /// <param name="property">Property to retrieve</param>
        /// <returns></returns>
        public static object GetPropertyCom(object instance, string property)
        {
            return instance.GetType().InvokeMember(property,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
                null, instance, null);
        }

        /// <summary>
        ///     Returns a property or field value using a base object and sub members including . syntax.
        ///     For example, you can access: oCustomer.oData.Company with (this,"oCustomer.oData.Company")
        ///     This method also supports indexers in the Property value such as:
        ///     Customer.DataSet.Tables["Customers"].Rows[0]
        /// </summary>
        /// <param name="Parent">Parent object to 'start' parsing from. Typically this will be the Page.</param>
        /// <param name="Property">The property to retrieve. Example: 'Customer.Entity.Company'</param>
        /// <returns></returns>
        public static object GetPropertyEx(object Parent, string Property)
        {
            Parent.GetType();
            var length = Property.IndexOf(".");
            if (length < 0)
            {
                return GetPropertyInternal(Parent, Property);
            }

            var Property1 = Property.Substring(0, length);
            var Property2 = Property.Substring(length + 1);
            return ReflectionUtils.GetPropertyEx(GetPropertyInternal(Parent, Property1),
                Property2);
        }

        /// <summary>
        ///     Returns a property or field value using a base object and sub members including . syntax.
        ///     For example, you can access: oCustomer.oData.Company with (this,"oCustomer.oData.Company")
        /// </summary>
        /// <param name="parent">Parent object to 'start' parsing from.</param>
        /// <param name="property">The property to retrieve. Example: 'oBus.oData.Company'</param>
        /// <returns></returns>
        public static object GetPropertyExCom(object parent, string property)
        {
            parent.GetType();
            var length = property.IndexOf(".");
            if (length < 0)
            {
                return property == "this" || property == "me"
                    ? parent
                    : parent.GetType().InvokeMember(property,
                        BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.GetProperty, null, parent, null);
            }

            var name = property.Substring(0, length);
            var property1 = property.Substring(length + 1);
            return GetPropertyExCom(
                parent.GetType().InvokeMember(name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.GetProperty, null, parent, null), property1);
        }

        /// <summary>
        ///     Returns a PropertyInfo object for a given dynamically accessed property
        ///     Property selection can be specified using . syntax ("Address.Street" or "DataTable[0].Rows[1]") hence the 'Ex' name
        ///     for this function.
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="Property"></param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfoEx(object Parent, string Property)
        {
            Parent.GetType();
            var length = Property.IndexOf(".");
            if (length < 0)
            {
                return ReflectionUtils.GetPropertyInfoInternal(Parent, Property);
            }

            var Property1 = Property.Substring(0, length);
            var Property2 = Property.Substring(length + 1);
            return GetPropertyInfoEx(GetPropertyInternal(Parent, Property1), Property2);
        }

        /// <summary>
        ///     Returns a PropertyInfo structure from an extended Property reference
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="Property"></param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfoInternal(object Parent, string Property)
        {
            if (Property == "this" || Property == "me")
            {
                return null;
            }

            var name = Property;
            if (Property.IndexOf("[") > -1)
            {
                name = Property.Substring(0, Property.IndexOf("["));
            }

            return Parent.GetType().GetProperty(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic);
        }

        /// <summary>
        ///     Retrieves a value from  a static property by specifying a type full name and property
        /// </summary>
        /// <param name="typeName">Full type name (namespace.class)</param>
        /// <param name="property">Property to get value from</param>
        /// <returns></returns>
        public static object GetStaticProperty(string typeName, string property)
        {
            var typeFromName = ReflectionUtils.GetTypeFromName(typeName);
            return typeFromName == (Type)null
                ? null
                : ReflectionUtils.GetStaticProperty(typeFromName, property);
        }

        /// <summary>Returns a static property from a given type</summary>
        /// <param name="type">Type instance for the static property</param>
        /// <param name="property">Property name as a string</param>
        /// <returns></returns>
        public static object GetStaticProperty(Type type, string property)
        {
            try
            {
                return type.InvokeMember(property,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty,
                    null, type, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Helper routine that looks up a type name and tries to retrieve the
        ///     full type reference using GetType() and if not found looking
        ///     in the actively executing assemblies and optionally loading
        ///     the specified assembly name.
        /// </summary>
        /// <param name="typeName">type to load</param>
        /// <param name="assemblyName">
        ///     Optional assembly name to load from if type cannot be loaded initially.
        ///     Use for lazy loading of assemblies without taking a type dependency.
        /// </param>
        /// <returns>null</returns>
        public static Type GetTypeFromName(string typeName, string assemblyName)
        {
            var type1 = Type.GetType(typeName, false);
            if (type1 != null)
            {
                return type1;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type1 = assembly.GetType(typeName, false);
                if (type1 != null)
                {
                    break;
                }
            }

            if (type1 != null)
            {
                return type1;
            }

            if (!string.IsNullOrEmpty(assemblyName) &&
                ReflectionUtils.LoadAssembly(assemblyName) != null)
            {
                var type2 = Type.GetType(typeName, false);
                if (type2 != null)
                {
                    return type2;
                }
            }

            return null;
        }

        /// <summary>
        ///     Overload for backwards compatibility which only tries to load
        ///     assemblies that are already loaded in memory.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetTypeFromName(string typeName)
        {
            return ReflectionUtils.GetTypeFromName(typeName, null);
        }

        /// <summary>
        ///     Allows invoking an event from an external classes where direct access
        ///     is not allowed (due to 'Can only assign to left hand side of operation')
        /// </summary>
        /// <param name="instance">Instance of the object hosting the event</param>
        /// <param name="eventName">Name of the event to invoke</param>
        /// <param name="parameters">Optional parameters to the event handler to be invoked</param>
        public static void InvokeEvent(object instance, string eventName, params object[] parameters)
        {
            foreach (var invocation in ((Delegate)instance.GetType()
                    .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance))
                .GetInvocationList())
            {
                invocation.Method.Invoke(invocation.Target, parameters);
            }
        }

        /// <summary>
        ///     Try to load an assembly into the application's app domain.
        ///     Loads by name first then checks for filename
        /// </summary>
        /// <param name="assemblyName">Assembly name or full path</param>
        /// <returns>null on failure</returns>
        public static Assembly LoadAssembly(string assemblyName)
        {
            var assembly1 = (Assembly)null;
            try
            {
                assembly1 = Assembly.Load(assemblyName);
            }
            catch
            {
            }

            if (assembly1 != null)
            {
                return assembly1;
            }

            if (File.Exists(assemblyName))
            {
                var assembly2 = Assembly.LoadFrom(assemblyName);
                if (assembly2 != null)
                {
                    return assembly2;
                }
            }

            return null;
        }

        /// <summary>
        ///     Sets the field on an object. This is a simple method that uses straight Reflection
        ///     and doesn't support indexers.
        /// </summary>
        /// <param name="obj">Object to set property on</param>
        /// <param name="property">Name of the field to set</param>
        /// <param name="value">value to set it to</param>
        public static void SetField(object obj, string property, object value)
        {
            obj.GetType().GetField(property,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(obj, value);
        }

        /// <summary>
        ///     Sets the property on an object. This is a simple method that uses straight Reflection
        ///     and doesn't support indexers.
        /// </summary>
        /// <param name="obj">Object to set property on</param>
        /// <param name="property">Name of the property to set</param>
        /// <param name="value">value to set it to</param>
        public static void SetProperty(object obj, string property, object value)
        {
            obj.GetType()
                .GetProperty(property,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).SetValue(obj, value, null);
        }

        /// <summary>Sets the property on an object.</summary>
        /// <param name="inst">Object to set property on</param>
        /// <param name="Property">Name of the property to set</param>
        /// <param name="Value">value to set it to</param>
        public static void SetPropertyCom(object inst, string Property, object Value)
        {
            inst.GetType().InvokeMember(Property,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                null, inst, new object[1]
                {
                    Value
                });
        }

        /// <summary>
        ///     Sets a value on an object. Supports . syntax for named properties
        ///     (ie. Customer.Entity.Company) as well as indexers.
        /// </summary>
        /// <param name="Object ParentParent">Object to set the property on.</param>
        /// <param name="String PropertyProperty">
        ///     Property to set. Can be an object hierarchy with . syntax and can
        ///     include indexers. Examples: Customer.Entity.Company,
        ///     Customer.DataSet.Tables["Customers"].Rows[0]
        /// </param>
        /// <param name="Object ValueValue">Value to set the property to</param>
        public static object SetPropertyEx(object parent, string property, object value)
        {
            parent.GetType();
            var length = property.IndexOf(".");
            if (length < 0)
            {
                SetPropertyInternal(parent, property, value);
                return null;
            }

            var Property = property.Substring(0, length);
            var property1 = property.Substring(length + 1);
            SetPropertyEx(GetPropertyInternal(parent, Property), property1, value);
            return null;
        }

        /// <summary>
        ///     Sets the value of a field or property via Reflection. This method alws
        ///     for using '.' syntax to specify objects multiple levels down.
        ///     ReflectionUtils.SetPropertyEx(this,"Invoice.LineItemsCount",10)
        ///     which would be equivalent of:
        ///     Invoice.LineItemsCount = 10;
        /// </summary>
        /// <param name="Object ParentParent">Object to set the property on.</param>
        /// <param name="String PropertyProperty">
        ///     Property to set. Can be an object hierarchy with . syntax.
        /// </param>
        /// <param name="Object ValueValue">Value to set the property to</param>
        public static object SetPropertyExCom(object parent, string property, object value)
        {
            parent.GetType();
            var length = property.IndexOf(".");
            if (length < 0)
            {
                parent.GetType().InvokeMember(property,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.SetProperty, null, parent, new object[1]
                    {
                        value
                    });
                return null;
            }

            var name = property.Substring(0, length);
            var property1 = property.Substring(length + 1);
            return SetPropertyExCom(
                parent.GetType().InvokeMember(name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.GetProperty, null, parent, null), property1, value);
        }

        /// <summary>
        ///     Turns a string into a typed value generically.
        ///     Explicitly assigns common types and falls back
        ///     on using type converters for unhandled types.
        ///     Common uses:
        ///     * UI -&gt; to data conversions
        ///     * Parsers
        ///     <seealso>Class ReflectionUtils</seealso>
        /// </summary>
        /// <param name="sourceString">The string to convert from</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="culture">
        ///     Culture used for numeric and datetime values.
        /// </param>
        /// <returns>object. Throws exception if it cannot be converted.</returns>
        public static object StringToTypedValue(
            string sourceString,
            Type targetType,
            CultureInfo culture = null)
        {
            var flag = string.IsNullOrEmpty(sourceString);
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            if (targetType == typeof(string))
            {
                return sourceString;
            }

            if (targetType == typeof(int) || targetType == typeof(int))
            {
                return flag ? 0 : (object)int.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(long))
            {
                return flag ? 0L : (object)long.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(short))
            {
                return flag
                    ? (short)0
                    : (object)short.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(decimal))
            {
                return flag
                    ? decimal.Zero
                    : (object)decimal.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(DateTime))
            {
                return flag ? DateTime.MinValue : (object)Convert.ToDateTime(sourceString, culture.DateTimeFormat);
            }

            if (targetType == typeof(byte))
            {
                return flag ? 0 : (object)Convert.ToByte(sourceString);
            }

            if (targetType == typeof(double))
            {
                return flag ? 0.0f : (object)double.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(float))
            {
                return flag ? 0.0f : (object)float.Parse(sourceString, NumberStyles.Any, culture.NumberFormat);
            }

            if (targetType == typeof(bool))
            {
                sourceString = sourceString.ToLower();
                return !flag && sourceString == "true" || sourceString == "on" || sourceString == "1" ||
                       sourceString == "yes"
                    ? true
                    : (object)false;
            }

            if (targetType == typeof(Guid))
            {
                return flag ? Guid.Empty : (object)new Guid(sourceString);
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, sourceString);
            }

            if (targetType == typeof(byte[]))
            {
                return null;
            }

            if (targetType.Name.StartsWith("Nullable`"))
            {
                if (sourceString.ToLower() == "null" || sourceString == string.Empty)
                {
                    return null;
                }

                targetType = Nullable.GetUnderlyingType(targetType);
                return ReflectionUtils.StringToTypedValue(sourceString, targetType);
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromString(null, culture, sourceString);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        ///     Generic version allow for automatic type conversion without the explicit type
        ///     parameter
        /// </summary>
        /// <typeparam name="T">Type to be converted to</typeparam>
        /// <param name="sourceString">input string value to be converted</param>
        /// <param name="culture">Culture applied to conversion</param>
        /// <returns></returns>
        public static T StringToTypedValue<T>(string sourceString, CultureInfo culture = null)
        {
            return (T)ReflectionUtils.StringToTypedValue(sourceString, typeof(T), culture);
        }

        /// <summary>
        ///     Converts a type to string if possible. This method supports an optional culture generically on any value.
        ///     It calls the ToString() method on common types and uses a type converter on all other objects
        ///     if available
        /// </summary>
        /// <param name="rawValue">The Value or Object to convert to a string</param>
        /// <param name="culture">Culture for numeric and DateTime values</param>
        /// <param name="unsupportedReturn">Return string for unsupported types</param>
        /// <returns>string</returns>
        public static string TypedValueToString(
            object rawValue,
            CultureInfo culture = null,
            string unsupportedReturn = null)
        {
            if (rawValue == null)
            {
                return string.Empty;
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            var type = rawValue.GetType();
            string str;
            if (type == typeof(string))
            {
                str = rawValue as string;
            }
            else if (type == typeof(int) || type == typeof(decimal) || type == typeof(double) ||
                     type == typeof(float) || type == typeof(float))
            {
                str = string.Format(culture.NumberFormat, "{0}", rawValue);
            }
            else if (type == typeof(DateTime))
            {
                str = string.Format(culture.DateTimeFormat, "{0}", rawValue);
            }
            else if (type == typeof(bool) || type == typeof(byte) || type.IsEnum)
            {
                str = rawValue.ToString();
            }
            else if (type == typeof(Guid?))
            {
                if (rawValue != null)
                {
                    return rawValue.ToString();
                }

                str = string.Empty;
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(type);
                str = converter == null || !converter.CanConvertTo(typeof(string)) ||
                      !converter.CanConvertFrom(typeof(string))
                    ? string.IsNullOrEmpty(unsupportedReturn) ? rawValue.ToString() : unsupportedReturn
                    : converter.ConvertToString(null, culture, rawValue);
            }

            return str;
        }

        /// <summary>
        ///     Parses Properties and Fields including Array and Collection references.
        ///     Used internally for the 'Ex' Reflection methods.
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="Property"></param>
        /// <returns></returns>
        private static object GetPropertyInternal(object Parent, string Property)
        {
            if (Property == "this" || Property == "me")
            {
                return Parent;
            }

            var name = Property;
            var str1 = (string)null;
            var flag = false;
            if (Property.IndexOf("[") > -1)
            {
                name = Property.Substring(0, Property.IndexOf("["));
                str1 = Property.Substring(Property.IndexOf("["));
                flag = true;
            }

            var memberInfo = Parent.GetType().GetMember(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic)[0];
            var instance = memberInfo.MemberType != MemberTypes.Property
                ? ((FieldInfo)memberInfo).GetValue(Parent)
                : ((PropertyInfo)memberInfo).GetValue(Parent, null);
            if (flag)
            {
                var s = str1.Replace("[", string.Empty).Replace("]", string.Empty);
                switch (instance)
                {
                    case Array _:
                        var result1 = -1;
                        int.TryParse(s, out result1);
                        instance = CallMethod(instance, "GetValue", (object)result1);
                        break;
                    case ICollection _:
                        if (s.StartsWith("\""))
                        {
                            var str2 = s.Trim('"');
                            instance = CallMethod(instance, "get_Item", (object)str2);
                            break;
                        }

                        var result2 = -1;
                        int.TryParse(s, out result2);
                        instance = CallMethod(instance, "get_Item", (object)result2);
                        break;
                }
            }

            return instance;
        }

        /// <summary>
        ///     Parses Properties and Fields including Array and Collection references.
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="Property"></param>
        /// <returns></returns>
        private static object SetPropertyInternal(object Parent, string Property, object Value)
        {
            if (Property == "this" || Property == "me")
            {
                return Parent;
            }

            var instance = (object)null;
            var name = Property;
            var str1 = (string)null;
            var flag = false;
            if (Property.IndexOf("[") > -1)
            {
                name = Property.Substring(0, Property.IndexOf("["));
                str1 = Property.Substring(Property.IndexOf("["));
                flag = true;
            }

            if (!flag)
            {
                var memberInfo = Parent.GetType().GetMember(name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic)[0];
                if (memberInfo.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = (PropertyInfo)memberInfo;
                    if (propertyInfo.CanWrite)
                    {
                        propertyInfo.SetValue(Parent, Value, null);
                    }
                }
                else
                {
                    ((FieldInfo)memberInfo).SetValue(Parent, Value);
                }

                return null;
            }

            var memberInfo1 = Parent.GetType().GetMember(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic)[0];
            if (memberInfo1.MemberType == MemberTypes.Property)
            {
                var propertyInfo = (PropertyInfo)memberInfo1;
                if (propertyInfo.CanRead)
                {
                    instance = propertyInfo.GetValue(Parent, null);
                }
            }
            else
            {
                instance = ((FieldInfo)memberInfo1).GetValue(Parent);
            }

            if (flag)
            {
                var s = str1.Replace("[", string.Empty).Replace("]", string.Empty);
                switch (instance)
                {
                    case Array _:
                        var result1 = -1;
                        int.TryParse(s, out result1);
                        instance = ReflectionUtils.CallMethod(instance, "SetValue", Value,
                            (object)result1);
                        break;
                    case ICollection _:
                        if (s.StartsWith("\""))
                        {
                            var str2 = s.Trim('"');
                            instance = ReflectionUtils.CallMethod(instance, "set_Item",
                                (object)str2, Value);
                            break;
                        }

                        var result2 = -1;
                        int.TryParse(s, out result2);
                        instance = ReflectionUtils.CallMethod(instance, "set_Item",
                            (object)result2, Value);
                        break;
                }
            }

            return instance;
        }
    }
}