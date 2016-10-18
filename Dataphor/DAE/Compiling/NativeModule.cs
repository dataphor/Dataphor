using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Sigil;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

//#define IL_USE_METHOD_BUILDER

namespace Alphora.Dataphor.DAE
{
	public class NativeEmitter
	{
		//TODO: Enable emission debugging

        #if IL_USE_METHOD_BUILDER
		private AssemblyName _assemblyName;
		private AssemblyBuilder _assembly;
		private ModuleBuilder _module;
        #else
        private TypeBuilder _type;
        #endif
		//private ISymbolDocumentWriter _symbolWriter;

        public NativeEmitter(string name)
        {
            #if IL_USE_METHOD_BUILDER
            _assemblyName = new AssemblyName(name);
            _assembly =
                AppDomain.CurrentDomain.DefineDynamicAssembly
                (
                    _assemblyName,
                    //#if (DEBUG)
                    //AssemblyBuilderAccess.RunAndSave
                    //#else
                    AssemblyBuilderAccess.RunAndCollect
                    //#endif
                );
            _module = _assembly.DefineDynamicModule(_assemblyName.Name, _assemblyName.Name + ".dll", false/* _options.DebugOn */);
            _type = _module.DefineType("Program");
            #endif
            //_assembly.SetCustomAttribute
            //(
            //    new CustomAttributeBuilder
            //    (
            //        typeof(DebuggableAttribute).GetConstructor
            //        (
            //            new System.Type[] { typeof(DebuggableAttribute.DebuggingModes) }
            //        ),
            //        new object[]
            //        {
            //            DebuggableAttribute.DebuggingModes.DisableOptimizations |
            //            DebuggableAttribute.DebuggingModes.Default
            //        }
            //    )
            //);
        }

        public void SaveAssembly()
        {
            try
            {
                #if IL_USE_METHOD_BUILDER
                _module.CreateGlobalFunctions();
                #if (DEBUG)
                _assembly.Save(_assemblyName + ".dll");
                #endif
                #endif

                //var pdbGenerator = _debugOn ? System.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator() : null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                // Don't rethrow - for debugging
            }
        }

        public NativeMethod CreateMethod(string name = "Main")
        {
            return new NativeMethod(_type, name);
        }
	}
}
