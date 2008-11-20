using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark.Compiler;
using Spark.Compiler.CSharp.ChunkVisitors;
using GeneratedCodeVisitor=Spark.Scripting.Compiler.Python.ChunkVisitors.GeneratedCodeVisitor;

namespace Spark.Scripting.Compiler.Python
{
    public class PythonViewCompiler : ViewCompiler
    {
        public override void CompileView(IEnumerable<IList<Chunk>> viewTemplates, IEnumerable<IList<Chunk>> allResources)
        {
            GenerateSourceCode(viewTemplates, allResources);

            var compiler = new BatchCompiler();
            var assembly = compiler.Compile(Debug, SourceCode);
            CompiledType = assembly.GetType(ViewClassFullName);
        }

        public override void GenerateSourceCode(IEnumerable<IList<Chunk>> viewTemplates, IEnumerable<IList<Chunk>> allResources)
        {
            var script = new SourceWriter();
            var globals = new Dictionary<string, object>();

            var globalMembersVisitor = new ChunkVisitors.GlobalMembersVisitor(script, globals);
            foreach(var resource in allResources)
                globalMembersVisitor.Accept(resource);

            var globalFunctionsVisitor = new ChunkVisitors.GlobalFunctionsVisitor(script, globals);
            foreach (var resource in allResources)
                globalFunctionsVisitor.Accept(resource);


            var templateIndex = 0;
            foreach (var template in viewTemplates)
            {
                script.Write("def RenderViewLevel").Write(templateIndex).WriteLine("():");
                script.Indent++;
                foreach (var global in globals.Keys)
                    script.Write("global ").WriteLine(global);
                var generator = new GeneratedCodeVisitor(script, globals);
                generator.Accept(template);
                script.Indent--;
                script.WriteLine();
                templateIndex++;
            }
            
            for (var renderIndex = 0; renderIndex != templateIndex; ++renderIndex)
            {
                if (renderIndex < templateIndex - 1)
                {
                    script.WriteLine("scope=OutputScopeAdapter(None)");
                    script.Write("RenderViewLevel").Write(renderIndex).WriteLine("()");
                    script.WriteLine("Content[\"view\"] = Output");
                    script.WriteLine("scope.Dispose()");
                }
                else
                {
                    script.Write("RenderViewLevel").Write(renderIndex).WriteLine("()");
                }
            }

            var baseClassGenerator = new BaseClassVisitor { BaseClass = BaseClass };
            foreach (var resource in allResources)
                baseClassGenerator.Accept(resource);

            BaseClass = baseClassGenerator.BaseClassTypeName;

            var source = new StringBuilder();

            var viewClassName = "View" + GeneratedViewId.ToString("n");
            if (Descriptor != null && !string.IsNullOrEmpty(Descriptor.TargetNamespace))
            {
                ViewClassFullName = Descriptor.TargetNamespace + "." + viewClassName;
                source.Append("namespace ").AppendLine(Descriptor.TargetNamespace);
                source.AppendLine("{");
            }
            else
            {
                ViewClassFullName = viewClassName;
            }

            if (Descriptor != null)
            {
                // [SparkView] attribute
                source.AppendLine("[global::Spark.SparkViewAttribute(");
                if (TargetNamespace != null)
                    source.AppendFormat("    TargetNamespace=\"{0}\",", TargetNamespace).AppendLine();
                source.AppendLine("    Templates = new string[] {");
                source.Append("      ").AppendLine(string.Join(",\r\n      ",
                                                               Descriptor.Templates.Select(
                                                                   t => "\"" + t.Replace("\\", "\\\\") + "\"").ToArray()));
                source.AppendLine("    })]");
            }

            source.Append("public class ").Append(viewClassName).Append(" : ").Append(BaseClass).AppendLine(", global::Spark.Scripting.IScriptingSparkView");
            source.AppendLine("{");
            
            source.Append("static System.Guid _generatedViewId = new System.Guid(\"").Append(GeneratedViewId).AppendLine("\");");
            source.AppendLine("public override System.Guid GeneratedViewId");
            source.AppendLine("{");
            source.AppendLine("get { return _generatedViewId; }");
            source.AppendLine("}");

            source.AppendLine("public global::System.IDisposable OutputScopeAdapter(object arg) ");
            source.AppendLine("{");
            source.AppendLine("if (arg == null) return OutputScope();");
            source.AppendLine("if (arg is string) return OutputScope((string)arg);");
            source.AppendLine("if (arg is global::System.IO.TextWriter) return OutputScope((global::System.IO.TextWriter)arg);");
            source.AppendLine("throw new global::Spark.Compiler.CompilerException(\"Invalid argument for OutputScopeAdapter\");");
            source.AppendLine("}");

            source.AppendLine("public void OutputWriteAdapter(object arg) ");
            source.AppendLine("{");
            source.AppendLine("Output.Write(arg);");
            source.AppendLine("}");

            source.AppendLine("public global::Microsoft.Scripting.Hosting.CompiledCode CompiledCode {get;set;}");
            
            source.AppendLine("public string ScriptSource");
            source.AppendLine("{");
            source.Append("get { return @\"").Append(script.ToString().Replace("\"", "\"\"")).AppendLine("\"; }");
            source.AppendLine("}");
            
            source.AppendLine("public override void RenderView(System.IO.TextWriter writer)");
            source.AppendLine("{");
            source.AppendLine("using(OutputScope(writer))");
            source.AppendLine("{");
            source.AppendLine("CompiledCode.Execute(");
            source.AppendLine("CompiledCode.Engine.CreateScope(");
            source.AppendLine("new global::Spark.Scripting.ScriptingViewSymbolDictionary(this)");
            source.AppendLine("));");
            source.AppendLine("}");
            source.AppendLine("}");
            
            source.AppendLine("}");

            if (Descriptor != null && !string.IsNullOrEmpty(Descriptor.TargetNamespace))
            {
                source.AppendLine("}");
            }

            SourceCode = source.ToString();
        }
    }


}