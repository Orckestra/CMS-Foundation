﻿using System;

namespace Composite.Types
{
	public static class CodeGenerationHelper
	{
        public static string GetTypeAlias(Type type)
        {
            return GetTypeAlias(type.FullName);
        }

        public static string GetTypeAlias(string typeFullName)
        {
            return typeFullName.Replace(".", "_");
        }
	}
}
