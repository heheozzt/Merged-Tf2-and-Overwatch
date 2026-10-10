using System.Reflection;
// usage: apidump <dll> <dir-of-deps> <type-regex>
var dll = args[0]; var depDir = args[1]; var rx = new System.Text.RegularExpressions.Regex(args[2]);
var paths = Directory.GetFiles(depDir, "*.dll").Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")).Append(dll).Distinct();
var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths));
var asm = ctx.LoadFromAssemblyPath(dll);
Type[] types; try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
string N(Type t) { try { return t.Name; } catch { return "?"; } }
foreach (var t in types.Where(t => rx.IsMatch(t.FullName ?? "")).OrderBy(t => t.FullName))
{
    string bt = ""; try { bt = t.BaseType?.Name; } catch {}
    Console.WriteLine($"== {t.FullName} : {bt}");
    var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    try { foreach (var f in t.GetFields(bf)) { string ft; try { ft = N(f.FieldType); } catch { ft = "?"; } Console.WriteLine($"  F {(f.IsStatic ? "static " : "")}{ft} {f.Name}"); } } catch (Exception e) { Console.WriteLine("  fields err " + e.Message); }
    try { foreach (var p in t.GetProperties(bf)) { string pt; try { pt = N(p.PropertyType); } catch { pt = "?"; } Console.WriteLine($"  P {pt} {p.Name}"); } } catch {}
    try { foreach (var m in t.GetMethods(bf)) { string ps; try { ps = string.Join(", ", m.GetParameters().Select(p => N(p.ParameterType) + " " + p.Name)); } catch { ps = "?"; } string rt; try { rt = N(m.ReturnType); } catch { rt = "?"; } Console.WriteLine($"  M {(m.IsStatic ? "static " : "")}{rt} {m.Name}({ps})"); } } catch {}
}
