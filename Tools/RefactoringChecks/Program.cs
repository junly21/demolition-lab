using DemolitionLab;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string project = Path.GetFullPath(args.Length > 0 ? args[0] : Directory.GetCurrentDirectory());
if (!Directory.Exists(Path.Combine(project, "Assets/Scripts")))
    throw new Exception("Run from the repository root or pass its path.");
var trees = Directory.GetFiles(Path.Combine(project, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(project, "Assets/Editor"), "*.cs", SearchOption.AllDirectories))
    .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)).ToList();
int assertions = 0;
void Check(bool condition, string name)
{
    assertions++;
    if (!condition) throw new Exception(name);
}

foreach (var tree in trees)
{
    foreach (var diagnostic in tree.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error))
        throw new Exception(diagnostic.ToString());
    foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
        Check(method.ExplicitInterfaceSpecifier == null,
            $"Unexpected explicit-interface method: {method.Identifier} in {tree.FilePath}");
}
Console.WriteLine($"PASS: Roslyn syntax for {trees.Count} C# files (not a Unity compilation).");

var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path));
var partialCompilation = CSharpCompilation.Create("RefactoringAccessibility", trees, references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var accessibilityCodes = new HashSet<string>
{
    "CS0050", "CS0051", "CS0052", "CS0053", "CS0054", "CS0055", "CS0056", "CS0057", "CS0060", "CS0061", "CS0122"
};
foreach (var diagnostic in partialCompilation.GetDiagnostics().Where(item => accessibilityCodes.Contains(item.Id)))
    throw new Exception(diagnostic.ToString());
Console.WriteLine("PASS: local accessibility diagnostics (Unity assemblies are unavailable; other semantic diagnostics are not validated).");

var classes = trees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
    .ToDictionary(type => type.Identifier.ValueText);
foreach (var invocation in trees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()))
{
    if (invocation.Expression is not MemberAccessExpressionSyntax access
        || access.Expression is not IdentifierNameSyntax owner
        || !classes.TryGetValue(owner.Identifier.ValueText, out var type)) continue;
    string methodName = access.Name.Identifier.ValueText;
    var candidates = type.Members.OfType<MethodDeclarationSyntax>()
        .Where(method => method.Identifier.ValueText == methodName).ToList();
    Check(candidates.Count > 0, $"Missing local static method: {invocation}");
    var caller = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
    if (caller != type)
        Check(candidates.Any(method => !method.Modifiers.Any(SyntaxKind.PrivateKeyword)),
            $"Inaccessible method: {invocation}");
    Check(candidates.Any(method => method.ParameterList.Parameters.Count >= invocation.ArgumentList.Arguments.Count
        && method.ParameterList.Parameters.Count(parameter => parameter.Default == null) <= invocation.ArgumentList.Arguments.Count),
        $"Argument count mismatch: {invocation} in {invocation.SyntaxTree.FilePath}");
}

int staticAssertions = assertions;
foreach (int ammo in new[] { 1, 3, 4 })
{
    var flow = new StageFlowStateMachine();
    flow.Initialize(ammo);
    Check(flow.State == StageState.Initializing && flow.AmmoRemaining == ammo, "initial state");
    Check(!flow.RegisterShot() && !flow.Complete(true), "initial state guards");
    Check(flow.MarkReady() && flow.State == StageState.Ready, "ready");
    Check(!flow.RegisterShot(), "ready cannot fire");
    Check(flow.Activate() && flow.State == StageState.Active, "active");
    Check(!flow.Activate(), "duplicate activation");
    for (int shot = 1; shot <= ammo; shot++)
    {
        Check(flow.RegisterShot(), "accepted shot");
        Check(flow.AmmoRemaining == ammo - shot && flow.ShotsUsed == shot, "exact ammo decrement");
    }
    Check(!flow.RegisterShot() && flow.AmmoRemaining == 0, "no negative ammo");
    Check(!flow.Complete(true), "must settle before completion");
    Check(flow.BeginSettling() && !flow.BeginSettling(), "settle once");
    Check(!flow.RegisterShot(), "settling cannot fire");
    Check(flow.Complete(false) && flow.State == StageState.Failed, "fail");
    Check(!flow.RegisterShot() && !flow.Complete(true), "result state guards");
    flow.Initialize(ammo);
    Check(flow.ShotsUsed == 0 && flow.AmmoRemaining == ammo, "reinitialization");
    flow.MarkReady(); flow.Activate(); flow.RegisterShot(); flow.BeginSettling();
    Check(flow.Complete(true) && flow.State == StageState.Clear, "clear");
    Check(!flow.RegisterShot(), "clear cannot fire");
}
Console.WriteLine($"PASS: {staticAssertions} declaration/local-call checks; {assertions - staticAssertions} executable state-machine assertions.");
