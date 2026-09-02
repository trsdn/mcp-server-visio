using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Vba;

public class VbaCommands : IVbaCommands
{
    public VbaModuleListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = (dynamic)ctx.Document;
            dynamic vbProject = pres.VBProject;
            dynamic components = vbProject.VBComponents;
            try
            {
                int count = (int)components.Count;
                var result = new VbaModuleListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath
                };

                for (int i = 1; i <= count; i++)
                {
                    dynamic comp = components.Item(i);
                    try
                    {
                        int modType = (int)comp.Type;
                        int lineCount = 0;
                        try { lineCount = (int)comp.CodeModule.CountOfLines; } catch { }

                        result.Modules.Add(new VbaModuleInfo
                        {
                            Name = comp.Name?.ToString() ?? "",
                            ModuleType = modType,
                            ModuleTypeName = GetModuleTypeName(modType),
                            LineCount = lineCount
                        });
                    }
                    finally
                    {
                        ComUtilities.Release(ref comp!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref components!);
                ComUtilities.Release(ref vbProject!);
            }
        });
    }

    public VbaModuleCodeResult View(IVisioBatch batch, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = (dynamic)ctx.Document;
            dynamic vbProject = pres.VBProject;
            dynamic components = vbProject.VBComponents;
            dynamic comp = components.Item(moduleName);
            dynamic? codeModule = null;
            try
            {
                codeModule = comp.CodeModule;
                int lineCount = (int)codeModule.CountOfLines;
                string code = lineCount > 0
                    ? codeModule.Lines(1, lineCount)?.ToString() ?? ""
                    : "";

                return new VbaModuleCodeResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    ModuleName = moduleName,
                    Code = code,
                    LineCount = lineCount
                };
            }
            finally
            {
                if (codeModule != null) ComUtilities.Release(ref codeModule!);
                ComUtilities.Release(ref comp!);
                ComUtilities.Release(ref components!);
                ComUtilities.Release(ref vbProject!);
            }
        });
    }

    public OperationResult Import(IVisioBatch batch, string moduleName, string code, int moduleType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = (dynamic)ctx.Document;
            dynamic vbProject = pres.VBProject;
            dynamic components = vbProject.VBComponents;
            dynamic? comp = null;
            dynamic? codeModule = null;
            try
            {
                // vbext_ct_StdModule = 1, vbext_ct_ClassModule = 2
                int vbType = moduleType == 2 ? 2 : 1;
                comp = components.Add(vbType);
                comp.Name = moduleName;
                codeModule = comp.CodeModule;
                codeModule.AddFromString(code);

                return new OperationResult
                {
                    Success = true,
                    Action = "import",
                    Message = $"Imported VBA module '{moduleName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (codeModule != null) ComUtilities.Release(ref codeModule!);
                if (comp != null) ComUtilities.Release(ref comp!);
                ComUtilities.Release(ref components!);
                ComUtilities.Release(ref vbProject!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = (dynamic)ctx.Document;
            dynamic vbProject = pres.VBProject;
            dynamic components = vbProject.VBComponents;
            dynamic comp = components.Item(moduleName);
            try
            {
                components.Remove(comp);
                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted VBA module '{moduleName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref comp!);
                ComUtilities.Release(ref components!);
                ComUtilities.Release(ref vbProject!);
            }
        });
    }

    public OperationResult Run(IVisioBatch batch, string macroName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(macroName);

        return batch.Execute((ctx, ct) =>
        {
            // PowerPoint.Application.Run(macroName)
            dynamic app = ((dynamic)ctx.Document).Application;
            try
            {
                app.Run(macroName);
                return new OperationResult
                {
                    Success = true,
                    Action = "run",
                    Message = $"Executed macro '{macroName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref app!);
            }
        });
    }

    private static string GetModuleTypeName(int moduleType) => moduleType switch
    {
        1 => "Standard",
        2 => "ClassModule",
        3 => "MSForm",
        100 => "Document",
        _ => $"Unknown({moduleType})"
    };
}
