"""Modern-.NET feasibility spike driver. See README.md in this folder.

    python tools/net-spike/run.py <work-dir> [--tfm net10.0] [--smoke] [--coverage] [--only A,B]

Everything is generated under <work-dir> (keep it OUTSIDE the repository). The
repository is never modified: each legacy csproj is wrapped by an SDK-style
project that compiles the same source list by absolute path, a handful of
source files are replaced by patched copies (see PATCHES), and API surface
that no longer exists is stubbed (see stubs/) so the compiler reaches the real
usage sites.
"""
import argparse
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

# (repo-relative file, [(old, new), ...]) -- every patch here is a change the
# real port must make. docs/modernization-plan.md, Appendix A explains each.
PATCHES = [
    # WCF client package has no ChannelFactory(string endpointConfigurationName) ctor.
    (r"Dataphor\Common\WCF\ServiceClient.cs",
     [("new ChannelFactory<T>(endpointConfigurationName))", "null /* SPIKE: config-name ctor n/a */)")]),
    (r"Dataphor\Client\Client\ClientServer.cs",
     [("new ChannelFactory<IClientDataphorService>(_clientConfigurationName);", "null; /* SPIKE */")]),
    # Reflection on the private field Thread.m_Name; the field does not exist on modern .NET (DEBUG builds only).
    (r"Dataphor\Base\ThreadUtility.cs",
     [("GetThreadNameFieldInfo().SetValue(thread, name);",
       "var fi = GetThreadNameFieldInfo(); if (fi != null) fi.SetValue(thread, name); /* SPIKE */")]),
    # System.Data.SQLite API drift, and take the write lock up front (BEGIN IMMEDIATE) -- see plan, catalog store.
    (r"Dataphor\DAE.SQLite\SQLiteConnection.cs",
     [("localException.ErrorCode", "localException.ResultCode"),
      ("base.InternalBeginTransaction(isolationLevel);",
       "base.InternalBeginTransaction(SQLIsolationLevel.Serializable); /* SPIKE: BEGIN IMMEDIATE */")]),
    # A static, non-thread-safe emitter shared by every catalog session: concurrent DDL garbles stored object scripts.
    (r"Dataphor\Server\Device.Catalog\ServerCatalogDeviceSession.cs",
     [("return _emitter.Emit(", "return new D4TextEmitter().Emit(")]),
    # System.Xml.IStreamProvider now collides with Dataphor's.
    (r"Libraries\Frontend\Source\Device\FrontendDevice.cs",
     [("public class FrontendDeviceSession : MemoryDeviceSession, IStreamProvider",
       "public class FrontendDeviceSession : MemoryDeviceSession, Alphora.Dataphor.DAE.Streams.IStreamProvider")]),
    # The base Connection library registers the SQL CE connection class; SQL CE does not exist on modern .NET.
    (r"Libraries\Connection\Source\Register.cs",
     [('classes.Add(new SettingsItem(String.Format("{0}.{1}", D4ClassDefinitionNameSpace, "SQLCEConnection"), typeof(SQLCEConnection).AssemblyQualifiedName));',
       "/* SPIKE: SQLCE removed */")]),
]

STUBS = {"Engine": "Engine.cs", "Server": "Server.cs"}

# D4 library -> (wrapper project, assembly file) needed to run the coverage scripts.
COVERAGE_LIBS = {
    "Frontend": ("Frontend.Server", "Alphora.Dataphor.Frontend.Server.dll"),
    "SimpleDevice": ("DAE.Device.Simple", "Alphora.Dataphor.DAE.Device.Simple.dll"),
    "TestFramework.Coverage.Base": ("DAE.Diagnostics", "Alphora.Dataphor.DAE.Diagnostics.dll"),
    "SQLDevice": ("DAE.Device.SQL", "Alphora.Dataphor.DAE.Device.SQL.dll"),
    "MSSQLDevice": ("DAEDeviceMSSQL", "AlphoraMSSQLDevice.dll"),
    "Connection": ("DAE.Connection", "Alphora.Dataphor.DAE.Connection.dll"),
    "TestFramework": None,
    "TestFramework.Coverage.Scripts": None,
}


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
    proj = os.path.join(out, name, name + ".csproj")
    log = os.path.join(out, name + ".log")
    r = run(["dotnet", "build", proj, "-nologo", "-v", "q", "-clp:NoSummary"])
    open(log, "w", encoding="utf-8").write(r.stdout + r.stderr)
    s = run([sys.executable, os.path.join(HERE, "summ.py"), log, "8"])
    print(s.stdout.replace(out + os.sep, "").rstrip())
    return r.returncode == 0


def find_dll(out, project, dll):
    for root, _, files in os.walk(os.path.join(out, project, "bin")):
        if dll in files:
            return os.path.join(root, dll)
    sys.exit(f"{dll} not built")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("work")
    ap.add_argument("--tfm", default="net10.0")
    ap.add_argument("--smoke", action="store_true", help="boot the engine in-process and run D4, incl. concurrent DDL")
    ap.add_argument("--coverage", action="store_true", help="run the D4 coverage scripts listed in Tests/DAE.Server.Tests/CoverageTest.cs")
    ap.add_argument("--only", help="comma list of wrapper projects to build (default: all in projects.txt)")
    a = ap.parse_args()
    work = os.path.abspath(a.work)
    if os.path.commonpath([work, REPO]) == REPO:
        sys.exit("work-dir must be outside the repository")
    out = os.path.join(work, "out")
    os.makedirs(out, exist_ok=True)

    projects = [l.strip() for l in open(os.path.join(HERE, "projects.txt")) if l.strip()]
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
        for lib, built in COVERAGE_LIBS.items():
            shutil.copytree(os.path.join(REPO, "Libraries", lib), os.path.join(libs, lib),
                            ignore=shutil.ignore_patterns("Source", "*.dll", "bin", "obj"))
            if built:
                build(out, built[0])
                shutil.copy(find_dll(out, built[0], built[1]), os.path.join(libs, lib))
        if build(out, "Cov"):
            subprocess.run(["dotnet", find_dll(out, "Cov", "Cov.dll"), rundir,
                            os.path.join(REPO, "Tests", "DAE.Server.Tests", "CoverageTest.cs"), "base"])


if __name__ == "__main__":
    main()
