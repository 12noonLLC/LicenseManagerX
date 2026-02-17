using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(
	 Workers = 0,                      // 0 = use environment default
	 Scope = ExecutionScope.ClassLevel // or MethodLevel if you prefer
)]

//[assembly: AssemblyTitle("LicenseManagerX.UnitTests")]
[assembly: AssemblyDescription("")]
//[assembly: AssemblyConfiguration("")]
//[assembly: AssemblyProduct("LicenseManagerX.UnitTests")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("2289cb07-935f-4078-a5f8-0f78b26ecd8a")]

// [assembly: AssemblyVersion("1.0.*")]
//[assembly: AssemblyVersion("1.0.0.0")]
//[assembly: AssemblyFileVersion("1.0.0.0")]
