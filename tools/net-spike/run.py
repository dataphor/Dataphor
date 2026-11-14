"""Modern-.NET feasibility spike driver. See README.md in this folder.

    python tools/net-spike/run.py <work-dir> [--tfm net10.0] [--smoke] [--coverage] [--only A,B]

Everything is generated under <work-dir> (keep it OUTSIDE the repository). No
repository source is modified: each legacy csproj is wrapped by an SDK-style
project that compiles the same source list by absolute path, a handful of
source files are replaced by patched copies (see PATCHES), and API surface
that no longer exists is stubbed (see stubs/) so the compiler reaches the real
usage sites. Projects already converted to SDK style are built in place, into
the repository's git-ignored artifacts/ folder, and wrappers reference them.
"""
import argparse
import os
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True  # importing convert must not leave __pycache__ in the repository
from convert import compiled_files, is_converted

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

# (repo-relative file, [(old, new), ...]) -- every patch here is a change the
# real port must make. docs/modernization-plan.md, Appendix A explains each.
PATCHES = [
    # System.Data.SQLite API drift, and take the write lock up front (BEGIN IMMEDIATE) -- see plan, catalog store.
    (r"src\engine\Store.SQLite\SQLiteConnection.cs",
     [("localException.ErrorCode", "localException.ResultCode"),
      ("base.InternalBeginTransaction(isolationLevel);",
       "base.InternalBeginTransaction(SQLIsolationLevel.Serializable); /* SPIKE: BEGIN IMMEDIATE */")]),
    # A static, non-thread-safe emitter shared by every catalog session: concurrent DDL garbles stored object scripts.
    (r"src\engine\Server\Device.Catalog\ServerCatalogDeviceSession.cs",
     [("return _emitter.Emit(", "return new D4TextEmitter().Emit(")]),
    # System.Xml.IStreamProvider now collides with Dataphor's.
    (r"libraries\Frontend\Source\Device\FrontendDevice.cs",
     [("public class FrontendDeviceSession : MemoryDeviceSession, IStreamProvider",
       "public class FrontendDeviceSession : MemoryDeviceSession, Alphora.Dataphor.DAE.Streams.IStreamProvider")]),
    # The base Connection library registers the SQL CE connection class; SQL CE does not exist on modern .NET.
    (r"libraries\Connection\Source\Register.cs",
     [('classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "SQLCEConnection"), typeof(SQLCEConnection).AssemblyQualifiedName));',
       "/* SPIKE: SQLCE removed */")]),
]

STUBS = {"Server": "Server.cs"}

# D4 library folder (repo-relative) -> (project, assembly file) needed to run the coverage
# scripts. Each is copied to covlibs/<folder name>: the server finds libraries as immediate subfolders.
COVERAGE_LIBS = {
    "libraries/Frontend": ("Frontend.Server", "Alphora.Dataphor.Frontend.Server.dll"),
    "libraries/SimpleDevice": ("DAE.Device.Simple", "Alphora.Dataphor.DAE.Device.Simple.dll"),
    "tests/libraries/TestFramework.Coverage.Base": ("DAE.Diagnostics", "Alphora.Dataphor.DAE.Diagnostics.dll"),
    "libraries/SQLDevice": ("DAE.Device.SQL", "Alphora.Dataphor.DAE.Device.SQL.dll"),
    "libraries/MSSQLDevice": ("DAEDeviceMSSQL", "AlphoraMSSQLDevice.dll"),
    "libraries/Connection": ("DAE.Connection", "Alphora.Dataphor.DAE.Connection.dll"),
    "tests/libraries/TestFramework": None,
    "tests/libraries/TestFramework.Coverage.Scripts": None,
}


# Project name -> absolute path of each listed project that is already SDK-style; filled by main().
CONVERTED = {}


def check_overrides():
    """A converted project compiles the repository's files, so a patch or stub aimed at it would be ignored."""
    for name, proj in CONVERTED.items():
        if name in STUBS:
            sys.exit(f"STUBS has an entry for {name}, which is SDK-style and builds in place without it: "
                     "remove the API use from the repository and the entry from STUBS")
        files = compiled_files(proj)
        for rel, _ in PATCHES:
            if os.path.normcase(os.path.abspath(os.path.join(REPO, rel))) in files:
                sys.exit(f"PATCHES entry {rel} is compiled by {name}, which is SDK-style and builds in place "
                         "without it: land the change in the repository (or leave it to the ticket that owns it) "
                         "and remove the entry")


def apply_patches(out):
    for rel, pairs in PATCHES:
        src = os.path.join(REPO, rel)
        dst = os.path.join(out, "_overrides", rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        text = open(src, encoding="utf-8-sig", newline="").read()
        for old, new in pairs:
            if old not in text:
                sys.exit(f"patch no longer applies (already fixed in the repo?): {rel}: {old[:60]}")
            text = text.replace(old, new)
        open(dst, "w", encoding="utf-8", newline="").write(text)


def run(cmd, **kw):
    return subprocess.run(cmd, text=True, capture_output=True, **kw)


def build(out, name):
    proj = CONVERTED.get(name) or os.path.join(out, name, name + ".csproj")
    log = os.path.join(out, name + ".log")
    r = run(["dotnet", "build", proj, "-nologo", "-v", "q", "-clp:NoSummary"])
    open(log, "w", encoding="utf-8").write(r.stdout + r.stderr)
    s = run([sys.executable, os.path.join(HERE, "summ.py"), log, "8"])
    print(s.stdout.replace(out + os.sep, "").rstrip())
    return r.returncode == 0


def find_dll(out, project, dll):
    if project in CONVERTED:
        # Only MSBuild knows where artifacts/ puts it; never a same-named wrapper left in an older work dir.
        r = run(["dotnet", "msbuild", CONVERTED[project], "-nologo", "-getProperty:TargetPath", "-p:Configuration=Debug"])
        path = r.stdout.strip()
        if r.returncode != 0 or os.path.basename(path) != dll or not os.path.exists(path):
            sys.exit(f"{dll} not built (MSBuild reports TargetPath {path!r})")
        return path
    for root, _, files in os.walk(os.path.join(out, project, "bin")):
        if dll in files:
            return os.path.join(root, dll)
    sys.exit(f"{dll} not built")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("work")
    ap.add_argument("--tfm", default="net10.0")
    ap.add_argument("--smoke", action="store_true", help="boot the engine in-process and run D4, incl. concurrent DDL")
    ap.add_argument("--coverage", action="store_true", help="run the D4 coverage scripts listed in tests/DAE.Server.Tests/CoverageTest.cs")
    ap.add_argument("--only", help="comma list of projects to build (default: all in projects.txt)")
    a = ap.parse_args()
    work = os.path.abspath(a.work)
    if os.path.commonpath([work, REPO]) == REPO:
        sys.exit("work-dir must be outside the repository")
    out = os.path.join(work, "out")
    os.makedirs(out, exist_ok=True)

    projects = [l.strip() for l in open(os.path.join(HERE, "projects.txt")) if l.strip()]
    for p in projects:
        path = os.path.join(REPO, *p.split("/"))
        if is_converted(path):
            CONVERTED[os.path.splitext(os.path.basename(p))[0]] = path
    check_overrides()
    apply_patches(out)
    r = run([sys.executable, os.path.join(HERE, "convert.py"), REPO, out, a.tfm] + projects)
    print(r.stdout + r.stderr)
    for project, stub in STUBS.items():
        shutil.copy(os.path.join(HERE, "stubs", stub), os.path.join(out, project, "Stubs.cs"))
    for tool in ("Smoke", "Cov"):
        shutil.copytree(os.path.join(HERE, tool), os.path.join(out, tool), dirs_exist_ok=True)
        # Stored as .csproj.txt so IDEs and `dotnet build` at the repo root do not pick them up.
        os.remove(os.path.join(out, tool, tool + ".csproj.txt"))
        template = open(os.path.join(HERE, tool, tool + ".csproj.txt"), encoding="utf-8").read()
        open(os.path.join(out, tool, tool + ".csproj"), "w", encoding="utf-8").write(template.replace("net8.0", a.tfm))

    names = [os.path.splitext(os.path.basename(p))[0] for p in projects]
    if a.only:
        names = [n for n in names if n in a.only.split(",")]
    for n in names:
        build(out, n)

    rundir = os.path.join(work, "run")
    os.makedirs(rundir, exist_ok=True)
    if a.smoke:
        if build(out, "Smoke"):
            env = dict(os.environ, SMOKE_CS=";Journal Mode=WAL;BusyTimeout=30000")
            subprocess.run(["dotnet", find_dll(out, "Smoke", "Smoke.dll"), rundir, "sqlite", "8", "15"], env=env)
    if a.coverage:
        libs = os.path.join(rundir, "covlibs")
        shutil.rmtree(libs, ignore_errors=True)
        for folder, built in COVERAGE_LIBS.items():
            lib = os.path.join(libs, os.path.basename(folder))
            shutil.copytree(os.path.join(REPO, *folder.split("/")), lib,
                            ignore=shutil.ignore_patterns("Source", "*.dll", "bin", "obj"))
            if built:
                build(out, built[0])
                shutil.copy(find_dll(out, built[0], built[1]), lib)
        if build(out, "Cov"):
            subprocess.run(["dotnet", find_dll(out, "Cov", "Cov.dll"), rundir,
                            os.path.join(REPO, "tests", "DAE.Server.Tests", "CoverageTest.cs"), "base"])


if __name__ == "__main__":
    main()
