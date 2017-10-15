using System;
using System.IO;
using Microsoft.Scripting.Hosting;
using System.Collections.Generic;


namespace PyRevitBaseClasses
{
    public class EngineManager
    {
        private Stream _defaultOutput;

        public EngineManager() {}

        public ScriptEngine GetEngine(ref PyRevitCommandRuntime pyrvtCmd)
        {
            ScriptEngine engine;
            bool cachedEngine = false;

            // If the command required a fullframe engine
            if (pyrvtCmd.NeedsFullFrameEngine)
                engine = CreateNewEngine(ref pyrvtCmd, fullframe: true);

            // If the command required a clean engine
            else if (pyrvtCmd.NeedsCleanEngine)
                engine = CreateNewEngine(ref pyrvtCmd);

            // if the user is asking to refresh the cached engine for the command,
            // then update the engine and save in cache
            else if (pyrvtCmd.NeedsRefreshedEngine)
                engine = RefreshCachedEngine(ref pyrvtCmd);

            // if not above, get/create cached engine
            else {
                engine = GetCachedEngine(ref pyrvtCmd);
                cachedEngine = true;
            }

            // now that the engine is ready, setup the builtins and io streams
            SetupStreams(engine, pyrvtCmd.OutputStream);
            SetupBuiltins(engine, ref pyrvtCmd, cachedEngine);
            SetupSearchPaths(engine, pyrvtCmd.ModuleSearchPaths);

            return engine;
        }

        public Dictionary<string, ScriptEngine> EngineDict
        {
            get
            {
                var engineDict = (Dictionary<string, ScriptEngine>) AppDomain.CurrentDomain.GetData(EnvDictionaryKeys.docEngineDict);

                if (engineDict == null)
                    engineDict = ClearEngines();

                return engineDict;
            }
        }

        public Dictionary<string, ScriptEngine> ClearEngines()
        {
            var engineDict = new Dictionary<string, ScriptEngine>();
            AppDomain.CurrentDomain.SetData(EnvDictionaryKeys.docEngineDict, engineDict);

            return engineDict;
        }

        public void CleanupEngine(ScriptEngine engine)
        {
            CleanupEngineBuiltins(engine);
            CleanupStreams(engine);
        }

        private ScriptEngine CreateNewEngine(ref PyRevitCommandRuntime pyrvtCmd, bool fullframe=false)
        {
            var flags = new Dictionary<string, object>(){{ "LightweightScopes", true }};

            if (fullframe)
            {
                // Disabling all frames to avoid the memory leak issue
                // that would increase the % of time spent in GC dramatically
                // Tried these options together and made the runtime much slower
                //  { "GCStress", 0 },
                //  { "MaxRecursion", 0 },
                flags["Frames"] = true;
                flags["FullFrames"] = true;
            }
                
            var engine = IronPython.Hosting.Python.CreateEngine(flags);

            // reference RevitAPI and RevitAPIUI
            engine.Runtime.LoadAssembly(typeof(Autodesk.Revit.DB.Document).Assembly);
            engine.Runtime.LoadAssembly(typeof(Autodesk.Revit.UI.TaskDialog).Assembly);

            // also, allow access to the RPL internals
            engine.Runtime.LoadAssembly(typeof(PyRevitBaseClasses.ScriptExecutor).Assembly);

            return engine;
        }

        private ScriptEngine CreateNewCachedEngine(ref PyRevitCommandRuntime pyrvtCmd)
        {
            var newEngine = CreateNewEngine(ref pyrvtCmd);
            this.EngineDict[pyrvtCmd.CommandExtension] = newEngine;
            return newEngine;
        }

        private ScriptEngine GetCachedEngine(ref PyRevitCommandRuntime pyrvtCmd)
        {
            if (this.EngineDict.ContainsKey(pyrvtCmd.CommandExtension))
            {
                var existingEngine = this.EngineDict[pyrvtCmd.CommandExtension];
                return existingEngine;
            }
            else
            {
                return CreateNewCachedEngine(ref pyrvtCmd);
            }
        }

        private ScriptEngine RefreshCachedEngine(ref PyRevitCommandRuntime pyrvtCmd)
        {
            return CreateNewCachedEngine(ref pyrvtCmd);
        }

        private void SetupSearchPaths(ScriptEngine engine, string[] searchPaths)
        {
            // Process search paths provided to executor
            // syspaths variable is a string of paths separated by ';'. Split syspath and update the search paths
            engine.SetSearchPaths(searchPaths);
        }

        private void SetupBuiltins(ScriptEngine engine, ref PyRevitCommandRuntime pyrvtCmd, bool cachedEngine)
        {
            // BUILTINS -----------------------------------------------------------------------------------------------
            // Get builtin to add custom variables
            var builtin = IronPython.Hosting.Python.GetBuiltinModule(engine);

            // Let commands know if they're being run in a cached engine
            builtin.SetVariable("__cachedengine__", cachedEngine);

            // Add current engine manager to builtins
            builtin.SetVariable("__ipyenginemanager__", this);

            // Add this script executor to the the builtin to be globally visible everywhere
            // This support pyrevit functionality to ask information about the current executing command
            builtin.SetVariable("__externalcommand__", pyrvtCmd);

            // Add host application handle to the builtin to be globally visible everywhere
            builtin.SetVariable("__revit__", pyrvtCmd.RevitUIApp);


            // Add handles to current document and ui document
            if (pyrvtCmd.RevitUIDoc != null) {
                builtin.SetVariable("__activeuidoc__",  pyrvtCmd.RevitUIDoc);
                builtin.SetVariable("__activedoc__",    pyrvtCmd.RevitDoc);
                builtin.SetVariable("__zerodoc__",      false);
            }
            else {
                builtin.SetVariable("__activeuidoc__",  (Object)null);
                builtin.SetVariable("__activedoc__",    (Object)null);
                builtin.SetVariable("__zerodoc__",      true);
            }

            // Adding data provided by IExternalCommand.Execute
            builtin.SetVariable("__commanddata__",          pyrvtCmd.CommandData);
            builtin.SetVariable("__elements__",             pyrvtCmd.SelectedElements);

            // Adding information on the command being executed
            builtin.SetVariable("__commandpath__",          Path.GetDirectoryName(pyrvtCmd.OriginalScriptSourceFile));
            builtin.SetVariable("__alternatecommandpath__", Path.GetDirectoryName(pyrvtCmd.AlternateScriptSourceFile));
            builtin.SetVariable("__commandname__",          pyrvtCmd.CommandName);
            builtin.SetVariable("__commandbundle__",        pyrvtCmd.CommandBundle);
            builtin.SetVariable("__commandextension__",     pyrvtCmd.CommandExtension);
            builtin.SetVariable("__commanduniqueid__",      pyrvtCmd.CommandUniqueId);
            builtin.SetVariable("__forceddebugmode__",      pyrvtCmd.DebugMode);
            builtin.SetVariable("__shiftclick__",           pyrvtCmd.AlternateMode);

            // Add reference to the results dictionary
            // so the command can add custom values for logging
            builtin.SetVariable("__result__",               pyrvtCmd.GetResultsDictionary());
        }

        private void SetupStreams(ScriptEngine engine, ScriptOutputStream outStream)
        {
            // Setup IO streams
            _defaultOutput = engine.Runtime.IO.OutputStream;
            engine.Runtime.IO.SetOutput(outStream, System.Text.Encoding.UTF8);
        }

        private void CleanupEngineBuiltins(ScriptEngine engine)
        {
            var builtin = IronPython.Hosting.Python.GetBuiltinModule(engine);

            builtin.SetVariable("__cachedengine__",         (Object)null);
            builtin.SetVariable("__ipyenginemanager__",     (Object)null);
            builtin.SetVariable("__externalcommand__",      (Object)null);
            builtin.SetVariable("__revit__",                (Object)null);
            builtin.SetVariable("__activeuidoc__",          (Object)null);
            builtin.SetVariable("__activedoc__",            (Object)null);
            builtin.SetVariable("__zerodoc__",              (Object)null);
            builtin.SetVariable("__commanddata__",          (Object)null);
            builtin.SetVariable("__elements__",             (Object)null);
            builtin.SetVariable("__commandpath__",          (Object)null);
            builtin.SetVariable("__alternatecommandpath__", (Object)null);
            builtin.SetVariable("__commandname__",          (Object)null);
            builtin.SetVariable("__commandbundle__",        (Object)null);
            builtin.SetVariable("__commandextension__",     (Object)null);
            builtin.SetVariable("__commanduniqueid__",      (Object)null);
            builtin.SetVariable("__forceddebugmode__",      (Object)null);
            builtin.SetVariable("__shiftclick__",           (Object)null);
            builtin.SetVariable("__result__",               (Object)null);


        }

        private void CleanupStreams(ScriptEngine engine)
        {
            // Remove IO streams references so GC can collect
            engine.Runtime.IO.SetOutput(_defaultOutput, System.Text.Encoding.UTF8);
        }

    }
}
