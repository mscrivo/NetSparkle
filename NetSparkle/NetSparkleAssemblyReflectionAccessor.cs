﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NetSparkle.Interfaces;

namespace NetSparkle
{
    /// <summary>
    ///     Assembly reflection accessor
    /// </summary>
    public class NetSparkleAssemblyReflectionAccessor : INetSparkleAssemblyAccessor
    {
        private readonly Assembly _assembly;
        private readonly List<Attribute> _assemblyAttributes = new List<Attribute>();

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="assemblyName">the assembly name</param>
        public NetSparkleAssemblyReflectionAccessor(string assemblyName)
        {
            if (assemblyName == null)
                _assembly = Assembly.GetEntryAssembly();
            else
            {
                var absolutePath = Path.GetFullPath(assemblyName);
                if (!File.Exists(absolutePath))
                    throw new FileNotFoundException();

                _assembly = Assembly.ReflectionOnlyLoadFrom(absolutePath);

                if (_assembly == null)
                    throw new ArgumentNullException("Unable to load assembly " + absolutePath);
            }

            // read the attributes            
            foreach (var data in _assembly.GetCustomAttributesData())
                _assemblyAttributes.Add(CreateAttribute(data));

            if (_assemblyAttributes == null || _assemblyAttributes.Count == 0)
                throw new ArgumentOutOfRangeException("Unable to load assembly attributes from " + _assembly.FullName);
        }

        /// <summary>
        ///     This methods creates an attribute instance from the attribute data
        ///     information
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Attribute CreateAttribute(CustomAttributeData data)
        {
            var arguments = from arg in data.ConstructorArguments
                select arg.Value;

            var attribute = data.Constructor.Invoke(arguments.ToArray())
                as Attribute;

            if (data.NamedArguments != null)
                foreach (var namedArgument in data.NamedArguments)
                {
                    var propertyInfo = namedArgument.MemberInfo as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(attribute, namedArgument.TypedValue.Value, null);
                    }
                    else
                    {
                        var fieldInfo = namedArgument.MemberInfo as FieldInfo;
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(attribute, namedArgument.TypedValue.Value);
                        }
                    }
                }

            return attribute;
        }

        private Attribute FindAttribute(Type attributeType)
        {
            foreach (var attr in _assemblyAttributes)
            {
                if (attr.GetType() == attributeType)
                    return attr;
            }

            throw new Exception("Attribute of type " + attributeType + " does not exists in the assembly " + _assembly.FullName);
        }

        #region Assembly Attribute Accessors

        /// <summary>
        ///     Gets the assembly title
        /// </summary>
        public string AssemblyTitle
        {
            get
            {
                var a = FindAttribute(typeof (AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                return a?.Title;
            }
        }

        /// <summary>
        ///     Gets the version
        /// </summary>
        public string AssemblyVersion => _assembly.GetName().Version.ToString();

        /// <summary>
        ///     Gets the description
        /// </summary>
        public string AssemblyDescription
        {
            get
            {
                var a = FindAttribute(typeof (AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                return a?.Description;
            }
        }

        /// <summary>
        ///     Gets the product
        /// </summary>
        public string AssemblyProduct
        {
            get
            {
                var a = FindAttribute(typeof (AssemblyProductAttribute)) as AssemblyProductAttribute;
                return a?.Product;
            }
        }

        /// <summary>
        ///     Gets the copyright
        /// </summary>
        public string AssemblyCopyright
        {
            get
            {
                var a = FindAttribute(typeof (AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
                return a?.Copyright;
            }
        }

        /// <summary>
        ///     Gets the company
        /// </summary>
        public string AssemblyCompany
        {
            get
            {
                var a = FindAttribute(typeof (AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                return a?.Company;
            }
        }

        #endregion
    }
}