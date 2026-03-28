using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Enforces MASTER_RULES code-level constraints at build / CI time.
/// Scans <c>Modules\**\*.cs</c> for architectural violations.
/// <para>
/// <b>Baseline thresholds:</b> pre-existing violations are capped at
/// a known count. Fixing a violation lowers the threshold. Adding a
/// new violation raises the count above the threshold and <b>fails</b>
/// the test.
/// </para>
/// </summary>
public partial class ArchitectureComplianceTests
{
    // ── Baselines (lower these as you fix violations) ─────────────

    /// <summary>Pre-existing cross-module references. Lower as you decouple modules.</summary>
    private const int CrossModuleBaseline = 35;

    /// <summary>Pre-existing async methods missing CancellationToken.</summary>
    private const int MissingCtBaseline = 0;

    private readonly ITestOutputHelper _output;

    public ArchitectureComplianceTests(ITestOutputHelper output) => _output = output;

    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0
                || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }

    private static IEnumerable<string> GetModuleCsFiles() =>
        Directory.EnumerateFiles(
                Path.Combine(SolutionRoot, "Modules"), "*.cs",
                SearchOption.AllDirectories)
            .OrderBy(f => f);

    // ═══════════════════════════════════════════════════════════════════
    //  1. NO DateTime.Now IN MODULE CODE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §6: "Timestamps: IRegionalSettingsService.Now (IST) —
    /// never DateTime.Now." Only <c>DateTime.UtcNow</c> is allowed for
    /// infrastructure concerns (lockout expiry, logging).
    /// </summary>
    [Fact]
    public void Modules_ShouldNot_UseDateTimeNow()
    {
        var pattern = DateTimeNowRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): DateTime.Now — use IRegionalSettingsService.Now\n    {trimmed}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} DateTime.Now usage(s) in module code:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  2. NO MessageBox IN VIEWMODELS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §7: "No MessageBox for validation — inline feedback only."
    /// ViewModels must set <c>ErrorMessage</c> instead.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_UseMessageBox()
    {
        var pattern = MessageBoxRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): MessageBox — use ErrorMessage/SuccessMessage instead\n    {trimmed}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} MessageBox usage(s) in ViewModel code:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  3. ASYNC METHODS MUST ACCEPT CANCELLATION TOKEN
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §5: "Every async service method MUST accept
    /// CancellationToken ct = default as its last parameter."
    /// Scans service interfaces for async methods missing ct.
    /// </summary>
    [Fact]
    public void ServiceInterfaces_AsyncMethods_MustAcceptCancellationToken()
    {
        var asyncMethodPattern = AsyncMethodSignatureRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).StartsWith('I') ||
                !Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            // Read lines to handle multi-line signatures
            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (!asyncMethodPattern.IsMatch(lines[i]))
                    continue;

                // Collect full signature (may span multiple lines until ';' or '{')
                var signatureLines = new List<string> { lines[i] };
                for (var j = i + 1; j < lines.Length; j++)
                {
                    signatureLines.Add(lines[j]);
                    if (lines[j].Contains(';') || lines[j].Contains('{'))
                        break;
                }

                var fullSignature = string.Join(" ", signatureLines);
                if (!fullSignature.Contains("CancellationToken", StringComparison.Ordinal))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Async method missing CancellationToken\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count <= MissingCtBaseline,
            $"Missing CancellationToken count ({violations.Count}) exceeds baseline ({MissingCtBaseline}). "
            + "New async service methods must accept CancellationToken ct = default. "
            + $"Fix violations or lower MissingCtBaseline when cleaning up.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  4. NO CROSS-MODULE REFERENCES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §1.2: "Modules never reference another module directly."
    /// Scans for <c>using StoreAssistantPro.Modules.X</c> inside module Y.
    /// </summary>
    [Fact]
    public void Modules_ShouldNot_ReferenceSiblingModules()
    {
        var violations = new List<string>();
        var modulesDir = Path.Combine(SolutionRoot, "Modules");

        foreach (var file in GetModuleCsFiles())
        {
            // Determine which module this file belongs to
            var relPath = Path.GetRelativePath(modulesDir, file);
            var ownerModule = relPath.Split(Path.DirectorySeparatorChar)[0];

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (!trimmed.StartsWith("using StoreAssistantPro.Modules.", StringComparison.Ordinal))
                    continue;

                // Extract the referenced module name
                var afterPrefix = trimmed["using StoreAssistantPro.Modules.".Length..];
                var refModule = afterPrefix.Split('.', ';')[0];

                if (!string.Equals(refModule, ownerModule, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Cross-module reference ({ownerModule} → {refModule})\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count <= CrossModuleBaseline,
            $"Cross-module reference count ({violations.Count}) exceeds baseline ({CrossModuleBaseline}). "
            + "Modules must only depend on Core/, Models/, Data/. Use IEventBus for cross-module communication. "
            + $"Fix violations or lower CrossModuleBaseline when cleaning up.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  5. VALIDATION ATTRIBUTES NEED [NotifyDataErrorInfo]
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Every property with a validation attribute
    /// ([MaxLength], [Required], [RegularExpression]) MUST also have
    /// [NotifyDataErrorInfo] — otherwise the attribute is silently
    /// ignored by CommunityToolkit.Mvvm."
    /// </summary>
    [Fact]
    public void ViewModels_ValidationAttributes_MustHaveNotifyDataErrorInfo()
    {
        var validationAttrPattern = ValidationAttributeRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (!validationAttrPattern.IsMatch(trimmed))
                    continue;

                // Look backwards up to 8 lines for [NotifyDataErrorInfo]
                var hasNotify = false;
                for (var j = Math.Max(0, i - 8); j <= i; j++)
                {
                    if (lines[j].Contains("[NotifyDataErrorInfo]", StringComparison.Ordinal))
                    {
                        hasNotify = true;
                        break;
                    }
                }

                if (!hasNotify)
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Validation attribute without [NotifyDataErrorInfo]\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} validation attribute(s) missing [NotifyDataErrorInfo]:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  6. NO ValidateAllProperties() IN NON-SAVE METHODS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Wizard/step dialogs MUST NOT call
    /// ValidateAllProperties() on Next — it causes cross-step
    /// validation bleed. ValidateAllProperties() is only correct
    /// in Save."
    /// <para>
    /// Scans for <c>ValidateAllProperties()</c> calls that are NOT
    /// inside a method named <c>Save*</c> or <c>*SaveAsync</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void ViewModels_ValidateAllProperties_OnlyInSave()
    {
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            string? currentMethod = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Track current method name
                var methodMatch = MethodSignatureRegex().Match(trimmed);
                if (methodMatch.Success)
                    currentMethod = methodMatch.Groups[1].Value;

                if (!trimmed.Contains("ValidateAllProperties()", StringComparison.Ordinal))
                    continue;

                // Skip if inside a Save method
                if (currentMethod is not null &&
                    currentMethod.Contains("Save", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relative = Path.GetRelativePath(SolutionRoot, file);
                var ctx = currentMethod is not null ? $" (in {currentMethod})" : "";
                violations.Add(
                    $"  {relative}({i + 1}): ValidateAllProperties() outside Save{ctx}\n    {trimmed}");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} ValidateAllProperties() call(s) outside Save methods. "
            + "Use per-step validation (ClearErrors + ValidateProperty) on Next.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Regex patterns
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Matches <c>DateTime.Now</c> but NOT <c>DateTime.UtcNow</c>.</summary>
    [GeneratedRegex(@"DateTime\.Now(?![\w])", RegexOptions.Compiled)]
    private static partial Regex DateTimeNowRegex();

    /// <summary>Matches <c>MessageBox.Show</c>.</summary>
    [GeneratedRegex(@"MessageBox\.Show", RegexOptions.Compiled)]
    private static partial Regex MessageBoxRegex();

    /// <summary>Matches async method signatures in interfaces: <c>Task&lt;…&gt; MethodAsync(</c>.</summary>
    [GeneratedRegex(@"Task[<\s].*Async\s*\(", RegexOptions.Compiled)]
    private static partial Regex AsyncMethodSignatureRegex();

    /// <summary>
    /// Matches validation attribute lines like <c>[Required]</c>,
    /// <c>[MaxLength(50)]</c>, <c>[RegularExpression(…)]</c>.
    /// Excludes comments and summary lines.
    /// </summary>
    [GeneratedRegex(
        @"^\[(Required|MaxLength|MinLength|Range|RegularExpression|EmailAddress|Phone|CreditCard|Url|Compare)[\](]",
        RegexOptions.Compiled)]
    private static partial Regex ValidationAttributeRegex();

    /// <summary>Matches method signatures to track the current method name.</summary>
    [GeneratedRegex(
        @"(?:private|public|protected|internal)\s+.*?\s+(\w+)\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex MethodSignatureRegex();

    /// <summary>Matches <c>async void MethodName(</c> patterns.</summary>
    [GeneratedRegex(
        @"\basync\s+void\s+\w+\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex AsyncVoidRegex();

    /// <summary>
    /// Matches <c>AddSingleton&lt;IFoo, ConcreteService&gt;</c>
    /// and captures the concrete type name.
    /// </summary>
    [GeneratedRegex(
        @"AddSingleton<I\w+,\s*(\w+Service)>",
        RegexOptions.Compiled)]
    private static partial Regex SingletonServiceRegex();

    /// <summary>
    /// Matches ViewModel class declarations capturing the class name
    /// and base class. Handles primary constructor syntax.
    /// <c>class FooViewModel(…) : BaseViewModel</c> or
    /// <c>class FooViewModel : BaseViewModel</c>.
    /// </summary>
    [GeneratedRegex(
        @"class\s+(\w+ViewModel)\b[^:]*:\s*(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex ViewModelClassRegex();

    /// <summary>
    /// Matches EF async methods called with empty parentheses (no ct):
    /// <c>ToListAsync()</c>, <c>FirstOrDefaultAsync()</c>,
    /// <c>SaveChangesAsync()</c>, <c>AnyAsync()</c>, etc.
    /// Does NOT match when <c>ct</c> or <c>cancellation</c> is inside.
    /// </summary>
    [GeneratedRegex(
        @"(?:ToListAsync|FirstOrDefaultAsync|SingleOrDefaultAsync|CountAsync|AnyAsync|SaveChangesAsync|ToDictionaryAsync|MaxAsync|MinAsync|SumAsync)\(\s*\)",
        RegexOptions.Compiled)]
    private static partial Regex EfAsyncWithoutCtRegex();

    /// <summary>
    /// Matches Window code-behind class declarations:
    /// <c>class FooWindow : BaseDialogWindow</c> or
    /// <c>class FooWindow : Window</c>.
    /// </summary>
    [GeneratedRegex(
        @"class\s+(\w+Window)\s*:\s*(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex WindowClassRegex();

    /// <summary>
    /// Matches <c>.ToString("C")</c>, <c>.ToString("N2")</c>,
    /// <c>.ToString("N0")</c>, <c>.ToString("P…")</c>, or
    /// date patterns like <c>.ToString("dd/MM/yyyy")</c>.
    /// Does NOT match <c>.ToString()</c> (no format) or
    /// <c>.ToString("G")</c> (generic).
    /// </summary>
    [GeneratedRegex(
        @"\.ToString\(\s*""[CNPcnp]\d?""",
        RegexOptions.Compiled)]
    private static partial Regex HardcodedToStringFormatRegex();

    /// <summary>Matches <c>MaxLength="123"</c> in XAML.</summary>
    [GeneratedRegex(
        @"MaxLength=""(\d+)""",
        RegexOptions.Compiled)]
    private static partial Regex MaxLengthXamlRegex();

    /// <summary>Matches <c>[MaxLength(123…)]</c> in C# attributes.</summary>
    [GeneratedRegex(
        @"^\[MaxLength\((\d+)",
        RegexOptions.Compiled)]
    private static partial Regex MaxLengthAttrRegex();

    /// <summary>Extracts the property name from a binding expression: <c>Binding PropertyName</c>.</summary>
    [GeneratedRegex(
        @"(?:Binding|Binding\s+Path=)\s*""?(\w+)""?",
        RegexOptions.Compiled)]
    private static partial Regex BindingPropertyRegex();

    /// <summary>Matches property declarations to extract the property name.</summary>
    [GeneratedRegex(
        @"partial\s+string\s+(\w+)\s*\{",
        RegexOptions.Compiled)]
    private static partial Regex PropertyNameRegex();

    /// <summary>Matches Model property declarations: <c>public string FirmName { get; set; }</c>.</summary>
    [GeneratedRegex(
        @"public\s+string\??\s+(\w+)\s*\{",
        RegexOptions.Compiled)]
    private static partial Regex ModelPropertyRegex();

    /// <summary>Matches wizard step method signatures: Next(), NextAsync(), Back(), BackAsync().</summary>
    [GeneratedRegex(
        @"(?:void|Task)\s+(?:Next|Back)(?:Async)?\s*\(\s*\)",
        RegexOptions.Compiled)]
    private static partial Regex WizardStepMethodRegex();

    /// <summary>Matches <c>new FooWindow(</c> — direct window instantiation.</summary>
    [GeneratedRegex(
        @"\bnew\s+\w+Window\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex NewWindowRegex();

    /// <summary>
    /// Matches direct <c>AppDbContext</c> constructor injection
    /// in primary constructors or regular constructors, but NOT
    /// inside generic type arguments like <c>IDbContextFactory&lt;AppDbContext&gt;</c>.
    /// </summary>
    [GeneratedRegex(
        @"(?<!Factory<)AppDbContext(?!>)\s+\w+",
        RegexOptions.Compiled)]
    private static partial Regex DirectDbContextInjectionRegex();

    /// <summary>
    /// Matches <c>AddSingleton</c> or <c>AddScoped</c> registrations
    /// of ViewModel, View, Window, or Page types.
    /// </summary>
    [GeneratedRegex(
        @"(?:AddSingleton|AddScoped)<\w*(?:ViewModel|View|Window|Page)>",
        RegexOptions.Compiled)]
    private static partial Regex NonTransientUiRegex();

    // ═══════════════════════════════════════════════════════════════════
    //  7. MODULE REGISTRATION COMPLETENESS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures every module folder under <c>Modules/</c> has a
    /// corresponding <c>Add{Name}Module()</c> call in
    /// <c>HostingExtensions.cs</c>. Prevents silent DI registration
    /// gaps after adding a new module.
    /// </summary>
    [Fact]
    public void EveryModule_MustBe_RegisteredInHostingExtensions()
    {
        var modulesDir = Path.Combine(SolutionRoot, "Modules");
        var hostingFile = Path.Combine(SolutionRoot, "HostingExtensions.cs");

        Assert.True(File.Exists(hostingFile), "HostingExtensions.cs not found");

        var hostingContent = File.ReadAllText(hostingFile);
        var moduleFolders = Directory.GetDirectories(modulesDir)
            .Select(d => Path.GetFileName(d))
            .OrderBy(n => n)
            .ToList();

        var violations = new List<string>();
        foreach (var module in moduleFolders)
        {
            var expectedCall = $"Add{module}Module";
            if (!hostingContent.Contains(expectedCall, StringComparison.Ordinal))
            {
                violations.Add($"  Module '{module}' has no {expectedCall}() call in HostingExtensions.cs");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} unregistered module(s):\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  8. EVERY MODULE FOLDER HAS A MODULE FILE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures every module folder has exactly one <c>*Module.cs</c>
    /// registration file that exposes the <c>Add{Name}Module()</c>
    /// extension method.
    /// </summary>
    [Fact]
    public void EveryModuleFolder_MustHave_ModuleRegistrationFile()
    {
        var modulesDir = Path.Combine(SolutionRoot, "Modules");
        var violations = new List<string>();

        foreach (var dir in Directory.GetDirectories(modulesDir))
        {
            var moduleName = Path.GetFileName(dir);
            var moduleFiles = Directory.GetFiles(dir, "*Module.cs", SearchOption.TopDirectoryOnly);

            if (moduleFiles.Length == 0)
            {
                violations.Add($"  Module '{moduleName}' has no *Module.cs registration file");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} module folder(s) without registration files:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  9. NO ASYNC VOID IN VIEWMODELS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// <c>async void</c> methods swallow exceptions and break error
    /// propagation. Only WPF event handlers may use <c>async void</c>.
    /// ViewModels must use <c>async Task</c> for all async operations.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_UseAsyncVoid()
    {
        var pattern = AsyncVoidRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): async void — use async Task instead\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} async void method(s) in ViewModel code. "
            + "Use async Task to ensure exceptions propagate correctly.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  10. MULTI-SAVE WITHOUT TRANSACTION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Financial writes require transactions."
    /// Detects service methods that call <c>SaveChangesAsync</c>
    /// more than once without a <c>BeginTransactionAsync</c> in the
    /// same method. Multiple saves in one method risk partial writes
    /// on failure.
    /// <para>
    /// <b>Excluded:</b> <c>LoginService.ValidatePinAsync</c> — its two
    /// saves are on mutually exclusive code paths (success vs. failure).
    /// </para>
    /// </summary>
    [Fact]
    public void Services_MultiSave_MustUseTransaction()
    {
        var violations = new List<string>();

        // Methods with mutually exclusive saves (separate if/else branches)
        var excluded = new HashSet<string>(StringComparer.Ordinal) { "ValidatePinAsync", "ValidateMasterPinAsync" };

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            string? currentMethod = null;
            var methodStartLine = 0;
            var saveCount = 0;
            var hasTx = false;
            var inMethod = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Detect method start
                var methodMatch = MethodSignatureRegex().Match(trimmed);
                if (methodMatch.Success && trimmed.Contains("async", StringComparison.Ordinal))
                {
                    // Flush previous method
                    if (inMethod && saveCount > 1 && !hasTx &&
                        currentMethod is not null && !excluded.Contains(currentMethod))
                    {
                        var relative = Path.GetRelativePath(SolutionRoot, file);
                        violations.Add(
                            $"  {relative}({methodStartLine}): {saveCount} SaveChangesAsync without transaction in {currentMethod}");
                    }

                    currentMethod = methodMatch.Groups[1].Value;
                    methodStartLine = i + 1;
                    saveCount = 0;
                    hasTx = false;
                    inMethod = true;
                }

                if (inMethod)
                {
                    if (trimmed.Contains("SaveChangesAsync", StringComparison.Ordinal))
                        saveCount++;
                    if (trimmed.Contains("BeginTransactionAsync", StringComparison.Ordinal) ||
                        trimmed.Contains("[Transactional]", StringComparison.Ordinal))
                        hasTx = true;
                }
            }

            // Flush last method
            if (inMethod && saveCount > 1 && !hasTx &&
                currentMethod is not null && !excluded.Contains(currentMethod))
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                violations.Add(
                    $"  {relative}({methodStartLine}): {saveCount} SaveChangesAsync without transaction in {currentMethod}");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} service method(s) with multiple SaveChangesAsync but no transaction. "
            + "Wrap multi-save operations in BeginTransactionAsync/CommitAsync.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  11. STYLE CHAIN ORDER IN App.xaml
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §2: "Modern UI System —
    /// DesignSystem.xaml → FluentTheme.xaml → MotionSystem.xaml →
    /// GlobalStyles.xaml → PosStyles.xaml."
    /// <para>
    /// UI/ legacy dictionaries must appear BEFORE Core/Styles/ so that
    /// Core tokens take precedence for overlapping keys. Core style
    /// dictionaries must then appear in the exact order above.
    /// Wrong order causes silent resource-not-found failures at runtime.
    /// </para>
    /// </summary>
    [Fact]
    public void AppXaml_CoreStyles_MustBeInCorrectOrder()
    {
        var appXamlPath = Path.Combine(SolutionRoot, "App.xaml");
        Assert.True(File.Exists(appXamlPath), "App.xaml not found");

        var content = File.ReadAllText(appXamlPath);

        var violations = new List<string>();

        // Required Core order — each must appear after the previous
        var requiredOrder = new[]
        {
            "Core/Styles/DesignSystem.xaml",
            "Core/Styles/FluentTheme.xaml",
            "Core/Styles/MotionSystem.xaml",
            "Core/Styles/GlobalStyles.xaml",
            "Core/Styles/ToggleSwitch.xaml",
            "Core/Styles/PosStyles.xaml"
        };

        var lastIndex = -1;
        foreach (var dictPath in requiredOrder)
        {
            var idx = content.IndexOf(dictPath, StringComparison.Ordinal);
            if (idx < 0)
            {
                violations.Add($"  Missing: {dictPath} not found in App.xaml MergedDictionaries");
            }
            else if (idx <= lastIndex)
            {
                violations.Add($"  Wrong order: {dictPath} must appear after previous dictionaries");
            }
            lastIndex = idx;
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            "Style chain must be: DesignSystem → FluentTheme → MotionSystem → GlobalStyles → ToggleSwitch → PosStyles.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  12. DB-ACCESSING SERVICES MUST NOT BE SINGLETON
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §5: "DB-accessing services with no mutable state →
    /// Transient. Only state-holding services → Singleton."
    /// <para>
    /// Scans <c>*Module.cs</c> files for <c>AddSingleton&lt;I…Service,
    /// …Service&gt;</c> registrations and checks whether the concrete
    /// service class injects <c>IDbContextFactory</c>. If so, it must
    /// be <c>Transient</c>, not <c>Singleton</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void DbAccessingServices_MustNotBe_Singleton()
    {
        var singletonPattern = SingletonServiceRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Module.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var match = singletonPattern.Match(lines[i]);
                if (!match.Success)
                    continue;

                var concreteType = match.Groups[1].Value;

                // Find the concrete service file
                var serviceFile = GetModuleCsFiles()
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                        .Equals(concreteType, StringComparison.OrdinalIgnoreCase));

                if (serviceFile is null)
                    continue;

                var serviceContent = File.ReadAllText(serviceFile);
                if (serviceContent.Contains("IDbContextFactory", StringComparison.Ordinal))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): {concreteType} accesses DB but is registered as Singleton — use AddTransient\n    {lines[i].TrimStart()}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} DB-accessing service(s) registered as Singleton. "
            + "DB-accessing services with no mutable state must be Transient.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  13. VIEWMODELS MUST INHERIT BaseViewModel
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §1: "ViewModels inherit BaseViewModel.
    /// PIN dialogs inherit PinPadViewModel."
    /// <para>
    /// Every <c>*ViewModel.cs</c> class must extend <c>BaseViewModel</c>
    /// or <c>PinPadViewModel</c>. Sub-component VMs (e.g. cart line items,
    /// dashboard strips) that extend <c>ObservableObject</c> directly
    /// must be listed in the excluded set.
    /// </para>
    /// </summary>
    [Fact]
    public void ViewModels_MustInherit_BaseViewModel()
    {
        var classPattern = ViewModelClassRegex();
        var violations = new List<string>();

        // Sub-component VMs that legitimately skip BaseViewModel
        var excluded = new HashSet<string>(StringComparer.Ordinal)
        {
            "DashboardViewModel",  // Status bar sub-component (ObservableObject)
            "CartLineViewModel"    // Inline cart line item (ObservableObject)
        };

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            var match = classPattern.Match(content);
            if (!match.Success)
                continue;

            var className = match.Groups[1].Value;
            var baseClass = match.Groups[2].Value;

            if (excluded.Contains(className))
                continue;

            if (baseClass is not "BaseViewModel" and not "PinPadViewModel")
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                violations.Add(
                    $"  {relative}: {className} inherits {baseClass} — must inherit BaseViewModel or PinPadViewModel");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} ViewModel(s) not inheriting BaseViewModel:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  14. EF ASYNC CALLS MUST FORWARD CancellationToken
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §5: "Every async service method MUST accept
    /// CancellationToken ct = default and pass it to all EF Core calls."
    /// <para>
    /// Detects EF async calls like <c>ToListAsync()</c>,
    /// <c>FirstOrDefaultAsync()</c>, <c>SaveChangesAsync()</c> that
    /// are called without passing <c>ct</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void Services_EfAsyncCalls_MustForwardCancellationToken()
    {
        var pattern = EfAsyncWithoutCtRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): EF async call without ct\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} EF async call(s) without CancellationToken. "
            + "Pass 'ct' to all EF async methods.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  15. DIALOG WINDOWS MUST INHERIT BaseDialogWindow
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §1: "Dialog windows inherit BaseDialogWindow."
    /// <para>
    /// Scans all <c>*Window.xaml.cs</c> code-behind files for the class
    /// declaration. Dialogs (shown via <c>IDialogService</c>) must
    /// inherit <c>BaseDialogWindow</c>. Non-dialog windows
    /// (MainWindow, LoginWindow, SetupWindow) are
    /// excluded — they use <c>WindowSizingService</c> directly.
    /// </para>
    /// </summary>
    [Fact]
    public void DialogWindows_MustInherit_BaseDialogWindow()
    {
        var classPattern = WindowClassRegex();
        var violations = new List<string>();

        // Non-dialog windows that legitimately inherit Window
        var excluded = new HashSet<string>(StringComparer.Ordinal)
        {
            "MainWindow",
            "LoginWindow",
            "SetupWindow"
        };

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Window.xaml.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            var match = classPattern.Match(content);
            if (!match.Success)
                continue;

            var className = match.Groups[1].Value;
            var baseClass = match.Groups[2].Value;

            if (excluded.Contains(className))
                continue;

            if (baseClass != "BaseDialogWindow")
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                violations.Add(
                    $"  {relative}: {className} inherits {baseClass} — dialog windows must inherit BaseDialogWindow");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} dialog window(s) not inheriting BaseDialogWindow:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  16. DIALOG WINDOWS MUST HAVE AddDialogRegistration
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Dialog modules register via
    /// <c>AddDialogRegistration&lt;TWindow&gt;()</c>."
    /// <para>
    /// Scans <c>*Module.cs</c> files for <c>AddTransient&lt;*Window&gt;</c>
    /// registrations and ensures a corresponding
    /// <c>AddDialogRegistration&lt;*Window&gt;</c> call exists for every
    /// dialog window (those inheriting <c>BaseDialogWindow</c>).
    /// </para>
    /// </summary>
    [Fact]
    public void DialogWindows_MustHave_DialogRegistration()
    {
        var violations = new List<string>();

        // Collect all BaseDialogWindow subclasses
        var dialogWindows = new HashSet<string>(StringComparer.Ordinal);
        var windowClassPat = WindowClassRegex();
        foreach (var file in GetModuleCsFiles())
        {
            if (!file.EndsWith("Window.xaml.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            var match = windowClassPat.Match(content);
            if (match.Success && match.Groups[2].Value == "BaseDialogWindow")
                dialogWindows.Add(match.Groups[1].Value);
        }

        // Check module registrations
        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Module.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);

            foreach (var dialog in dialogWindows)
            {
                if (content.Contains($"AddTransient<{dialog}>", StringComparison.Ordinal) &&
                    !content.Contains($"AddDialogRegistration<{dialog}>", StringComparison.Ordinal))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}: {dialog} registered as Transient but missing AddDialogRegistration");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} dialog window(s) missing AddDialogRegistration. "
            + "Dialog windows must be registered via AddDialogRegistration<TWindow>(dialogKey).\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  17. SERVICES MUST NOT USE HARDCODED FORMAT STRINGS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §6: "Formatting: IRegionalSettingsService.FormatCurrency/
    /// Date/Time — never hardcode format strings."
    /// <para>
    /// Detects <c>.ToString("C")</c>, <c>.ToString("N2")</c>,
    /// <c>.ToString("dd…")</c> etc. in service code. Services should use
    /// <c>IRegionalSettingsService.FormatCurrency()</c>,
    /// <c>FormatNumber()</c>, <c>FormatDate()</c> etc. instead.
    /// <c>RegionalSettingsService</c> itself is excluded (it defines
    /// the format helpers).
    /// </para>
    /// </summary>
    [Fact]
    public void Services_ShouldNot_UseHardcodedFormatStrings()
    {
        var pattern = HardcodedToStringFormatRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip the service that defines the format helpers
            if (Path.GetFileName(file).Equals("RegionalSettingsService.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Hardcoded format string — use IRegionalSettingsService\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} hardcoded format string(s) in service code. "
            + "Use IRegionalSettingsService.FormatCurrency/Number/Date instead.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  18. MAXLENGTH SYNC: XAML ↔ VIEWMODEL
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "MaxLength in XAML MUST match MaxLength in Model
    /// and ViewModel — all three layers in sync."
    /// <para>
    /// Scans XAML <c>TextBox</c> bindings for <c>MaxLength="N"</c> and
    /// verifies the bound ViewModel property has a matching
    /// <c>[MaxLength(N)]</c> attribute. Only checks ViewModels that
    /// use <c>[NotifyDataErrorInfo]</c> (attribute-based validation).
    /// VMs using legacy <c>Validate()</c> are excluded.
    /// </para>
    /// </summary>
    [Fact]
    public void MaxLength_InXaml_MustMatch_ViewModel()
    {
        var maxLenXamlPattern = MaxLengthXamlRegex();
        var bindingPropertyPattern = BindingPropertyRegex();
        var violations = new List<string>();
        var modulesDir = Path.Combine(SolutionRoot, "Modules");

        // Build a map: module name → (property → MaxLength)
        var vmByModule = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            if (!content.Contains("[NotifyDataErrorInfo]", StringComparison.Ordinal))
                continue;

            // Determine module
            var relPath = Path.GetRelativePath(modulesDir, file);
            var moduleName = relPath.Split(Path.DirectorySeparatorChar)[0];

            if (!vmByModule.TryGetValue(moduleName, out var props))
            {
                props = new Dictionary<string, int>(StringComparer.Ordinal);
                vmByModule[moduleName] = props;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var maxMatch = MaxLengthAttrRegex().Match(lines[i].TrimStart());
                if (!maxMatch.Success)
                    continue;

                var maxVal = int.Parse(maxMatch.Groups[1].Value);

                for (var j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                {
                    var propMatch = PropertyNameRegex().Match(lines[j]);
                    if (propMatch.Success)
                    {
                        props[propMatch.Groups[1].Value] = maxVal;
                        break;
                    }
                }
            }
        }

        // Scan XAML files — only compare with same-module ViewModel
        foreach (var xamlFile in Directory.EnumerateFiles(modulesDir, "*.xaml", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(modulesDir, xamlFile);
            var moduleName = relPath.Split(Path.DirectorySeparatorChar)[0];

            if (!vmByModule.TryGetValue(moduleName, out var props))
                continue;

            var lines = File.ReadAllLines(xamlFile);
            for (var i = 0; i < lines.Length; i++)
            {
                var maxMatch = maxLenXamlPattern.Match(lines[i]);
                if (!maxMatch.Success)
                    continue;

                var xamlMax = int.Parse(maxMatch.Groups[1].Value);

                var context = string.Join(" ", lines, Math.Max(0, i - 2), Math.Min(5, lines.Length - Math.Max(0, i - 2)));
                var bindMatch = bindingPropertyPattern.Match(context);
                if (!bindMatch.Success)
                    continue;

                var propertyName = bindMatch.Groups[1].Value;

                if (props.TryGetValue(propertyName, out var vmMax) && vmMax != xamlMax)
                {
                    var relative = Path.GetRelativePath(SolutionRoot, xamlFile);
                    violations.Add(
                        $"  {relative}({i + 1}): XAML MaxLength={xamlMax} vs ViewModel [MaxLength({vmMax})]  (property: {propertyName})");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} MaxLength mismatch(es) between XAML and ViewModel. "
            + "MaxLength must be in sync across XAML, ViewModel, and Model.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  19. MAXLENGTH SYNC: MODEL ↔ VIEWMODEL
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "MaxLength in XAML MUST match MaxLength in Model
    /// and ViewModel — all three layers in sync."
    /// <para>
    /// For each ViewModel property with <c>[MaxLength(N)]</c>, finds
    /// a Model class with a same-named property and verifies the
    /// <c>[MaxLength]</c> values match. Catches drift when a Model
    /// column length changes but the ViewModel isn't updated.
    /// </para>
    /// </summary>
    [Fact]
    public void MaxLength_InModel_MustMatch_ViewModel()
    {
        var violations = new List<string>();
        var modelsDir = Path.Combine(SolutionRoot, "Models");

        if (!Directory.Exists(modelsDir))
            return;

        // Build model property → MaxLength map (property name → set of values across models)
        var modelMaxByProp = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(modelsDir, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var maxMatch = MaxLengthAttrRegex().Match(lines[i].TrimStart());
                if (!maxMatch.Success) continue;

                var maxVal = int.Parse(maxMatch.Groups[1].Value);

                for (var j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                {
                    var propMatch = ModelPropertyRegex().Match(lines[j]);
                    if (propMatch.Success)
                    {
                        var propName = propMatch.Groups[1].Value;
                        if (!modelMaxByProp.TryGetValue(propName, out var set))
                        {
                            set = [];
                            modelMaxByProp[propName] = set;
                        }
                        set.Add(maxVal);
                        break;
                    }
                }
            }
        }

        // Check ViewModel properties
        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            if (!content.Contains("[NotifyDataErrorInfo]", StringComparison.Ordinal))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var maxMatch = MaxLengthAttrRegex().Match(lines[i].TrimStart());
                if (!maxMatch.Success) continue;

                var vmMax = int.Parse(maxMatch.Groups[1].Value);

                for (var j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                {
                    var propMatch = PropertyNameRegex().Match(lines[j]);
                    if (!propMatch.Success) continue;

                    var propName = propMatch.Groups[1].Value;

                    // Only flag when all model definitions agree on one MaxLength
                    // that differs from the VM value (avoids false positives when
                    // different models use different lengths for the same name)
                    if (modelMaxByProp.TryGetValue(propName, out var modelValues)
                        && modelValues.Count == 1
                        && !modelValues.Contains(vmMax))
                    {
                        var modelMax = modelValues.First();
                        var relative = Path.GetRelativePath(SolutionRoot, file);
                        violations.Add(
                            $"  {relative}({i + 1}): VM [MaxLength({vmMax})] vs Model [MaxLength({modelMax})]  (property: {propName})");
                    }
                    break;
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} MaxLength mismatch(es) between Model and ViewModel.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  20. WIZARD STEP METHODS MUST CLEAR MESSAGES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "SuccessMessage and ErrorMessage must be cleared
    /// on step changes (Next/Back)."
    /// <para>
    /// Scans wizard ViewModels (those with <c>CurrentStep</c>) for
    /// <c>Next</c> and <c>Back</c> methods that don't clear
    /// <c>ErrorMessage</c> or call <c>ClearMessages()</c> within the
    /// first few lines.
    /// </para>
    /// </summary>
    [Fact]
    public void WizardViewModels_StepMethods_MustClearMessages()
    {
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);

            // Only check wizard VMs (those with CurrentStep)
            if (!content.Contains("CurrentStep", StringComparison.Ordinal))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Match Next() / NextAsync() / Back() / BackAsync() methods
                if (!WizardStepMethodRegex().IsMatch(trimmed))
                    continue;

                // Collect method body (first 8 lines)
                var bodyLines = new List<string>();
                for (var j = i; j < Math.Min(i + 8, lines.Length); j++)
                    bodyLines.Add(lines[j]);

                var body = string.Join("\n", bodyLines);
                if (!body.Contains("ErrorMessage", StringComparison.Ordinal) &&
                    !body.Contains("ClearMessages", StringComparison.Ordinal))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Step method doesn't clear ErrorMessage/SuccessMessage\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} wizard step method(s) not clearing messages. "
            + "Next/Back must clear ErrorMessage and SuccessMessage.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  21. NO DIRECT WINDOW INSTANTIATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §1: "Dialogs are shown via
    /// IDialogService.ShowDialogAsync(dialogKey) — never instantiate
    /// windows directly."
    /// <para>
    /// Scans module code for <c>new …Window(</c> patterns. All dialog
    /// creation must go through <c>IDialogService</c>. Workflow classes
    /// that create top-level windows (Login, Setup) via DI are excluded
    /// since they use <c>IServiceProvider</c>, not <c>new</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void Modules_ShouldNot_InstantiateWindowsDirectly()
    {
        var pattern = NewWindowRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Direct window instantiation — use IDialogService\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} direct window instantiation(s). "
            + "Use IDialogService.ShowDialogAsync(dialogKey) instead.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  22. SERVICES MUST USE IDbContextFactory (NOT DbContext DIRECTLY)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §5: "EF Core via IDbContextFactory&lt;AppDbContext&gt;
    /// — short-lived contexts."
    /// <para>
    /// Services must inject <c>IDbContextFactory&lt;AppDbContext&gt;</c>
    /// and create short-lived contexts. Injecting <c>AppDbContext</c>
    /// directly causes lifetime management issues with Transient services.
    /// </para>
    /// </summary>
    [Fact]
    public void Services_MustUse_DbContextFactory()
    {
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Path.GetFileName(file).StartsWith('I'))
                continue;

            var content = File.ReadAllText(file);

            // Skip services that don't access DB at all
            if (!content.Contains("AppDbContext", StringComparison.Ordinal))
                continue;

            // Check for direct injection of AppDbContext (not via factory)
            if (DirectDbContextInjectionRegex().IsMatch(content) &&
                !content.Contains("IDbContextFactory", StringComparison.Ordinal))
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                violations.Add(
                    $"  {relative}: Injects AppDbContext directly — use IDbContextFactory<AppDbContext>");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} service(s) injecting AppDbContext directly. "
            + "Use IDbContextFactory<AppDbContext> for short-lived contexts.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  23. VIEWMODELS AND VIEWS MUST BE TRANSIENT
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "ViewModels: Transient. Views: Transient."
    /// <para>
    /// Scans <c>*Module.cs</c> files for <c>AddSingleton</c> or
    /// <c>AddScoped</c> registrations of ViewModel or View types.
    /// These must always be <c>AddTransient</c> to avoid stale state
    /// and cross-dialog data leaks.
    /// </para>
    /// </summary>
    [Fact]
    public void ViewModelsAndViews_MustBe_Transient()
    {
        var pattern = NonTransientUiRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Module.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Non-Transient ViewModel/View/Window — use AddTransient\n    {lines[i].TrimStart()}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} non-Transient ViewModel/View/Window registration(s). "
            + "ViewModels, Views, and Windows must always be AddTransient.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  24. WIZARD DIALOGS MUST USE ConfirmStepCommand PATTERN
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "For multi-step wizard dialogs, never bind
    /// ConfirmCommand directly to SaveCommand. Create a
    /// ConfirmStepCommand that delegates to Next on non-last steps
    /// and Save on the last step."
    /// <para>
    /// Scans wizard ViewModels (those with <c>CurrentStep</c> and
    /// <c>SaveAsync</c>) for a <c>ConfirmStep</c> command.
    /// Without it, pressing Enter on the last field of a non-final
    /// step triggers Save prematurely.
    /// Only applies to VMs whose View inherits <c>BaseDialogWindow</c>
    /// (which has keyboard-driven <c>DefaultCommand</c> routing).
    /// </para>
    /// </summary>
    [Fact]
    public void WizardViewModels_MustHave_ConfirmStepCommand()
    {
        var violations = new List<string>();

        // Collect VMs that are used in BaseDialogWindow-based views
        var dialogVmNames = new HashSet<string>(StringComparer.Ordinal);
        var windowClassPat = WindowClassRegex();
        foreach (var file in GetModuleCsFiles())
        {
            if (!file.EndsWith("Window.xaml.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            var classMatch = windowClassPat.Match(content);
            if (!classMatch.Success || classMatch.Groups[2].Value != "BaseDialogWindow")
                continue;

            // Extract ViewModel type from DataContext cast or constructor
            var vmMatch = Regex.Match(content, @"(\w+ViewModel)");
            if (vmMatch.Success)
                dialogVmNames.Add(vmMatch.Groups[1].Value);
        }

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);

            // Only check wizard VMs with step navigation and a save
            if (!content.Contains("CurrentStep", StringComparison.Ordinal))
                continue;
            if (!content.Contains("Save", StringComparison.Ordinal))
                continue;
            if (!content.Contains("Next", StringComparison.Ordinal))
                continue;

            // Extract class name
            var classMatch = ViewModelClassRegex().Match(content);
            if (!classMatch.Success)
                continue;

            var vmName = classMatch.Groups[1].Value;

            // Only enforce for BaseDialogWindow-based VMs
            if (!dialogVmNames.Contains(vmName))
                continue;

            if (!content.Contains("ConfirmStep", StringComparison.Ordinal))
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                violations.Add(
                    $"  {relative}: Wizard dialog VM with CurrentStep + Save but no ConfirmStepCommand");
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} wizard dialog VM(s) missing ConfirmStepCommand. "
            + "Wizard dialogs must use ConfirmStepCommand to prevent premature Save on Enter.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  25. STYLE DICTIONARIES MUST NOT REFERENCE UNDEFINED KEYS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that every <c>{StaticResource Key}</c> in
    /// <c>Core/Styles/*.xaml</c> references a key that is defined in
    /// a previously-merged dictionary, in <c>App.xaml</c>, or locally.
    /// <para>
    /// Style dictionaries are merged in a fixed order (DesignSystem →
    /// FluentTheme → MotionSystem → GlobalStyles → ToggleSwitch →
    /// PosStyles). A resource in FluentTheme can reference a key from
    /// DesignSystem but not from GlobalStyles. A missing key causes a
    /// runtime <c>XamlParseException</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void StyleDictionaries_StaticResources_MustReferencePriorKeys()
    {
        var coreDir = Path.Combine(SolutionRoot, "Core", "Styles");
        var uiDir = Path.Combine(SolutionRoot, "UI");
        var appXaml = Path.Combine(SolutionRoot, "App.xaml");

        // Ordered list matching App.xaml merge order
        var coreChain = new[]
        {
            "DesignSystem.xaml", "FluentTheme.xaml", "MotionSystem.xaml",
            "GlobalStyles.xaml", "ToggleSwitch.xaml", "PosStyles.xaml"
        };

        var keyPattern = StyleResourceKeyRegex();
        var refPattern = StyleResourceRefRegex();

        // Collect keys from UI/ dictionaries (merged before Core)
        var cumulativeKeys = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(uiDir))
        {
            foreach (var file in Directory.EnumerateFiles(uiDir, "*.xaml", SearchOption.AllDirectories))
            {
                foreach (Match m in keyPattern.Matches(File.ReadAllText(file)))
                    cumulativeKeys.Add(m.Groups[1].Value);
            }
        }

        // Collect keys from App.xaml (converters, etc.)
        if (File.Exists(appXaml))
        {
            foreach (Match m in keyPattern.Matches(File.ReadAllText(appXaml)))
                cumulativeKeys.Add(m.Groups[1].Value);
        }

        var violations = new List<string>();

        foreach (var dictName in coreChain)
        {
            var filePath = Path.Combine(coreDir, dictName);
            if (!File.Exists(filePath))
                continue;

            var content = File.ReadAllText(filePath);

            // Collect local keys
            var localKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in keyPattern.Matches(content))
                localKeys.Add(m.Groups[1].Value);

            // Check references — skip lines inside XML comments
            var lines = File.ReadAllLines(filePath);
            var inComment = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Track multi-line comment state
                if (trimmed.Contains("<!--"))
                    inComment = true;
                if (trimmed.Contains("-->"))
                {
                    inComment = false;
                    continue;
                }
                if (inComment)
                    continue;

                foreach (Match m in refPattern.Matches(lines[i]))
                {
                    var key = m.Groups[1].Value;
                    if (!cumulativeKeys.Contains(key) && !localKeys.Contains(key))
                    {
                        violations.Add(
                            $"  Core/Styles/{dictName}({i + 1}): StaticResource '{key}' not defined in any prior dictionary");
                    }
                }
            }

            // Add local keys to cumulative for the next dictionary
            foreach (var k in localKeys)
                cumulativeKeys.Add(k);
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} broken StaticResource reference(s) in Core/Styles dictionaries. "
            + "Ensure referenced keys are defined in a previously-merged dictionary.\n"
            + string.Join("\n", violations));
    }

    // ── Additional regex for test 25 ─────────────────────────────

    /// <summary>Matches <c>x:Key="KeyName"</c> in XAML resource definitions.</summary>
    [GeneratedRegex(
        @"x:Key=""([^""]+)""",
        RegexOptions.Compiled)]
    private static partial Regex StyleResourceKeyRegex();

    /// <summary>Matches <c>{StaticResource KeyName}</c> in XAML (excludes comments).</summary>
    [GeneratedRegex(
        @"\{StaticResource\s+(\w+)\}",
        RegexOptions.Compiled)]
    private static partial Regex StyleResourceRefRegex();

    // ═══════════════════════════════════════════════════════════════════
    //  26. EVENTBUS SUBSCRIBE / UNSUBSCRIBE MUST BE BALANCED
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Derived VMs that subscribe to events or hold
    /// resources MUST override Dispose() and call base.Dispose()."
    /// <para>
    /// For every <c>_eventBus.Subscribe&lt;T&gt;</c> call in a
    /// ViewModel, there must be a matching
    /// <c>_eventBus.Unsubscribe&lt;T&gt;</c> in <c>Dispose()</c>.
    /// An unbalanced subscription causes a memory leak — the event
    /// bus holds a strong reference to the ViewModel, preventing GC.
    /// </para>
    /// </summary>
    [Fact]
    public void ViewModels_EventBusSubscriptions_MustBeBalanced()
    {
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            if (!content.Contains("_eventBus.Subscribe", StringComparison.Ordinal))
                continue;

            var subMatches = EventBusSubscribeRegex().Matches(content);
            var unsubMatches = EventBusUnsubscribeRegex().Matches(content);

            var subscribed = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in subMatches)
                subscribed.Add(m.Groups[1].Value);

            var unsubscribed = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in unsubMatches)
                unsubscribed.Add(m.Groups[1].Value);

            var leaking = subscribed.Except(unsubscribed).ToList();
            if (leaking.Count > 0)
            {
                var relative = Path.GetRelativePath(SolutionRoot, file);
                foreach (var evt in leaking)
                {
                    violations.Add(
                        $"  {relative}: Subscribes to {evt} but never Unsubscribes (memory leak)");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} unbalanced EventBus subscription(s). "
            + "Every Subscribe<T> must have a matching Unsubscribe<T> in Dispose().\n"
            + string.Join("\n", violations));
    }

    /// <summary>Matches <c>_eventBus.Subscribe&lt;FooEvent&gt;</c>.</summary>
    [GeneratedRegex(
        @"_eventBus\.Subscribe<(\w+)>",
        RegexOptions.Compiled)]
    private static partial Regex EventBusSubscribeRegex();

    /// <summary>Matches <c>_eventBus.Unsubscribe&lt;FooEvent&gt;</c>.</summary>
    [GeneratedRegex(
        @"_eventBus\.Unsubscribe<(\w+)>",
        RegexOptions.Compiled)]
    private static partial Regex EventBusUnsubscribeRegex();

    // ═══════════════════════════════════════════════════════════════════
    //  27. COMMAND HANDLERS MUST BE TRANSIENT
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES: "Command handlers: Transient."
    /// <para>
    /// Scans <c>*Module.cs</c> files for handler registrations using
    /// <c>AddSingleton</c> or <c>AddScoped</c>. Handlers must be
    /// <c>AddTransient</c> because they may inject short-lived
    /// <c>IDbContextFactory</c> contexts and must not hold state
    /// across requests.
    /// </para>
    /// </summary>
    [Fact]
    public void CommandHandlers_MustBe_Transient()
    {
        var pattern = NonTransientHandlerRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Module.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Non-Transient handler — use AddTransient\n    {lines[i].TrimStart()}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} non-Transient command handler registration(s). "
            + "Command handlers must always be AddTransient.\n"
            + string.Join("\n", violations));
    }

    /// <summary>
    /// Matches <c>AddSingleton</c> or <c>AddScoped</c> registrations
    /// of handler types (ICommandRequestHandler, BaseCommandHandler).
    /// </summary>
    [GeneratedRegex(
        @"(?:AddSingleton|AddScoped)<.*(?:Handler|CommandRequest)",
        RegexOptions.Compiled)]
    private static partial Regex NonTransientHandlerRegex();

    // ═══════════════════════════════════════════════════════════════════
    //  28. SERVICES MUST NOT USE ToShortDateString / ToLongDateString
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §6: "Formatting: IRegionalSettingsService.FormatDate —
    /// never hardcode format strings."
    /// <para>
    /// <c>ToShortDateString()</c> and <c>ToLongDateString()</c> use the
    /// thread's current culture, bypassing <c>IRegionalSettingsService</c>.
    /// Services must use <c>regional.FormatDate()</c> instead.
    /// </para>
    /// </summary>
    [Fact]
    public void Services_ShouldNot_UseDateStringMethods()
    {
        var pattern = DateStringMethodRegex();
        var violations = new List<string>();

        foreach (var file in GetModuleCsFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Path.GetFileName(file).StartsWith('I'))
                continue;
            if (Path.GetFileName(file).Equals("RegionalSettingsService.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(SolutionRoot, file);
                    violations.Add(
                        $"  {relative}({i + 1}): Culture-dependent date method — use IRegionalSettingsService.FormatDate\n    {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} culture-dependent date formatting call(s) in service code. "
            + "Use IRegionalSettingsService.FormatDate/FormatTime instead.\n"
            + string.Join("\n", violations));
    }

    /// <summary>
    /// Matches <c>.ToShortDateString()</c>, <c>.ToLongDateString()</c>,
    /// <c>.ToShortTimeString()</c>, <c>.ToLongTimeString()</c>.
    /// </summary>
    [GeneratedRegex(
        @"\.To(?:Short|Long)(?:Date|Time)String\(\)",
        RegexOptions.Compiled)]
    private static partial Regex DateStringMethodRegex();
}
