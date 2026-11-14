"""Feasibility spike: wrap legacy csproj files in out-of-tree SDK-style projects.

Usage: python convert.py <repo_root> <out_dir> <tfm> <legacy.csproj> [...]

Each generated project compiles exactly the Compile/EmbeddedResource items the
legacy project lists (by absolute path), so nothing in the repo is modified.
A listed project that is already SDK-style gets no wrapper: wrappers reference
its real project file, and it builds in place into the repository's artifacts/.
"""
import os
import re
import sys
import xml.etree.ElementTree as ET

NS = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}


def is_converted(path):
    """True for an SDK-style project file: root <Project Sdk="..."> with no MSBuild 2003 namespace."""
    root = ET.parse(path).getroot()
    return root.tag == "Project" and root.get("Sdk") is not None


def compiled_files(path):
    """normcased absolute paths of the Compile items an SDK-style project lists."""
    d = os.path.dirname(os.path.abspath(path))
    return {os.path.normcase(os.path.abspath(os.path.join(d, c.get("Include"))))
            for c in ET.parse(path).getroot().iter("Compile") if c.get("Include")}


def central_versions(repo):
    """PackageVersion items of the repository's Directory.Packages.props, so wrappers never pin below the
    converted projects they reference (a lower version fails restore with NU1605)."""
    props = os.path.join(repo, "Directory.Packages.props")
    if not os.path.exists(props):
        return {}
    return {e.get("Include"): e.get("Version") for e in ET.parse(props).getroot().iter("PackageVersion")}

# Framework assembly reference -> NuGet package(s) on modern .NET
PACKAGE_MAP = {
    "system.configuration": [("System.Configuration.ConfigurationManager", "8.0.1")],
    "system.servicemodel": [
        ("System.ServiceModel.Primitives", "8.1.2"),
        ("System.ServiceModel.NetTcp", "8.1.2"),
        ("System.ServiceModel.Http", "8.1.2"),
    ],
    "system.serviceprocess": [("System.ServiceProcess.ServiceController", "8.0.1")],
    "system.data.sqlclient": [("System.Data.SqlClient", "4.9.0")],
    "newtonsoft.json": [("Newtonsoft.Json", "13.0.3")],
    "system.data.sqlite": [("System.Data.SQLite.Core", "1.0.119")],
    "system.drawing": [("System.Drawing.Common", "8.0.11")],
    "system.data.oledb": [("System.Data.OleDb", "8.0.1")],
    "system.data.odbc": [("System.Data.Odbc", "8.0.1")],
    "system.directoryservices": [("System.DirectoryServices", "8.0.0")],
    "system.management": [("System.Management", "8.0.0")],
    "system.security": [("System.Security.Cryptography.Xml", "8.0.2")],
    "npgsql": [("Npgsql", "8.0.6")],
}
ALWAYS = [("System.Data.SqlClient", "4.9.0"), ("System.Data.Odbc", "8.0.1"), ("System.Data.OleDb", "8.0.1"),
          ("System.Configuration.ConfigurationManager", "8.0.1")]


def main():
    repo, out, tfm = sys.argv[1], sys.argv[2], sys.argv[3]
    projects = sys.argv[4:]
    central = central_versions(repo)
    names = {}
    converted = set()
    for p in projects:
        src = os.path.normcase(os.path.abspath(os.path.join(repo, p)))
        names[src] = os.path.splitext(os.path.basename(p))[0]
        if is_converted(src):
            converted.add(src)
    for p in projects:
        src = os.path.abspath(os.path.join(repo, p))
        srcdir = os.path.dirname(src)
        name = names[os.path.normcase(src)]
        if os.path.normcase(src) in converted:
            print(f"{name}: SDK-style, used in place")
            continue
        root = ET.parse(src).getroot()

        def first(tag):
            e = root.find(".//m:" + tag, NS)
            return e.text if e is not None and e.text else None

        asm = first("AssemblyName") or name
        rootns = first("RootNamespace") or asm
        outtype = first("OutputType") or "Library"
        defines = ""
        for pg in root.findall("m:PropertyGroup", NS):
            cond = pg.get("Condition") or ""
            if "Debug" in cond:
                d = pg.find("m:DefineConstants", NS)
                if d is not None and d.text:
                    defines = d.text
                    break
        winforms = wpf = False
        pkgs = dict()
        missing_refs = []
        binrefs = []
        for r in root.findall(".//m:Reference", NS):
            inc = (r.get("Include") or "").split(",")[0].strip()
            key = inc.lower()
            if key == "system.windows.forms":
                winforms = True
            elif key in ("presentationcore", "presentationframework", "windowsbase", "system.xaml"):
                wpf = True
            elif key in PACKAGE_MAP:
                for pk, v in PACKAGE_MAP[key]:
                    pkgs[pk] = v
            elif key.startswith("system.servicemodel"):
                for pk, v in PACKAGE_MAP["system.servicemodel"]:
                    pkgs[pk] = v
            elif key in ("system", "system.core", "system.data", "system.xml", "system.xml.linq", "microsoft.csharp",
                         "system.data.datasetextensions", "system.runtime.serialization", "mscorlib",
                         "system.net.http", "system.numerics", "system.transactions", "system.io.compression",
                         "system.componentmodel.dataannotations"):
                pass
            else:
                # Third-party binary checked into the repo: reference it as-is (a .NET Framework
                # assembly; it compiles, but whether it runs on modern .NET is a separate question).
                hint = r.find("m:HintPath", NS)
                hint_path = os.path.abspath(os.path.join(srcdir, hint.text)) if hint is not None and hint.text else None
                if hint_path and os.path.exists(hint_path):
                    binrefs.append((inc, hint_path))
                else:
                    missing_refs.append(inc)
        for pk, v in ALWAYS:
            pkgs.setdefault(pk, v)
        prefs = []
        for r in root.findall(".//m:ProjectReference", NS):
            real = os.path.abspath(os.path.join(srcdir, r.get("Include")))
            inc = os.path.normcase(real)
            if inc in converted:
                prefs.append(real)
            elif inc in names:
                prefs.append(f"..\\{names[inc]}\\{names[inc]}.csproj")
            else:
                missing_refs.append("PROJECT:" + r.get("Include"))
        lines = ['<Project Sdk="Microsoft.NET.Sdk">', "  <PropertyGroup>"]
        use_tfm = tfm + ("-windows" if (winforms or wpf) and "windows" not in tfm else "")
        lines += [
            f"    <TargetFramework>{use_tfm}</TargetFramework>",
            f"    <AssemblyName>{asm}</AssemblyName>",
            f"    <RootNamespace>{rootns}</RootNamespace>",
            f"    <OutputType>{'Library' if outtype.lower() == 'library' else 'Exe'}</OutputType>",
            "    <EnableDefaultItems>false</EnableDefaultItems>",
            "    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>",
            "    <Nullable>disable</Nullable>", "    <Deterministic>false</Deterministic>",
            "    <ImplicitUsings>disable</ImplicitUsings>",
            "    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>",
            "    <NoWarn>$(NoWarn);CS0618;CS0168;CS0219;CS0649;CS0169;CS0414;CS1591;CS0108;CS0114;CS0067;SYSLIB0011;NU1701;NU1903;NU1902;NU1904;CA1416;WFO1000</NoWarn>",
            "    <EnableWindowsTargeting>true</EnableWindowsTargeting>",
            "    <GenerateResourceUsePreserializedResources>true</GenerateResourceUsePreserializedResources>",
            f"    <DefineConstants>{defines}</DefineConstants>",
        ]
        if winforms:
            lines.append("    <UseWindowsForms>true</UseWindowsForms>")
        if wpf:
            lines.append("    <UseWPF>true</UseWPF>")
        lines.append("  </PropertyGroup>")
        lines.append("  <ItemGroup>")
        lines.append("    <Compile Include=\"Stubs.cs\" Condition=\"Exists('Stubs.cs')\" />")
        n_compile = 0
        for c in root.findall(".//m:Compile", NS):
            inc = c.get("Include")
            if not inc:
                continue
            path = os.path.abspath(os.path.join(srcdir, inc.replace("%28", "(").replace("%29", ")")))
            ov = os.path.join(out, "_overrides", os.path.relpath(path, repo))
            if os.path.exists(ov):
                path = os.path.abspath(ov)
            lines.append(f'    <Compile Include="{path}" />')
            n_compile += 1
        for c in root.findall(".//m:EmbeddedResource", NS):
            inc = c.get("Include")
            if not inc:
                continue
            path = os.path.abspath(os.path.join(srcdir, inc))
            logical = rootns + "." + re.sub(r"[\\/]", ".", inc)
            if inc.lower().endswith(".resx"):
                logical = rootns + "." + re.sub(r"[\\/]", ".", inc[:-5]) + ".resources"
                lines.append(f'    <EmbeddedResource Include="{path}" ManifestResourceName="{logical[:-10]}" />')
            else:
                lines.append(f'    <EmbeddedResource Include="{path}" LogicalName="{logical}" />')
        lines.append("  </ItemGroup>")
        lines.append("  <ItemGroup>")
        for pk, v in sorted(pkgs.items()):
            lines.append(f'    <PackageReference Include="{pk}" Version="{central.get(pk, v)}" />')
        lines.append('    <PackageReference Include="System.Resources.Extensions" Version="8.0.0" />')
        for name_, path_ in binrefs:
            lines.append(f'    <Reference Include="{name_}"><HintPath>{path_}</HintPath></Reference>')
        for pr in prefs:
            lines.append(f'    <ProjectReference Include="{pr}" />')
        lines.append("  </ItemGroup>")
        lines.append("</Project>")
        d = os.path.join(out, name)
        os.makedirs(d, exist_ok=True)
        with open(os.path.join(d, name + ".csproj"), "w", encoding="utf-8") as f:
            f.write("\n".join(lines) + "\n")
        print(f"{name}: {n_compile} compile items, refs->{[os.path.splitext(os.path.basename(r))[0] for r in prefs]}, binary={[b[0] for b in binrefs]}, unmapped={missing_refs}, winforms={winforms}, wpf={wpf}")


if __name__ == "__main__":
    main()
