/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// La información general sobre un ensamblado se controla mediante el siguiente 
// conjunto de atributos. Cambie estos atributos para modificar la información
// asociada con un ensamblado.
[assembly: AssemblyTitle("Alphora Dataphor - DAE PostgreSQL Connection")]
[assembly: AssemblyDescription("Implementation for connection to SQL-based DBMSs through the ADO.NET Oracle Provider")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alphora")]
[assembly: AssemblyProduct("Dataphor")]
[assembly: AssemblyCopyright("© Copyright 2009-2015 Alphora")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: Alphora.Dataphor.DAE.Server.DAERegister("Alphora.Dataphor.DAE.Connection.PGSQL.DAERegister")]

// Si establece ComVisible como false, los tipos de este ensamblado no estarán visibles 
// para los componentes COM. Si necesita obtener acceso a un tipo de este ensamblado desde 
// COM, establezca el atributo ComVisible como true en este tipo.
[assembly: ComVisible(false)]

// El siguiente GUID sirve como identificador de typelib si este proyecto se expone a COM
[assembly: Guid("a6d37fb4-a630-42b3-919e-c68bd84a5b6e")]

// La información de versión de un ensamblado consta de los cuatro valores siguientes:
//
//      Versión principal
//      Versión secundaria 
//      Número de versión de compilación
//      Revisión
//
// Puede especificar todos los valores o establecer como predeterminados los números de versión de compilación y de revisión 
// mediante el asterisco ('*'), como se muestra a continuación:

[assembly: AssemblyVersion("3.1.*")]

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
#if SIGNASSEMBLIES
[assembly: AssemblyKeyFile("..\\..\\..\\..\\..\\Dataphor.snk")]
#else
[assembly: AssemblyKeyFile("")]
#endif
[assembly: AssemblyKeyName("")]
